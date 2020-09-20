//*===================================================================================
//* ----||||Simulator||||----
//*
//* By Adam Bruss and Scott Nykl
//*
//* Scott participated in Fall of 2005. Adam has participated from fall 2005 
//* until the present.
//*
//* Loads in quake 3 m_maps. Three modes of interaction are Player, Ghost and Spectator.
//*===================================================================================

using System;
using System.IO;
using System.Collections.Generic;
using utilities;
using gl_font;
using obsvr;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;

namespace engine
{
	/// <summary>
	/// A collection of Shapes make up a single figure that is displayed.
	/// </summary>
	public class Figure : Communicator
	{
		public enum ESignals { SHAPE_READ = SignalStarts.g_nFigureStart, BOUNDINGBOX_INITIALIZED };

		const double m_dBoundingBoxDimension = 4.0;
		private const int m_nBoundingBoxVersion = 6;
		const double mcd_HalfWidth = 2.0;
		const double mcd_MaxEasyHopOverHeight = 0.3;
		Mutex m_mutThreadShutdownChecker = new Mutex();

		private const string m_sBoundingBoxHeader = "DEF BoundingBoxes {";
		private const string m_sBoundingBoxHeaderBSPLeaf = "DEF BoundingBoxesBSPLeaf {";
		private const string m_sMultitextureHeader = "texture MultiTexture { materialColor TRUE texture [";
		private const string m_sChannelOneTextureDef = "texture DEF";

		List<BoundingBox> m_lLeafBoundingBoxes = new List<BoundingBox>();

		protected List<Shape> m_lShapes = new List<Shape>();
		protected List<Shape> m_lShapesCustomRenderOrder = new List<Shape>();

		private List<BoundingBox> m_lFaceContainingBoundingBoxes = new List<BoundingBox>();
		private List<Viewpoint> m_SpecPoints = new List<Viewpoint>();
		private List<Viewpoint> m_SpawnPoints = new List<Viewpoint>();
		protected List<Face> m_lMapFaceReferences = null;
        List<BoundingBox> m_lLastBoxesInside = new List<BoundingBox>();
		List<Color> m_lBoxColors = new List<Color>();
		List<IntersectionInfo> m_lAllIntersections = new List<IntersectionInfo>();
		Dictionary<string, Texture> m_textureObjects = new Dictionary<string, Texture>();
		protected StreamReader m_srMapReader = null;
		BoundingBox m_BSPRootBoundingBox = null;
		HashSet<BoundingBox> m_hUtilBoxes = new HashSet<BoundingBox>();
		Mutex m_mutProgress = new Mutex();		

		private Edge m_RayCollider = new Edge();

		private MapInfo m_map = new MapInfo();
		private Zipper m_zipper = new Zipper();

		private int m_nInitializeProgress = 0;
		private int m_nThreadShutdownCounter = 0;

		private bool m_bUpToDateBoundingBoxes = false;
		int m_nTraversalCounter;

		private List<D3Vect> m_ld3Head = new List<D3Vect>();
		private List<D3Vect> m_ld3HeadLookAts = new List<D3Vect>();
        private D3Vect m_LookAtRay = new D3Vect();

		private BasicFont m_fonter = null;

		private int m_nMapFileLineCounter = 0;

		public Figure()
		{
			m_lBoxColors.Add(new Color(255, 0, 0));
			m_lBoxColors.Add(new Color(245, 222, 179));
			m_lBoxColors.Add(new Color(255, 215, 0));
			m_lBoxColors.Add(new Color(170, 221, 0));
			m_lBoxColors.Add(new Color(40, 174, 123));
			m_lBoxColors.Add(new Color(219, 254, 248));
			m_lBoxColors.Add(new Color(1, 152, 225));
			m_lBoxColors.Add(new Color(0, 0, 255));
			m_lBoxColors.Add(new Color(155, 48, 255));
			m_lBoxColors.Add(new Color(238, 121, 159));
			m_BSPRootBoundingBox = new BoundingBox(this);
		}

		public int GetNumFaces
		{
			get { return m_lMapFaceReferences.Count; }
		}

		public List<BoundingBox> GetFaceContainingBoundingBoxes() { return m_lFaceContainingBoundingBoxes; }

		public int GetNumShapes
		{
			get { return m_lShapes.Count + m_lShapesCustomRenderOrder.Count; }
		}

		public int GetNumViewPoints 
		{
			get { return m_SpecPoints.Count + m_SpawnPoints.Count; }
		}

		public int GetNumBBoxesLastInside
		{
			get { return m_lLastBoxesInside.Count; }
		}

		public int GetMapReadLinePosition
		{
			get	{ return m_nMapFileLineCounter;	}
		}

		public string GetDisplayName 
		{ 
			get	{ return m_map.ToString(); } 
		}

		public int GetDisplayListCreationProgress
		{
			get {
				m_mutProgress.WaitOne();
				int n = m_nInitializeProgress;
				m_mutProgress.ReleaseMutex();
				return n;
			}
		}

		public bool GetUpToDateBoundingBoxes
		{
			get { return m_bUpToDateBoundingBoxes; }
		}

		/// <summary>
		/// Shows all m_lShapes within the figure.
		/// </summary>
		virtual public void Show(Engine.EGraphicsMode mode, MovableCamera cam)
		{
			if (mode == Engine.EGraphicsMode.WIREFRAME)
			{
				sgl.PUSHATT(AttribMask.TextureBit | AttribMask.LineBit);
				GL.Disable(EnableCap.Texture2D);
				GL.LineWidth(1.2f);

				for (int i = 0; i < m_lShapes.Count; i++)
					m_lShapes[i].ShowWireframe();

                for (int i = 0; i < m_lShapesCustomRenderOrder.Count; i++)
					m_lShapesCustomRenderOrder[i].ShowWireframe();

                sgl.POPATT();
			}
			else
			{
				sgl.PUSHATT(AttribMask.AllAttribBits);				

				for (int i = 0; i < m_lShapes.Count; i++)
					m_lShapes[i].Show();

				// sort custom render order list
				m_lShapesCustomRenderOrder.Sort(CompareShapesByCamDistance);

                for (int i = 0; i < m_lShapesCustomRenderOrder.Count; i++)
					m_lShapesCustomRenderOrder[i].Show();

                sgl.POPATT();
			}
		}

		public void DrawBoundingBoxes()
		{
			for (int j = 0; j < m_lFaceContainingBoundingBoxes.Count; j++)
				m_lFaceContainingBoundingBoxes[j].Draw();

			m_BSPRootBoundingBox.DrawBoxesContainingLeafBoxes();
		}

		public int GetNumBoundingBoxes
		{
			get { return m_lFaceContainingBoundingBoxes.Count; }
		}

		/// <summary>
		/// Return a string representation of the last set of bounding boxes the player was in
		/// </summary>
		/// <returns>ex. "Bounding Box 2 encompassing 25 map faces"</returns>
		public string GetLastBBoxText()
		{
			string sOut = ""; 

			for (int i = 0; i < m_lLastBoxesInside.Count; i++)
			{
				sOut = sOut + m_lLastBoxesInside[i].ToString() + "\n";
			}
			sOut = sOut + "Num Face Containing Boxes Checked: " + m_hUtilBoxes.Count + ", Traversal Count: " + m_nTraversalCounter + "\n";

			return sOut;
		}

		/// <summary>
		/// Return a randomly chosen view point
		/// </summary>
		public Viewpoint GetRandomViewPoint(bool bSpawnpoint)
		{
			Random ran = new Random();
			if (bSpawnpoint)
			{
				if (m_SpawnPoints.Count == 0)
					return null;
				else
					return m_SpawnPoints[ran.Next(0, m_SpawnPoints.Count - 1)];
			}
			else
			{
				if (m_SpecPoints.Count == 0)
					return null;
				else
					return m_SpecPoints[ran.Next(0, m_SpecPoints.Count - 1)];
			}
		}

		/// <summary>
		/// Reads in material and texture data for a shape and stores the textures
		/// </summary>
		/// <param name="sr">stream to map file</param>
		/// <returns>true if successfully read texture data for a shape
		/// false means there are no more valid shapes to read</returns>
		bool Read(List<Texture> lTextureObjects)
		{
			string sKey, sURL, sDEForUSE = "";
			string[] textureTokens = null;

			bool bFoundMultitexture = stringhelper.LookFor(m_srMapReader, m_sMultitextureHeader, ref m_nMapFileLineCounter);
			bool bFoundChannelOneTexture = false;
			
			if(bFoundMultitexture) {
				sDEForUSE = m_srMapReader.ReadLine();
				m_nMapFileLineCounter++;
			}
			else {
				bFoundChannelOneTexture = stringhelper.FindFirstOccurence(m_srMapReader, m_sChannelOneTextureDef, ref sDEForUSE, ref m_nMapFileLineCounter);
			}

			if (bFoundChannelOneTexture || bFoundMultitexture)
			{
				if (sDEForUSE.Contains("DEF"))
				{
					textureTokens = stringhelper.Tokenize(sDEForUSE, ' ');

					if (bFoundMultitexture)
						sKey = textureTokens[1];
					else
						sKey = textureTokens[2];

					sURL = stringhelper.Tokenize(m_srMapReader.ReadLine(), '"')[1];
					m_nMapFileLineCounter++;
					if (sURL != "null.png")
					{
						m_textureObjects.Add(sKey, new Texture(sURL));
						LOGGER.Debug("Add texture " + sKey);
						lTextureObjects.Add(m_textureObjects[sKey]);                        
					}
					m_srMapReader.ReadLine(); // eat close bracket line
					m_nMapFileLineCounter++;
				}
				else if (sDEForUSE.Contains("USE"))
				{
					sKey = stringhelper.Tokenize(sDEForUSE, ' ')[1];
					LOGGER.Info("Use texture " + sKey);
					lTextureObjects.Add(m_textureObjects[sKey]);
				}
				sDEForUSE = m_srMapReader.ReadLine();
				m_nMapFileLineCounter++;
				if (sDEForUSE.Contains("DEF"))
				{
					textureTokens = stringhelper.Tokenize(sDEForUSE, ' ');
					sKey = textureTokens[1];
					sURL = stringhelper.Tokenize(m_srMapReader.ReadLine(), '"')[1];
					LOGGER.Debug("Add texture " + sKey);
					m_nMapFileLineCounter++;
					m_textureObjects.Add(sKey, new Texture(sURL));
					lTextureObjects.Add(m_textureObjects[sKey]);
				}
				else if (sDEForUSE.Contains("USE"))
				{
					sKey = stringhelper.Tokenize(sDEForUSE, ' ')[1];
					LOGGER.Debug("Use texture " + sKey);
					lTextureObjects.Add(m_textureObjects[sKey]);
				}

				return true;
			}
			else 
				return false;
		}

		public override void Update(Change pChange)
		{
			if(pChange.GetCode == (int)Shape.ESignals.FACE_CREATED) {
				Notify((int)Shape.ESignals.FACE_CREATED);
			}
		}

		public void Delete()
		{
			foreach(Shape s in m_lShapes) 
			{
				s.Delete();
			}

            foreach (Shape s in m_lShapesCustomRenderOrder)
            {
                s.Delete();
            }

            if (m_fonter != null) m_fonter.Delete();
		}

		private static bool TextureShouldRenderAtEnd(string sTexturePath)
		{
			return sTexturePath.Contains("models");
		}

        private static int CompareShapes(Shape s1, Shape s2)
        {
			if (s1 == null) return -1;
			if (s2 == null) return 1;

			return s1.GetRenderOrder().CompareTo(s2.GetRenderOrder());
        }

        private static int CompareShapesByCamDistance(Shape s1, Shape s2)
        {
            if (s1 == null) return -1;
            if (s2 == null) return 1;

			D3Vect disOne = s1.GetMidpoint() - GameGlobals.m_CamPosition;
			D3Vect disTwo = s2.GetMidpoint() - GameGlobals.m_CamPosition;

			return disTwo.Length.CompareTo(disOne.Length);
		}

		private void HandleSubShapes(Shape s)
		{
			// for example for teleporters in ctf space
			// divide up shape into sub shapes, one for each teleporter(object). then I can control render order.
			for (int i = 0; i < s.GetSubShapes().Count; i++)
			{
				Shape sSubShape;
				if (GameGlobals.IsPortalEntry(s.GetMainTexture().GetPath()))
				{
					sSubShape = new Portal(s);
					Portal p = (Portal)(sSubShape);

					DefinePortal(p, i);
				}
				else sSubShape = new Shape(s);

				sSubShape.SetSubShape(true);
				Subscribe(sSubShape);

				sSubShape.SetCoordIndices(s.GetSubShapes()[i]);
				sSubShape.CreateFaces(m_lMapFaceReferences);

				if (sSubShape is Portal) m_lShapes.Add(sSubShape);
				else m_lShapesCustomRenderOrder.Add(sSubShape);
			}
		}

		/// <summary>
		/// Reads a VRML 2.0 compliant file and creates each shape
		/// specified within the file.
		/// </summary>
		/// <param m_DisplayName="file">GetPath to a VRML 2.0 compliant file</param>
		public void Read(MapInfo map)
		{
			m_map = map;

			m_nMapFileLineCounter = 0;

			m_lMapFaceReferences = new List<Face>();

			List<Texture> lShapeTextureObjects = new List<Texture>();
            m_srMapReader = new StreamReader(m_map.GetMapPathOnDisk);
			while (Read(lShapeTextureObjects))
			{
				Shape s = new Shape();

				//LOGGER.Debug("Create shape");
				Subscribe(s);

				s.ReadMain(lShapeTextureObjects, m_srMapReader, m_lMapFaceReferences, ref m_nMapFileLineCounter);

				Notify((int)ESignals.SHAPE_READ); // do this once is fine i think
				if (s.GetSubShapes().Count > 0)
				{
					Unsubscribe(s, true);

					for(int i = 0; i < s.GetMapFaces().Count; i++)
					{
						m_lMapFaceReferences.Remove(s.GetMapFaces()[i]); // clear these out
					}

					HandleSubShapes(s);
				}
				else
				{
					m_lShapes.Add(s);
				}

				lShapeTextureObjects.Clear();
			}

			//LOGGER.Info("Read in " + m_lShapes.Count.ToString() + " shapes for " + map.GetMapPathOnDisk);

			ReadEntities();
		}

		private void PutFaceContainingBoxesInLeafBSPBoxes()
		{
			int nMaxBoxes = m_lLeafBoundingBoxes.Count;
			int nProcCount = Environment.ProcessorCount;
			m_nThreadShutdownCounter = nProcCount;

			int nBoxesPerThread = nMaxBoxes / nProcCount;
			int nRemainder = nMaxBoxes % nProcCount;
			List<int> lBoxCountsPerThread = new List<int>();
			for (int i = 0; i < nProcCount; i++)
			{
				lBoxCountsPerThread.Add(nBoxesPerThread);
			}
			for (int i = 0; i < nRemainder; i++)
			{
				lBoxCountsPerThread[i] = lBoxCountsPerThread[i] + 1;
			}

			int nStartIndex = 0;
			for (int i = 0; i < nProcCount; i++)
			{
				BackgroundWorker bw = new BackgroundWorker();
				bw.DoWork += workerthread_AddFaceBoxesToLeafBoundingBoxes;
				bw.RunWorkerCompleted += mainthread_FinishedAddingFacesToBoundingBoxes;
				bw.RunWorkerAsync(new KeyValuePair<int, int>(nStartIndex, nStartIndex + lBoxCountsPerThread[i]));
				nStartIndex += lBoxCountsPerThread[i];
			}

			while (true)
			{
				m_mutThreadShutdownChecker.WaitOne();
				if (m_nThreadShutdownCounter == 0)
				{
					m_mutThreadShutdownChecker.ReleaseMutex();
					break;
				}
				else
				{
					m_mutThreadShutdownChecker.ReleaseMutex();
					Thread.Sleep(1000);
				}
			}
        }

		/// <summary>
		/// Do precalculations and create and initialize bounding boxes
		/// </summary>
		public void Initialize()
		{
			// Done every time ***
			CreateFaceContainingBBoxes();

            // create bsp bounding boxes. start with dividing the outer one in half and putting the appropriate leaf bounding boxes
            // in each half by doing a box to box intersection test
            m_lLeafBoundingBoxes.Clear();
            DefineBSPBoundingBoxes(m_BSPRootBoundingBox);
			for (int i = 0; i < m_lLeafBoundingBoxes.Count; i++)
				m_lLeafBoundingBoxes[i].UnPrunedIndex = i;
			// ***

            bool bReadIn = ReadBoundingBoxDefinitions();

			if (!bReadIn)
			{
				InitializeBoundingBoxes();
				PutFaceContainingBoxesInLeafBSPBoxes();

                PruneBSPLeafBoxes();
                PruneFaceContainingBoxes();

                ExportBoundingBoxDefinition();
			}
			else
			{
                PruneBSPLeafBoxes();
                PruneFaceContainingBoxes();

                m_srMapReader.Close();
			}
		}

		private void PruneFaceContainingBoxes()
		{
			int nMax = m_lFaceContainingBoundingBoxes.Count;
			int nCurIndex = 0;
			for(int i = 0; i < nMax; i++)
			{
				if (m_lFaceContainingBoundingBoxes[nCurIndex].GetNumMapFaces == 0)
				{
					m_lFaceContainingBoundingBoxes.RemoveAt(nCurIndex);
				}
				else
				{
					m_lFaceContainingBoundingBoxes[nCurIndex].PrunedIndex = nCurIndex;
					nCurIndex++;
				}
			}
		}

		private void PruneBSPLeafBoxes()
		{
			int nMax = m_lLeafBoundingBoxes.Count;
			int nCurIndex = 0;
			for (int i = 0; i < nMax; i++)
			{
				if (m_lLeafBoundingBoxes[nCurIndex].SizeFaceContainingBoxes() == 0)
				{
					m_lLeafBoundingBoxes.RemoveAt(nCurIndex);
				}
				else
				{
					m_lLeafBoundingBoxes[nCurIndex].PrunedIndex = nCurIndex;
					nCurIndex++;
				}
			}
		}

		public void InitializeLists()
		{
			m_fonter = new BasicFont();

			// thread shape inits below
			foreach(Shape s in m_lShapes)
			{
				s.InitializeLists();

				m_mutProgress.WaitOne();
				m_nInitializeProgress++;				
				m_mutProgress.ReleaseMutex();

				Notify(s.GetQ3Shader().GetShaderName(), (int)ESignals.SHAPE_READ);
			}

            foreach (Shape s in m_lShapesCustomRenderOrder)
            {
                s.InitializeLists();

                m_mutProgress.WaitOne();
                m_nInitializeProgress++;
                m_mutProgress.ReleaseMutex();

                Notify(s.GetQ3Shader().GetShaderName(), (int)ESignals.SHAPE_READ);
            }

            m_lShapes.Sort(CompareShapes);
			// will sort m_lShapesCustomRenderOrder every frame
		}

		/// <summary>
		/// Return whether the map file has up to date bounding box definitions already
		/// </summary>
		/// <returns>true if up to date</returns>
		public bool CheckBoundingBoxes(MapInfo map)
		{
			int nDummy = 0;
			bool bUpToDate = false;

			if(!File.Exists(map.GetMapPathOnDisk))
				throw new Exception("Can't find map at location " + map.GetMapPathOnDisk);

			StreamReader sr = new StreamReader(map.GetMapPathOnDisk);
			if (stringhelper.LookFor(sr, m_sBoundingBoxHeader, ref nDummy))
			{
				int nReadVersion = -1, nReadCount = -1, nReadNumSides = -1;
				GetBoxHeaderData(ref nReadVersion, ref nReadCount, ref nReadNumSides, sr);

				if(nReadVersion == m_nBoundingBoxVersion && nReadNumSides == (int)m_dBoundingBoxDimension) {
					bUpToDate = true;
				}
			}

			sr.Close();

			m_bUpToDateBoundingBoxes = bUpToDate;
			return m_bUpToDateBoundingBoxes;
		}

		private bool ReadBoundingBoxDefinitions()
		{
			bool bSuccessfullyRead = false;

			// read face containing boxes
			if (stringhelper.LookFor(m_srMapReader, m_sBoundingBoxHeader, ref m_nMapFileLineCounter))
			{
				int nReadVersion = -1, nReadCount = -1, nReadNumSides = -1;
				GetBoxHeaderData(ref nReadVersion, ref nReadCount, ref nReadNumSides, m_srMapReader);

				if(nReadVersion != m_nBoundingBoxVersion || nReadNumSides != m_dBoundingBoxDimension) {
					return bSuccessfullyRead;
				}

				// Read in bounding box data
				for(int i = 0; i < nReadCount; i++)
				{
					BoundingBox.Read(m_srMapReader, m_lFaceContainingBoundingBoxes, m_lMapFaceReferences, ref m_nMapFileLineCounter, i);	
				}
				bSuccessfullyRead = true;				
			}
			if (bSuccessfullyRead)
			{
				if (stringhelper.LookFor(m_srMapReader, m_sBoundingBoxHeaderBSPLeaf, ref m_nMapFileLineCounter))
				{
					int nReadVersion = -1, nReadCount = -1, nReadNumSides = 0;
					GetBoxHeaderData(ref nReadVersion, ref nReadCount, ref nReadNumSides, m_srMapReader);

					// Read in bounding box data
					for (int i = 0; i < nReadCount; i++)
					{
						BoundingBox.Read(m_srMapReader, m_lLeafBoundingBoxes, m_lMapFaceReferences, ref m_nMapFileLineCounter, i);
					}                    
                }
			}

            return bSuccessfullyRead;	
		}

		private void GetBoxHeaderData(ref int nVersion, ref int nCount, ref int nNumSides, StreamReader sr)
		{
			string s = sr.ReadLine();
			m_nMapFileLineCounter++;
			string[] ps = stringhelper.Tokenize(s, ' ');
			nVersion = Convert.ToInt32(ps[1]);

			s = sr.ReadLine();
			m_nMapFileLineCounter++;
			ps = stringhelper.Tokenize(s, ' ');
			nCount = Convert.ToInt32(ps[1]);

			if (nNumSides == -1)
			{
				s = sr.ReadLine();
				m_nMapFileLineCounter++;
				ps = stringhelper.Tokenize(s, ' ');
				nNumSides = Convert.ToInt32(ps[1]);
			}
		}

		/// <summary>
		/// Write map minus bounding box data to temp file
		/// </summary>
		/// <returns>path to temp file</returns>
		private string WriteMapToTempFileWithoutBoxData()
		{
			StreamReader sr = new StreamReader(m_map.GetMapPathOnDisk);
			string sFileBlock = "";
			int nCounter = 0;
			List<string> lMapStorage = new List<string>();
			string sLine = sr.ReadLine();
			while (!sLine.Contains(m_sBoundingBoxHeader))
			{
				sFileBlock = sFileBlock + sLine + "\n";
				if(nCounter != 0 && nCounter % 1000 == 0) {
					lMapStorage.Add(sFileBlock);
					sFileBlock = "";
				}
				sLine = sr.ReadLine();
				nCounter++;
			}
			if (sFileBlock != "") lMapStorage.Add(sFileBlock);
			sr.Close();

			string sTempFile = Path.GetTempFileName();
			StreamWriter sw = new StreamWriter(sTempFile);
			foreach (string s in lMapStorage)
			{
				sw.Write(s);
			}
			sw.Close();
			return sTempFile;
		}

		private void ExportBoundingBoxDefinition()
		{
			m_srMapReader.Close();

			if (m_lFaceContainingBoundingBoxes.Count > 0 && m_map.ExtractedFromZip)
			{
				string sTempPath = "";
				StreamReader sr = new StreamReader(m_map.GetMapPathOnDisk);
				int nLineCounter = 0;
				if (stringhelper.LookFor(sr, m_sBoundingBoxHeader, ref nLineCounter)) 
				{
					sr.Close();
					sTempPath = WriteMapToTempFileWithoutBoxData();
				}
				else				
					sr.Close();

				StreamWriter sw = null;
				if(sTempPath == "") {
					sw = new StreamWriter(m_map.GetMapPathOnDisk, true);
					sw.WriteLine();
				}
				else
					sw = new StreamWriter(sTempPath, true);

				// Write header
				sw.WriteLine(m_sBoundingBoxHeader);
				sw.WriteLine("version " + Convert.ToString(m_nBoundingBoxVersion));
				sw.WriteLine("count " + Convert.ToString(m_lFaceContainingBoundingBoxes.Count));
				sw.Write("dimension " + Convert.ToString(m_dBoundingBoxDimension));

				// Write face containing box data
				int nCounter = 0;
				int nMax = m_lFaceContainingBoundingBoxes.Count;
				while (nCounter < nMax)
				{
					sw.WriteLine();
					m_lFaceContainingBoundingBoxes[nCounter].Write(sw);
					nCounter++;
				}
				sw.Write("\n}\n");

                // Write header
                sw.WriteLine(m_sBoundingBoxHeaderBSPLeaf);
                sw.WriteLine("version " + Convert.ToString(m_nBoundingBoxVersion));
                sw.WriteLine("count " + Convert.ToString(m_lLeafBoundingBoxes.Count));

                // Write bsp leaf boxes
                nCounter = 0;
                nMax = m_lLeafBoundingBoxes.Count;
                while (nCounter < nMax)
                {
                    if(nCounter > 0) sw.WriteLine();
					m_lLeafBoundingBoxes[nCounter].Write(sw);
                    nCounter++;
                }
                sw.Write("\n}");

                sw.Close();

                if (sTempPath != "")
                {
                    File.Copy(sTempPath, m_map.GetMapPathOnDisk, true);
                    File.Delete(sTempPath);                    
                }

                m_zipper.UpdateMap(m_map.GetMapPathOnDisk, m_map.GetPath);
			}
		}

		public List<Viewpoint> SpecPoints
		{
			get { return m_SpecPoints; }
		}

		public List<Viewpoint> SpawnPoints
		{
			get { return m_SpawnPoints; }
		}

		public List<Viewpoint> ViewPoints
		{
			get
			{
				List<Viewpoint> vps = new List<Viewpoint>(m_SpecPoints);
				vps.AddRange(m_SpawnPoints);
				return vps;
			}
		}

		/// <summary>
		/// Get a list of faces that are intersected by a finite ray
		/// </summary>
		/// <param name="ray">line with start and end point</param>
		/// <param name="lIntersections">faces which ray goes through</param>
		public void IntersectionTest(Edge ray, List<IntersectionInfo> lIntersections)
		{
			foreach(Face f in m_lMapFaceReferences) {
				IntersectionInfo intersection = new IntersectionInfo();
				if(!f.CanMove(ray.Vertice1, ray.Vertice2, intersection, true)) {
					lIntersections.Add(intersection);
				}
			}
		}

		/// <summary>
		/// See which bsp bounding boxes the current ray collider is in.
		/// Then gather the leaf bounding boxes.
		/// </summary>
		public void GatherLeafBoundingBoxesToTest(BoundingBox bContainer, HashSet<BoundingBox> hUtilBoxes)
		{
			if(bContainer.LineInside(m_RayCollider))
			{
				if(bContainer.SizeFaceContainingBoxes() > 0 || bContainer.SizeBSPBoxes() == 0)
				{
					foreach(BoundingBox b in bContainer.GetFaceContainingBoxes())
					{
						hUtilBoxes.Add(b);
					}
				}
				else
				{
					Debug.Assert(bContainer.SizeBSPBoxes() == 2);

					m_nTraversalCounter++;
                    GatherLeafBoundingBoxesToTest(bContainer.GetBSPBoxes()[0], hUtilBoxes);
                    GatherLeafBoundingBoxesToTest(bContainer.GetBSPBoxes()[1], hUtilBoxes);
                }
			}
		}

		/// <summary>
		/// Determine if this figure gets in the way
		/// </summary>
		/// <param name="dest">end of ray to test</param>
		/// <param name="position">start of ray to test</param>
		/// <param name="intersection">the closest intersection</param>
		/// <returns>true if nothing in way</returns>
		public bool CanMove(D3Vect dest, D3Vect position, IntersectionInfo intersection, MovableCamera cam, double dExtraDistanceToCheck,
			MovableCamera.DIRECTION eSourceMovement, double dPlayerHeight)
		{
			// get up, down, left and right vectors from current camera position
			D3Vect d3UpDirection = cam.GetVector(MovableCamera.DIRECTION.UP);
			D3Vect d3DownDirection = cam.GetVector(MovableCamera.DIRECTION.DOWN);
			D3Vect d3LeftDirection = cam.GetVector(MovableCamera.DIRECTION.LEFT);
			D3Vect d3RightDirection = cam.GetVector(MovableCamera.DIRECTION.RIGHT);

			// scale them a bit to give player some size
			d3UpDirection.Scale(mcd_HalfWidth);
			if (eSourceMovement != MovableCamera.DIRECTION.UP && eSourceMovement != MovableCamera.DIRECTION.DOWN)
			{
				// This is to extend the player's size down to near the ground to act as legs.
				// This way the player is not floating 
				// I determined 0.3 experimentally by testing going up stairs and being stopped by the low barrier in 
				// q3dm17
				d3DownDirection.Length = dPlayerHeight - mcd_MaxEasyHopOverHeight;
			}
			else
			{
				d3DownDirection.Scale(mcd_HalfWidth);
			}
			d3LeftDirection.Scale(mcd_HalfWidth);
			d3RightDirection.Scale(mcd_HalfWidth);

			m_ld3Head.Clear();
			m_ld3HeadLookAts.Clear();

			// create points in space around player and including camera position to be used for collision detection
			m_ld3Head.Add(position + (d3LeftDirection + d3UpDirection)); // top left
			m_ld3Head.Add(position + (d3RightDirection + d3UpDirection)); // top right
			m_ld3Head.Add(position + (d3RightDirection + d3DownDirection)); // lower right
			m_ld3Head.Add(position + (d3LeftDirection + d3DownDirection)); // lower left
			m_ld3Head.Add(position); // cam position

			// ray from cam position to dest look at
            m_LookAtRay.x = dest[0] - position[0];
			m_LookAtRay.y = dest[1] - position[1];
			m_LookAtRay.z = dest[2] - position[2];

			if (!m_LookAtRay.Empty)
				m_LookAtRay.Length = m_LookAtRay.Length + dExtraDistanceToCheck;

			// define set of five rays coming out of player in direction they are trying to move
            for (int i = 0; i < m_ld3Head.Count; i++)
			{
				m_ld3HeadLookAts.Add(m_ld3Head[i] + m_LookAtRay);
			}

			m_lLastBoxesInside.Clear();

			bool bCollision = false;

			m_lAllIntersections.Clear();

			// thread this loop. it's very slow i think. when moving forward you do 10 reps of this loop
			// when just standing still you do 5
			// so for moving forward that's 10 you have to wait for. if i thread on my 4 proc laptop that goes
			// down to 4 reps to wait for
			for (int i = 0; i < m_ld3Head.Count; i++)
			{
				m_RayCollider.Vertice1 = m_ld3Head[i];
				m_RayCollider.Vertice2 = m_ld3HeadLookAts[i];

				// use bsp boxes here instead of looping the leafs
				// am I in bsp box this or that?
				// if this then check his bsp boxes and so on until there are no more bsp boxes and instead there are leaf boxes
				// then loop those leaf boxes here 

				m_hUtilBoxes.Clear();
				m_nTraversalCounter = 0;

				// find out which leaf bounding boxes we are in (slow)
				GatherLeafBoundingBoxesToTest(m_BSPRootBoundingBox, m_hUtilBoxes);

				foreach (BoundingBox b in m_hUtilBoxes)
				{
					// slow
					if (b.LineInside(m_RayCollider))
					{
						if (STATE.DebuggingMode)
						{
							if (!m_lLastBoxesInside.Contains(b))
							{
								m_lLastBoxesInside.Add(b);
							}
						}

						// slow
						if (b.IsCollidingWithMapFaces(m_RayCollider, m_lAllIntersections))
						{
							bCollision = true;
							break;
						}
					}
				}
				// I wish I could get out of here when one collision happened but if I do, things get screwed up on stairs at the least.
				// I'm not sure why... It must somehow have to deal with sorting the intersections below but I can't find anyone
				// who cares much about that sorted list. 
				//if (eSourceMovement == MovableCamera.DIRECTION.DOWN && bCollision) break;
			}

			if (bCollision)
			{	
				if(m_lAllIntersections.Count == 0) 
					throw new Exception("Collision true but no intersections");

				foreach (IntersectionInfo i in m_lAllIntersections)
				{
					i.DistanceFromCam = (position - i.Intersection).Length;
				}
				m_lAllIntersections.Sort(Player.CompareIntersectionInfos);
				if (intersection != null)
				{
					intersection.Intersection.Copy(m_lAllIntersections[0].Intersection);
					intersection.DistanceFromCam = m_lAllIntersections[0].DistanceFromCam;
					intersection.Face = m_lAllIntersections[0].Face;
				}
			}

			return !bCollision;
		}

		private void ReadEntities()
		{
			// Read Entities Group
			string sTarget = "Viewpoint { position";
			string sFullLine = "";

			bool bContinue = stringhelper.FindFirstOccurence(m_srMapReader, sTarget, ref sFullLine, ref m_nMapFileLineCounter);
			int nDMCounter = 0, nSpecCounter = 0;
			while (bContinue)
			{
				Viewpoint vp = new Viewpoint();

				string[] tokens = stringhelper.Tokenize(sFullLine, ' ');

				int nPosIndex = stringhelper.FindToken(tokens, "position");
				int nOrIndex = stringhelper.FindToken(tokens, "orientation");

				vp.Position = new D3Vect(Convert.ToDouble(tokens[nPosIndex + 1]),
					Convert.ToDouble(tokens[nPosIndex + 2]),
					Convert.ToDouble(tokens[nPosIndex + 3]));

				List<double> orientation = new List<double>();
				orientation.Add(Convert.ToDouble(tokens[nOrIndex + 1]));
				orientation.Add(Convert.ToDouble(tokens[nOrIndex + 2]));
				orientation.Add(Convert.ToDouble(tokens[nOrIndex + 3]));
				orientation.Add(Convert.ToDouble(tokens[nOrIndex + 4]));
				vp.Orientation = orientation;

				if (stringhelper.FindToken(stringhelper.Tokenize(m_srMapReader.ReadLine(), ' '), "\"info_player_deathmatch\"") >= 0)
				{
					nDMCounter++;
					vp.Name = "DM " + nDMCounter.ToString();
					m_SpawnPoints.Add(vp);
				}
				else
				{
					nSpecCounter++;
					vp.Name = "SPEC " + nSpecCounter.ToString();
					m_SpecPoints.Add(vp);
				}

				m_nMapFileLineCounter++;

				bContinue = stringhelper.FindFirstOccurence(m_srMapReader, sTarget, ref sFullLine, ref m_nMapFileLineCounter);
			}
		}

		/// <summary>
		/// Loop over all m_faces and create bounding boxes
		/// </summary>
		private void CreateFaceContainingBBoxes()
		{
			BoundingBox bBoxAroundAllFaces = new BoundingBox(this);

			// Create bounding box around figure	
			for (int j = 0; j < m_lMapFaceReferences.Count; j++)
			{
                // if face is a sky, continue
                if (m_lMapFaceReferences[j].GetParentShape().IsSky()) continue;

                for (int k = 0; k < m_lMapFaceReferences[j].Count; k++)
				{
					bBoxAroundAllFaces.Update(m_lMapFaceReferences[j][k]);
				}
			}

			bBoxAroundAllFaces.Expand();

			CreateSubBoxes(bBoxAroundAllFaces);

			List<Face> lBoxFacesRef;
			// Create bounding box around face containing bounding boxes	
			for (int j = 0; j < m_lFaceContainingBoundingBoxes.Count; j++)
			{
				lBoxFacesRef = m_lFaceContainingBoundingBoxes[j].GetBoxFaces;
				for (int k = 0; k < lBoxFacesRef.Count; k++)
				{
					for (int l = 0; l < lBoxFacesRef[k].Count; l++)
					{
						m_BSPRootBoundingBox.Update(lBoxFacesRef[k][l]);
					}
				}
			}
        }

		/// <summary>
		/// Create sub bounding boxes. 
		/// </summary>
		/// <param name="OuterBox">Outer bounding box</param>
		private void CreateSubBoxes(BoundingBox OuterBox)
		{
			double totxlen = Math.Abs(OuterBox.GetMaxCorner[0] - OuterBox.GetMinCorner[0]);
			double totylen = Math.Abs(OuterBox.GetMaxCorner[1] - OuterBox.GetMinCorner[1]);
			double totzlen = Math.Abs(OuterBox.GetMaxCorner[2] - OuterBox.GetMinCorner[2]);

			double minx = OuterBox.GetMinCorner[0];
			double miny = OuterBox.GetMinCorner[1];
			double minz = OuterBox.GetMinCorner[2];

			double submaxX;
			double submaxY;
			double submaxZ;

            double dVX = (totxlen / m_dBoundingBoxDimension);
			double dVY = (totylen / m_dBoundingBoxDimension);
			double dVZ = (totzlen / m_dBoundingBoxDimension);

			int nNumBoxesX = dVX % 1 == 0 ? (int)(dVX) : (int)(dVX) + 1;
			int nNumBoxesY = dVY % 1 == 0 ? (int)(dVY) : (int)(dVY) + 1;
			int nNumBoxesZ = dVZ % 1 == 0 ? (int)(dVZ) : (int)(dVZ) + 1;

			double altx;
			double alty;
			double altz;

			// create bounding boxes which contain faces
			int nColorIndexer = 0;
			for (int i = 0; i < nNumBoxesX; i++)
			{
				altx = minx + i * m_dBoundingBoxDimension;
				for (int j = 0; j < nNumBoxesY; j++)
				{
					alty = miny + j * m_dBoundingBoxDimension;
					for (int k = 0; k < nNumBoxesZ; k++)
					{
						altz = minz + k * m_dBoundingBoxDimension;
						submaxX = altx + m_dBoundingBoxDimension;
						submaxY = alty + m_dBoundingBoxDimension;
						submaxZ = altz + m_dBoundingBoxDimension;
						BoundingBox bbox = new BoundingBox(new D3Vect(altx, alty, altz), new D3Vect(submaxX, submaxY, submaxZ), m_lBoxColors[nColorIndexer], this);
						bbox.UnPrunedIndex = i * j * k;
						nColorIndexer++;
						if (nColorIndexer >= m_lBoxColors.Count) nColorIndexer = 0;
						m_lFaceContainingBoundingBoxes.Add(bbox);
					}
				}
			}
		}
		
		/// <summary>
		/// Divide incoming bounding box in half creating two bounding boxes from it. If this is the last division,
		/// put leaf bounding boxes in newly created halfs. Leaf bounding boxes contain map faces.
		/// </summary>
		/// <param name="bContainer"></param>											
		private void DefineBSPBoundingBoxes(BoundingBox bContainer)
		{
			double totxlen;
			double totylen;
			double totzlen;

            totxlen = Math.Abs(bContainer.GetMaxCorner[0] - bContainer.GetMinCorner[0]);
            totylen = Math.Abs(bContainer.GetMaxCorner[1] - bContainer.GetMinCorner[1]);
            totzlen = Math.Abs(bContainer.GetMaxCorner[2] - bContainer.GetMinCorner[2]);

            int nLongest = 0;
			if (totylen >= totxlen && totylen >= totzlen) nLongest = 1;
			else if (totzlen >= totxlen && totzlen >= totylen) nLongest = 2;

			double dLongestMidpoint = 0;
			if (nLongest == 0) dLongestMidpoint = totxlen / 2;
			else if (nLongest == 1) dLongestMidpoint = totylen / 2;
			else dLongestMidpoint = totzlen / 2;

			double dNumLeafBoxesInNewBoundingBox = dLongestMidpoint / m_dBoundingBoxDimension;
			double dWidthOfNewBox = dNumLeafBoxesInNewBoundingBox * m_dBoundingBoxDimension;

			D3Vect d3B1Max = new D3Vect();
			D3Vect d3B2Min = new D3Vect();

			if (nLongest == 0) {
				d3B1Max.SetXYZ(bContainer.GetMinCorner.x + dWidthOfNewBox, bContainer.GetMaxCorner.y, bContainer.GetMaxCorner.z);
				d3B2Min.SetXYZ(bContainer.GetMinCorner.x + dWidthOfNewBox, bContainer.GetMinCorner.y, bContainer.GetMinCorner.z);
			}
			else if (nLongest == 1) {
				d3B1Max.SetXYZ(bContainer.GetMaxCorner.x, bContainer.GetMinCorner.y + dWidthOfNewBox, bContainer.GetMaxCorner.z);
				d3B2Min.SetXYZ(bContainer.GetMinCorner.x, bContainer.GetMinCorner.y + dWidthOfNewBox, bContainer.GetMinCorner.z);
			}
			else {
				d3B1Max.SetXYZ(bContainer.GetMaxCorner.x, bContainer.GetMaxCorner.y, bContainer.GetMinCorner.z + dWidthOfNewBox);
				d3B2Min.SetXYZ(bContainer.GetMinCorner.x, bContainer.GetMinCorner.y, bContainer.GetMinCorner.z + dWidthOfNewBox);
			}			

			BoundingBox b1 = new BoundingBox(bContainer.GetMinCorner, d3B1Max, new Color(255, 255, 255), this);
			b1.SetParent(bContainer);
			BoundingBox b2 = new BoundingBox(d3B2Min, bContainer.GetMaxCorner, new Color(255, 255, 255), this);
			b2.SetParent(bContainer);

			bContainer.AddBSPBox(b1);
			bContainer.AddBSPBox(b2);

			int nNumTest = 0;
			b1.GetNumAncestors(ref nNumTest);

			if(b1.GetVolume <= (9.0 * Math.Pow(m_dBoundingBoxDimension, 3.0)))
			{
				m_lLeafBoundingBoxes.Add(b1);
				m_lLeafBoundingBoxes.Add(b2);				
			}
			else
			{
				DefineBSPBoundingBoxes(b1);
				DefineBSPBoundingBoxes(b2);
			}
		}

		private void workerthread_AddFacesToBoundingBoxes(object sender, DoWorkEventArgs e)
		{
			KeyValuePair<int, int> kvp = (KeyValuePair<int, int>)e.Argument;
			for (int i = kvp.Key; i < kvp.Value; i++)
			{
				// For each face in the map, try to add it to bbox[nCounter]
				for (int j = 0; j < m_lMapFaceReferences.Count; j++)
				{
					// this is the function which when done many times is slow
					m_lFaceContainingBoundingBoxes[i].AddFace(m_lMapFaceReferences[j], true);
				}
				m_lFaceContainingBoundingBoxes[i].UnPrunedIndex = i; // gotta do this somewhere
			}
		}

        private void workerthread_AddFaceBoxesToLeafBoundingBoxes(object sender, DoWorkEventArgs e)
        { 
            KeyValuePair<int, int> kvp = (KeyValuePair<int, int>)e.Argument;
            for (int i = kvp.Key; i < kvp.Value; i++) // for each bsp leaf box
            {
				for (int j = 0; j < m_lFaceContainingBoundingBoxes.Count; j++) // for each face containing box
				{
					if (m_lLeafBoundingBoxes[i].IntersectsOrContains(m_lFaceContainingBoundingBoxes[j]))
					{
						m_lLeafBoundingBoxes[i].AddFaceContainingBox(m_lFaceContainingBoundingBoxes[j]);
					}
				}
            }
        }

        private void mainthread_FinishedAddingFacesToBoundingBoxes(object sender, RunWorkerCompletedEventArgs e)
        {
            m_mutThreadShutdownChecker.WaitOne();
            m_nThreadShutdownCounter--;
            m_mutThreadShutdownChecker.ReleaseMutex();
        }

        /// <summary>
        /// Puts faces inside the bounding boxes. Remove bounding boxes with zero faces.
        /// This is a very costly function. For each bounding box try to add every face.
        /// 
        /// Thread this function. 
        /// </summary>
        private void InitializeBoundingBoxes()
		{
			int nMaxBoxes = m_lFaceContainingBoundingBoxes.Count;
			int nProcCount = Environment.ProcessorCount;
			m_nThreadShutdownCounter = nProcCount;

			int nBoxesPerThread = nMaxBoxes / nProcCount;
			int nRemainder = nMaxBoxes % nProcCount;
			List<int> lBoxCountsPerThread = new List<int>();
			for(int i = 0; i < nProcCount; i++)
			{
				lBoxCountsPerThread.Add(nBoxesPerThread);
			}
			for(int i = 0; i < nRemainder; i++)
			{
				lBoxCountsPerThread[i] = lBoxCountsPerThread[i] + 1;
			}

			int nStartIndex = 0;
			for(int i = 0; i < nProcCount; i++)
			{
				BackgroundWorker bw = new BackgroundWorker();
				bw.DoWork += workerthread_AddFacesToBoundingBoxes;
				bw.RunWorkerCompleted += mainthread_FinishedAddingFacesToBoundingBoxes;
				bw.RunWorkerAsync(new KeyValuePair<int, int>(nStartIndex, nStartIndex + lBoxCountsPerThread[i]));
				nStartIndex += lBoxCountsPerThread[i];
			}
			
			while(true)
			{
				m_mutThreadShutdownChecker.WaitOne();
				if(m_nThreadShutdownCounter == 0)
				{
					m_mutThreadShutdownChecker.ReleaseMutex();
					break;
				}
				else
				{
					m_mutThreadShutdownChecker.ReleaseMutex();
					Thread.Sleep(1000);
				}
			}
        }

		private void DefinePortal(Portal p, int i)
		{
			// there's only one portal in all of these maps so don't need ot use index i
			double dZOffset = .3f;

			if(m_map.GetNick == "q3dm0")
			{
                p.D3TargetLocation = new D3Vect(-4.1, 7, 1.1 + dZOffset);
                p.PHI = 90;
                p.Theta = 270;
            }
			else if(m_map.GetNick == "q3dm7")
			{
                p.D3TargetLocation = new D3Vect(-24.2, 53, 1.2 + dZOffset);
                p.PHI = 90;
				p.Theta = -90;
            }
			else if(m_map.GetNick == "q3dm11")
			{
                p.D3TargetLocation = new D3Vect(70.9, 51.6, -.18 + dZOffset);
                p.PHI = 90;
                p.Theta = -44;
            }
		}
	}
}
