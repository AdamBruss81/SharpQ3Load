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
using Tao.OpenGl;
using Tao.Platform.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using utilities;
using gl_font;
using obsvr;
using engine;

namespace engine
{
	/// <summary>
	/// A collection of Shapes make up a single figure that is displayed.
	/// </summary>
	public class Figure : Communicator
	{
		public enum ESignals { SHAPE_READ = SignalStarts.g_nFigureStart, BOUNDINGBOX_INITIALIZED };

		private const int m_nNumSides = 6;
		private const int m_nBoundingBoxVersion = 1;

		private const string m_sBoundingBoxHeader = "DEF BoundingBoxes {";
		private const string m_sMultitextureHeader = "texture MultiTexture { materialColor TRUE texture [";
		private const string m_sChannelOneTextureDef = "texture DEF";

		protected List<Shape> m_lShapes = new List<Shape>();
		private List<BoundingBox> m_lbboxes = new List<BoundingBox>();
		private List<Viewpoint> m_SpecPoints = new List<Viewpoint>();
		private List<Viewpoint> m_SpawnPoints = new List<Viewpoint>();
		protected List<Face> m_lMapFaceReferences = null;
        List<BoundingBox> m_lLastBoxesInside = new List<BoundingBox>();
		List<Color> m_lBoxColors = new List<Color>();
		List<IntersectionInfo> m_lAllIntersections = new List<IntersectionInfo>();
		Dictionary<string, Texture> m_textureObjects = new Dictionary<string, Texture>();
		protected StreamReader m_srMapReader = null;

		private Edge m_RayCollider = new Edge();

		private MapInfo m_map = new MapInfo();
		private Zipper m_zipper = new Zipper();

		private int m_figureID = -1;
		private int m_nFaceCreationCounter = 0;
		private int m_nInitializingBoundingBoxCounter = 0;
		private int m_nMaxBoundingBoxes = 0;
		private int m_nInitializeProgress = 0;

		private bool m_bUpToDateBoundingBoxes = false;

		private List<D3Vect> m_ld3Head = new List<D3Vect>();
		private List<D3Vect> m_ld3HeadLookAts = new List<D3Vect>();
        private D3Vect m_LookAtRay = new D3Vect();
		private D3Vect m_LeftSideRay = new D3Vect();
		private D3Vect m_RightSideRay = new D3Vect();

		private BasicFont m_fonter = null;

		private int m_nMapFileLineCounter = 0;

		/// <summary>
		/// Figure that will be placed in a "dynamicList" must have a
		/// m_figureID set to a valid server-wide m_figureID.
		/// </summary>
		public Figure( int figureIDNum )
		{
			m_figureID = figureIDNum;
		}

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
		}

		public List<IntersectionInfo> GetLastIntersections
		{
			get { return m_lAllIntersections; }
		}

		public int GetFigureID 
		{ 
			get { return m_figureID; }
		}

		public int GetNumFaces
		{
			get { return m_lMapFaceReferences.Count; }
		}

		public int GetFaceCreationCounter
		{
			get { return m_nFaceCreationCounter; }
		}

		public int GetInitializingBoundingBoxCounter
		{
			get { return m_nInitializingBoundingBoxCounter; }
		}

		public int GetNumShapes
		{
			get { return m_lShapes.Count; }
		}

		public int GetMaxBoundingBoxes
		{
			get { return m_nMaxBoundingBoxes; }
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
			get { return m_nInitializeProgress; }
		}

		public bool GetUpToDateBoundingBoxes
		{
			get { return m_bUpToDateBoundingBoxes; }
		}

		/// <summary>
		/// Shows all m_lShapes within the figure.
		/// </summary>
		virtual public void Show(Engine.EGraphicsMode mode, ref int nNumFacesRendered, List<Plane> lFrustrum, MovableCamera cam)
		{
			if (mode == Engine.EGraphicsMode.WIREFRAME)
			{
				sgl.PUSHATT(Gl.GL_TEXTURE_BIT | Gl.GL_LINE_BIT);
				Gl.glDisable(Gl.GL_TEXTURE_2D);
				Gl.glLineWidth(1.2f);
				for (int i = 0; i < m_lbboxes.Count; i++)
				{
					if(m_lbboxes[i].InsideFrustrum(lFrustrum))
						m_lbboxes[i].DrawMapFaces(mode, ref nNumFacesRendered);
				}
				sgl.POPATT();
			}
			else
			{
				sgl.PUSHATT(Gl.GL_ALL_ATTRIB_BITS);

				foreach(BoundingBox box in m_lbboxes)
				{
					if (!box.InsideFrustrum(lFrustrum))
					{
						List<Face> lMapFacesPerBox = box.GetMapFaces;
						foreach(Face f in lMapFacesPerBox) {
							f.NumberOfVisibleBoundingBoxes--;
						}
					}						
				}				

				for (int i = 0; i < m_lShapes.Count; i++)
					m_lShapes[i].Show(mode, ref nNumFacesRendered);

				foreach(Face f in m_lMapFaceReferences)
				{
					f.RenderedThisPass = false;
					f.NumberOfVisibleBoundingBoxes = f.NumberOfBoundingBoxHolders;
				}

				sgl.POPATT();
			}

			foreach(Face f in m_lMapFaceReferences)
			{
				f.RenderedThisPass = false;
			}
		}

		public void DrawBoundingBoxes()
		{
			for (int j = 0; j < m_lbboxes.Count; j++)
				m_lbboxes[j].Draw();
		}

		public int GetNumBoundingBoxes
		{
			get { return m_lbboxes.Count; }
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
				sOut = sOut + m_lLastBoxesInside[i].ToString();
				if (i < m_lLastBoxesInside.Count)
					sOut += '\n';
			}

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
		bool Read(List<Texture> lTextureObjects, List<Texture> lSFXTextures)
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
						m_textureObjects.Add(sKey, new Texture(sURL, m_map));
						LOGGER.Debug("Add texture " + sKey);
						lTextureObjects.Add(m_textureObjects[sKey]);

                        // special case for flame texture. make this more modular later
                        if(sURL.Contains("flame1side"))
                        {
                            for(int i = 1; i < 9; i++)
                            {
                                string sNewUrl = System.IO.Path.GetDirectoryName(sURL);
                                sNewUrl += "/flame" + System.Convert.ToString(i) + ".jpg";
                                sNewUrl = sNewUrl.Replace("\\", "/");
                                lSFXTextures.Add(new Texture(sNewUrl, m_map));
                            }
                        }
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
					m_textureObjects.Add(sKey, new Texture(sURL, m_map));
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
				m_nFaceCreationCounter++;
				Notify((int)Shape.ESignals.FACE_CREATED);
			}
		}

		public void Delete()
		{
			foreach(Shape s in m_lShapes) 
			{
				s.Delete();
			}

			foreach(BoundingBox b in m_lbboxes)
			{
				b.Delete();
			}

			if(m_fonter != null) m_fonter.Delete();
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
            List<Texture> lShapeSFXTextures = new List<Texture>();
            m_srMapReader = new StreamReader(m_map.GetMapPathOnDisk);
			while (Read(lShapeTextureObjects, lShapeSFXTextures))
			{
				Shape s = new Shape();
				LOGGER.Debug("Create shape");
				Subscribe(s);
				s.ReadMain(lShapeTextureObjects, lShapeSFXTextures, m_srMapReader, m_lMapFaceReferences, ref m_nMapFileLineCounter);
				Notify((int)ESignals.SHAPE_READ);
				m_lShapes.Add(s);
                lShapeSFXTextures.Clear();
				lShapeTextureObjects.Clear();
			}

			LOGGER.Info("Read in " + m_lShapes.Count.ToString() + " shapes for " + map.GetMapPathOnDisk);

			ReadEntities();
		}

		/// <summary>
		/// Do precalculations and create and initialize bounding boxes
		/// </summary>
		public void Initialize()
		{
			CreateBoundingBoxes();

			if (!ReadBoundingBoxDefinitions())
			{
				InitializeBoundingBoxes();
				ExportBoundingBoxDefinition();
			}
			else
			{
				m_srMapReader.Close();
			}
		}

		public void InitializeLists()
		{
			m_fonter = new BasicFont();

			foreach(Shape s in m_lShapes)
			{
				s.InitializeLists();
				m_nInitializeProgress++;
			}
			foreach(BoundingBox b in m_lbboxes)
			{
				b.InitializeLists();
			}
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

				if(nReadVersion == m_nBoundingBoxVersion && nReadNumSides == m_nNumSides) {
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

			if (stringhelper.LookFor(m_srMapReader, m_sBoundingBoxHeader, ref m_nMapFileLineCounter))
			{
				int nReadVersion = -1, nReadCount = -1, nReadNumSides = -1;
				GetBoxHeaderData(ref nReadVersion, ref nReadCount, ref nReadNumSides, m_srMapReader);

				if(nReadVersion != m_nBoundingBoxVersion || nReadNumSides != m_nNumSides) {
					return bSuccessfullyRead;
				}

				// Read in bounding box data
				for(int i = 0; i < nReadCount; i++)
				{
					BoundingBox.Read(m_srMapReader, m_lbboxes, m_lMapFaceReferences, ref m_nMapFileLineCounter);	
				}
				bSuccessfullyRead = true;

				// Filter out empty bounding boxes
				int nMax = m_lbboxes.Count;
				int nCurIndex = 0;
				for(int i = 0; i < nMax; i++)
				{
					if (m_lbboxes[nCurIndex].GetNumMapFaces == 0)
					{
						m_lbboxes.RemoveAt(nCurIndex);
					}
					else
					{
						m_lbboxes[nCurIndex].Index = nCurIndex;
						Notify((int)ESignals.BOUNDINGBOX_INITIALIZED);
						nCurIndex++;
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

			s = sr.ReadLine();
			m_nMapFileLineCounter++;
			ps = stringhelper.Tokenize(s, ' ');
			nNumSides = Convert.ToInt32(ps[1]);
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

			if (m_lbboxes.Count > 0 && m_map.ExtractedFromZip)
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
				sw.WriteLine("count " + Convert.ToString(m_lbboxes.Count));
				sw.Write("numsides " + Convert.ToString(m_nNumSides));

				// Write box data
				int nCounter = 0;
				int nMax = m_lbboxes.Count;
				while (nCounter < nMax)
				{
					sw.WriteLine();
					m_lbboxes[nCounter].Write(sw);
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

		public void TurnOffDebugging()
		{
			for (int i = 0; i < m_lMapFaceReferences.Count; i++)
				m_lMapFaceReferences[i].DrawSolidColor = false;
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
				if(!f.CanMove(ray.Vertice1, ray.Vertice2, intersection)) {
					lIntersections.Add(intersection);
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
			MovableCamera.DIRECTION eSourceMovement)
		{
			bool bUpOrDown = eSourceMovement == MovableCamera.DIRECTION.UP || eSourceMovement == MovableCamera.DIRECTION.DOWN;

			// get up, down, left and right vectors from current camera position
			D3Vect d3UpDirection = cam.GetVector(MovableCamera.DIRECTION.UP);
			D3Vect d3DownDirection = cam.GetVector(MovableCamera.DIRECTION.DOWN);
			D3Vect d3LeftDirection = cam.GetVector(MovableCamera.DIRECTION.LEFT);
			D3Vect d3RightDirection = cam.GetVector(MovableCamera.DIRECTION.RIGHT);

			// scale them a bit to give player some size
			d3UpDirection.Scale(3.0);
			d3DownDirection.Scale(3.0);
			d3LeftDirection.Scale(3.0);
			d3RightDirection.Scale(3.0);

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

            for (int i = 0; i < m_ld3Head.Count; i++)
			{
				m_ld3HeadLookAts.Add(m_ld3Head[i] + m_LookAtRay);
			}

			for (int i = 0; i < m_lLastBoxesInside.Count; i++)
				m_lLastBoxesInside[i].SetMapFacesToDebugMode(false);

			m_lLastBoxesInside.Clear();

			bool bCollision = false;

			m_lAllIntersections.Clear();
			for (int i = 0; i < m_ld3Head.Count; i++)
			{
				m_RayCollider.Vertice1 = m_ld3Head[i];
				m_RayCollider.Vertice2 = m_ld3HeadLookAts[i];

				for (int j = 0; j < m_lbboxes.Count; j++)
				{
					if (m_lbboxes[j].LineInside(m_RayCollider))
					{
						if (STATE.DebuggingMode)
						{
							if (!m_lLastBoxesInside.Contains(m_lbboxes[j]))
							{
								m_lLastBoxesInside.Add(m_lbboxes[j]);
								if (STATE.ShowDebuggingFaces)
									m_lbboxes[j].SetMapFacesToDebugMode(true);
							}
						}

						if (m_lbboxes[j].IsCollidingWithMapFaces(m_RayCollider, m_lAllIntersections))
							bCollision = true;
					}
				}
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
		private void CreateBoundingBoxes()
		{
            // Create bounding box around figure
			BoundingBox Outer = new BoundingBox();
			for(int j = 0; j < m_lMapFaceReferences.Count; j++)
				for(int k = 0; k < m_lMapFaceReferences[j].Count; k++)
					Outer.Update(m_lMapFaceReferences[j][k]);

			Outer.Expand();

			CreateSubBoxes(Outer);
		}

		/// <summary>
		/// Create sub bounding boxes
		/// </summary>
		/// <param name="OuterBox">Outer bounding box</param>
		private void CreateSubBoxes(BoundingBox OuterBox)
		{
			double totxlen = Math.Abs(OuterBox.GetMaxCorner[0] - OuterBox.GetMinCorner[0]);
			double totylen = Math.Abs(OuterBox.GetMaxCorner[1] - OuterBox.GetMinCorner[1]);
			double totzlen = Math.Abs(OuterBox.GetMaxCorner[2] - OuterBox.GetMinCorner[2]);

			double xlen = totxlen / m_nNumSides;
			double ylen = totylen / m_nNumSides;
			double zlen = totzlen / m_nNumSides;

			double minx = OuterBox.GetMinCorner[0];
			double miny = OuterBox.GetMinCorner[1];
			double minz = OuterBox.GetMinCorner[2];

			double submaxX = 0.0;
			double submaxY = 0.0;
			double submaxZ = 0.0;

			double altx = 0.0;
			double alty = 0.0;
			double altz = 0.0;

			int nColorIndexer = 0;
			for (int i = 0; i < m_nNumSides; i++)
			{
				altx = minx + i * xlen;
				for (int j = 0; j < m_nNumSides; j++)
				{
					alty = miny + j * ylen;
					for (int k = 0; k < m_nNumSides; k++)
					{
						altz = minz + k * zlen;
						submaxX = altx + xlen;
						submaxY = alty + ylen;
						submaxZ = altz + zlen;
						BoundingBox bbox = new BoundingBox(new D3Vect(altx, alty, altz), new D3Vect(submaxX, submaxY, submaxZ), m_lBoxColors[nColorIndexer]);
						nColorIndexer++;
						if (nColorIndexer >= m_lBoxColors.Count) nColorIndexer = 0;
						m_lbboxes.Add(bbox);
					}
				}
			}
		}

		/// <summary>
		/// Puts faces inside the bounding boxes. Remove bounding boxes with zero faces.
		/// </summary>
		private void InitializeBoundingBoxes()
		{
			m_nMaxBoundingBoxes = m_lbboxes.Count;
			int nCurIndex = 0;
			for (m_nInitializingBoundingBoxCounter = 0; m_nInitializingBoundingBoxCounter < m_nMaxBoundingBoxes; m_nInitializingBoundingBoxCounter++)
			{
				// For each face in the map, try to add it to bbox[nCounter]
				for (int j = 0; j < m_lMapFaceReferences.Count; j++)
				{
					m_lbboxes[nCurIndex].AddFace(m_lMapFaceReferences[j], true);
				}
				if (m_lbboxes[nCurIndex].GetNumMapFaces == 0)
				{
					m_lbboxes.RemoveAt(nCurIndex);
				}
				else {
					Notify((int)ESignals.BOUNDINGBOX_INITIALIZED);
					m_lbboxes[nCurIndex].Index = nCurIndex;
					m_lbboxes[nCurIndex].GlobalIndex = m_nInitializingBoundingBoxCounter;

					nCurIndex++;
				}
			}
		}
	}
}
