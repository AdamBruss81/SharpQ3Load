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
using System.Collections.Generic;
using utilities;
using System.IO;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace engine
{
	/// <summary>
	/// Represents a geometric rectangle in space to symbolically divide up a set of m_faces
	/// </summary>
	public class BoundingBox
	{
		List<Face> m_lMapFaces = new List<Face>();
		List<Face> m_lFaces = new List<Face>();
        List<D3Vect> m_lVertices = new List<D3Vect>();
        List<Edge> m_lEdgeLines = new List<Edge>(); // pointers to 12 unique bbox directional lines
		List<BoundingBox> m_lBSPBoxes = new List<BoundingBox>();
		List<BoundingBox> m_lLeafBoxes = new List<BoundingBox>(); 
		D3Vect m_MaxCorner;
		D3Vect m_MinCorner;
		D3Vect m_d3MidPoint = new D3Vect();
		D3Vect m_d3HalfDimensions = new D3Vect();
		Color m_color;
		int m_nIndex;
		int m_nGlobalIndex;
		BoundingBox m_pParent = null;

		static double m_nBOXDRAWOFFSET = 0.01;
		static D3Vect zeroAdj = new D3Vect(-m_nBOXDRAWOFFSET, -m_nBOXDRAWOFFSET, -m_nBOXDRAWOFFSET);
		static D3Vect oneAdj = new D3Vect(-m_nBOXDRAWOFFSET, -m_nBOXDRAWOFFSET, m_nBOXDRAWOFFSET);
		static D3Vect twoAdj = new D3Vect(-m_nBOXDRAWOFFSET, m_nBOXDRAWOFFSET, m_nBOXDRAWOFFSET);
		static D3Vect threeAdj = new D3Vect(-m_nBOXDRAWOFFSET, m_nBOXDRAWOFFSET, -m_nBOXDRAWOFFSET);
		static D3Vect fourAdj = new D3Vect(m_nBOXDRAWOFFSET, m_nBOXDRAWOFFSET, m_nBOXDRAWOFFSET);
		static D3Vect fiveAdj = new D3Vect(m_nBOXDRAWOFFSET, m_nBOXDRAWOFFSET, -m_nBOXDRAWOFFSET);
		static D3Vect sixAdj = new D3Vect(m_nBOXDRAWOFFSET, -m_nBOXDRAWOFFSET, -m_nBOXDRAWOFFSET);
		static D3Vect sevenAdj = new D3Vect(m_nBOXDRAWOFFSET, -m_nBOXDRAWOFFSET, m_nBOXDRAWOFFSET);

		const double m_nEPSILON = 0.001;

		/// <summary>
		/// Initialize corners to default extremes
		/// </summary>
		public BoundingBox()
		{
			m_MaxCorner = new D3Vect(-1000000000, -1000000000, -1000000000);
			m_MinCorner = new D3Vect(1000000000, 1000000000, 1000000000);

			SetCenter();
		}

		/// <summary>
		/// Create bounding box out of two world coordinate positions in space
		/// </summary>
		/// <param name="min">minimum corner</param>
		/// <param name="max">maximum corner</param>
		/// <param name="col">color for bbox draw lines</param>
		public BoundingBox(D3Vect min, D3Vect max, Color col)
		{
			m_MinCorner = min;
			m_MaxCorner = max;

			SetCenter();

			m_color = col;

			double xwidth = Math.Abs(m_MaxCorner[0] - m_MinCorner[0]);
			double ywidth = Math.Abs(m_MaxCorner[1] - m_MinCorner[1]);
			double zwidth = Math.Abs(m_MaxCorner[2] - m_MinCorner[2]);

			// Max face - numbers represent index of vertice in m_lVertices
			// 3------- 0 (max)
			// |		|
			// |		|
			// |		|
			// 2------- 1
			m_lVertices.Add(m_MaxCorner);
			m_lVertices.Add(new D3Vect(m_MaxCorner[0], m_MaxCorner[1], m_MaxCorner[2] - zwidth));
			m_lVertices.Add(new D3Vect(m_MaxCorner[0], m_MaxCorner[1] - ywidth, m_MaxCorner[2] - zwidth));
			m_lVertices.Add(new D3Vect(m_MaxCorner[0], m_MaxCorner[1] - ywidth, m_MaxCorner[2]));

			// Min face - numbers represent index of vertice in m_lVertices
			// 		5-------6 
			// 		|		|
			// 		|		|
			// 		|		|
			//(min) 4-------7
			m_lVertices.Add(m_MinCorner);
			m_lVertices.Add(new D3Vect(m_MinCorner[0], m_MinCorner[1], m_MinCorner[2] + zwidth));
			m_lVertices.Add(new D3Vect(m_MinCorner[0], m_MinCorner[1] + ywidth, m_MinCorner[2] + zwidth));
			m_lVertices.Add(new D3Vect(m_MinCorner[0], m_MinCorner[1] + ywidth, m_MinCorner[2]));

			CreateFaces();
		}

		public void SetParent(BoundingBox b) { m_pParent = b; }

		public void GetNumAncestors(ref int nNum)
		{
			if(m_pParent != null)
			{
				nNum++;
				m_pParent.GetNumAncestors(ref nNum);
			}
		}

		private void DrawFaceNormals()
		{
			foreach(Face face in m_lFaces) {
				face.DrawNormals();
			}
		}

		public void DrawBoxesContainingLeafBoxes()
		{
			if(SizeLeafBoxes() > 0 || m_lBSPBoxes.Count == 0)
			{
				Draw();
			}
			else
			{
				Debug.Assert(m_lBSPBoxes.Count == 2);
                m_lBSPBoxes[0].DrawBoxesContainingLeafBoxes();
                m_lBSPBoxes[1].DrawBoxesContainingLeafBoxes();
            }
		}

		public void Contract()
		{
            m_MinCorner[0] += m_nEPSILON;
            m_MinCorner[1] += m_nEPSILON;
            m_MinCorner[2] += m_nEPSILON;

            m_MaxCorner[0] -= m_nEPSILON;
            m_MaxCorner[1] -= m_nEPSILON;
            m_MaxCorner[2] -= m_nEPSILON;

            SetCenter();
        }

		/// <summary>
		/// Increase size of box by a small amount
		/// </summary>
		public void Expand()
		{
			m_MinCorner[0] -= m_nEPSILON;
			m_MinCorner[1] -= m_nEPSILON;
			m_MinCorner[2] -= m_nEPSILON;

			m_MaxCorner[0] += m_nEPSILON;
			m_MaxCorner[1] += m_nEPSILON;
			m_MaxCorner[2] += m_nEPSILON;

			SetCenter();
		}

		public void AddBSPBox(BoundingBox b)
		{
			m_lBSPBoxes.Add(b);
		}

		public int SizeBSPBoxes() { return m_lBSPBoxes.Count; }

		public List<BoundingBox> GetBSPBoxes() { return m_lBSPBoxes; }

		public int SizeLeafBoxes() { return m_lLeafBoxes.Count; }

		public List<BoundingBox> GetLeafBoxes() { return m_lLeafBoxes; }

        public void AddLeafBox(BoundingBox b)
        {
            m_lLeafBoxes.Add(b);
        }

        /// <summary>
        /// Returns if this bounding box intersects in anyway b
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool IntersectsOrContains(BoundingBox b)
		{
			bool bIntersection = false;

			for(int i = 0; i < b.m_lEdgeLines.Count; i++)
			{
				if(LineInside(b.m_lEdgeLines[i]))
				{
					bIntersection = true;
					break;
				}
			}

			return bIntersection;
		}

		public double GetVolume
		{
			get { return (m_MaxCorner.x - m_MinCorner.x) * (m_MaxCorner.y - m_MinCorner.y) * (m_MaxCorner.z - m_MinCorner.z); }
		}

		public int GetNumMapFaces
		{
			get { return m_lMapFaces.Count; }
		}

		public List<Face> GetMapFaces
		{
			get { return m_lMapFaces; }
		}

		public List<Face> GetBoxFaces
		{
			get { return m_lFaces; }
		}

		public D3Vect GetMaxCorner
		{
			get { return m_MaxCorner; }
		}

		public int Index
		{
			set {
				m_nIndex = value;
			}
		}

		public int GlobalIndex
		{
			set { m_nGlobalIndex = value; }
			get { return m_nGlobalIndex; }
		}

		public D3Vect GetMinCorner
		{
			get { return m_MinCorner; }
		}

		public void Update(D3Vect point)
		{
			for (int i = 0; i < 3; i++)
			{
				if (point[i] < m_MinCorner[i])
					m_MinCorner[i] = point[i];
				if (point[i] > m_MaxCorner[i])
					m_MaxCorner[i] = point[i];
			}
		}

		public void Draw()
		{
			sgl.PUSHATT(AttribMask.CurrentBit | AttribMask.LineBit);

			bool bAbort = false;

			if (SizeLeafBoxes() > 0)
			{
                GL.Color3(System.Drawing.Color.HotPink);
                GL.LineWidth(5.0f);
            }
			else
			{
				//bAbort = true;
				GL.Color3(m_color.GetColor);
				GL.LineWidth(2.5f);
			}

			if (!bAbort)
			{
				GL.Begin(PrimitiveType.Lines);
				// MAX END
				GL.Vertex3((m_lVertices[0] + zeroAdj).Vect);
				GL.Vertex3((m_lVertices[1] + oneAdj).Vect);
				GL.Vertex3((m_lVertices[1] + oneAdj).Vect);
				GL.Vertex3((m_lVertices[2] + twoAdj).Vect);
				GL.Vertex3((m_lVertices[2] + twoAdj).Vect);
				GL.Vertex3((m_lVertices[3] + threeAdj).Vect);
				GL.Vertex3((m_lVertices[3] + threeAdj).Vect);
				GL.Vertex3((m_lVertices[0] + zeroAdj).Vect);

				// BODY
				GL.Vertex3((m_lVertices[0] + zeroAdj).Vect);
				GL.Vertex3((m_lVertices[6] + sixAdj).Vect);
				GL.Vertex3((m_lVertices[1] + oneAdj).Vect);
				GL.Vertex3((m_lVertices[7] + sevenAdj).Vect);
				GL.Vertex3((m_lVertices[3] + threeAdj).Vect);
				GL.Vertex3((m_lVertices[5] + fiveAdj).Vect);
				GL.Vertex3((m_lVertices[2] + twoAdj).Vect);
				GL.Vertex3((m_lVertices[4] + fourAdj).Vect);

				// MIN END
				GL.Vertex3((m_lVertices[4] + fourAdj).Vect);
				GL.Vertex3((m_lVertices[5] + fiveAdj).Vect);
				GL.Vertex3((m_lVertices[5] + fiveAdj).Vect);
				GL.Vertex3((m_lVertices[6] + sixAdj).Vect);
				GL.Vertex3((m_lVertices[6] + sixAdj).Vect);
				GL.Vertex3((m_lVertices[7] + sevenAdj).Vect);
				GL.Vertex3((m_lVertices[7] + sevenAdj).Vect);
				GL.Vertex3((m_lVertices[4] + fourAdj).Vect);

				GL.End();
			}

            if (STATE.DrawFaceNormals) {
				DrawFaceNormals();
			}

			sgl.POPATT();
		}

		public void DrawMapFaces(Engine.EGraphicsMode mode)
		{
			foreach(Face f in m_lMapFaces) 
			{
				f.Draw(mode);
			}
		}

		/// <summary>
		/// Checks if face intersects with this bounding box. If it does,
		/// add the face to m_lMapFaces for future use in collision detection.
		/// </summary>
		/// <param name="face">Face to check</param>
		/// <returns>True if added face, false otherwise</returns>
		public bool AddFace(Face face, bool bCheck)
		{
			if (!bCheck)
			{
				m_lMapFaces.Add(face);

				face.NumberOfBoundingBoxHolders++;
				return true;
			}
			else
			{
				int nFaceVerticeIndexer = 0;
				int nDirLineIndex = 0;
				int nBoxEdgeIndex = 0;
				bool bAddedFace = false;

				// Test if a face vertice is inside the bounding box
				while (nFaceVerticeIndexer < face.Count && !bAddedFace)
				{
					if (IsPointInside(face[nFaceVerticeIndexer]))
					{
						if (m_lMapFaces.Contains(face))
							throw new System.Exception("Attempted to add same face twice to a Bounding Box");

						m_lMapFaces.Add(face);
						bAddedFace = true;
					}

					nFaceVerticeIndexer++;
				}

				if (bAddedFace)
				{
					face.NumberOfBoundingBoxHolders++;
					return true;
				}

				// Intersect bounding box edges with face
				while (nBoxEdgeIndex < m_lEdgeLines.Count && !bAddedFace)
				{
					if (!face.CanMove(m_lEdgeLines[nBoxEdgeIndex].Vertice1, m_lEdgeLines[nBoxEdgeIndex].Vertice2, null, false))
					{
						if (m_lMapFaces.Contains(face))
							throw new System.Exception("Attempted to add same face twice to a Bounding Box");

						m_lMapFaces.Add(face);
						bAddedFace = true;
					}
					nBoxEdgeIndex++;
				}

				if (bAddedFace)
				{
					face.NumberOfBoundingBoxHolders++;
					return true;
				}

				// Intersect face edges with bounding box faces
				List<Edge> dirLines = face.GetEdges;
				while (nDirLineIndex < dirLines.Count && !bAddedFace)
				{
					if (LineInside(dirLines[nDirLineIndex]))
					{
						if (m_lMapFaces.Contains(face))
							throw new System.Exception("Attempted to add same face twice to a Bounding Box");

						m_lMapFaces.Add(face);
						bAddedFace = true;
					}
					nDirLineIndex++;
				}

				if (bAddedFace) face.NumberOfBoundingBoxHolders++;

				return bAddedFace;
			}
		}

		/// <summary>
		/// Tests whether line intersects with one of the faces of the map that exists in this bounding box
		/// </summary>
		/// <param name="line">the directional line to test</param>
		/// <param name="lIntersections">list of resulting intersections</param>
		/// <returns>true if collision, false otherwise</returns>
		public bool IsCollidingWithMapFaces(Edge line, List<IntersectionInfo> lIntersections)
		{
			IntersectionInfo intersection = new IntersectionInfo();
			bool bCopy;
			bool bAdded = false;

			for (int i = 0; i < m_lMapFaces.Count; i++)
			{
				bCopy = false;
				if (!m_lMapFaces[i].CanMove(line.Vertice1, line.Vertice2, intersection, false))
				{
					foreach(IntersectionInfo ii in lIntersections) 
					{
						if(ii.Face == intersection.Face) 
						{
							bCopy = true;
							break;
						}
					}

					if (!bCopy)
					{
						lIntersections.Add(intersection);
						bAdded = true;
						intersection = new IntersectionInfo();
					}
				}
			}
			return bAdded == true;
		}

        /// <summary>
        /// Return whether line intersects with this bounding box
        /// </summary>
        /// <param name="line">the directional line to test</param>
        /// <returns>true if collision, false otherwise</returns>
        private bool IsCollidingWithBoxFaces(Edge line) 
        {
            for (int i = 0; i < m_lFaces.Count; i++)
            {
				if (!m_lFaces[i].CanMove(line.Vertice1, line.Vertice2, null, false))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Test whether line is inside this bounding box
        /// </summary>
        /// <param name="line">line from MovableCamera position to future MovableCamera position</param>
        /// <returns>true if line is inside bounding box</returns>
		public bool LineInside(Edge line)
		{
			if (IsPointInside(line.Vertice1) || IsPointInside(line.Vertice2) || IsCollidingWithBoxFaces(line))
				return true;
			else
				return false;
		}

		/// <summary>
		/// Returns whether vertice is inside the box formed by max and min corners
		/// </summary>
		/// <param name="vertice">Vertice in world space to test</param>
		/// <returns>true if vertice inside or on, and false if outside</returns>
		private bool IsPointInside(D3Vect vertice)
		{
			if( (vertice[0] >= m_MinCorner[0] && vertice[0] <= m_MaxCorner[0]) &&
				(vertice[1] >= m_MinCorner[1] && vertice[1] <= m_MaxCorner[1]) &&
				(vertice[2] >= m_MinCorner[2] && vertice[2] <= m_MaxCorner[2]) )
				return true;
			else
				return false;
		}

		public override string ToString()
		{
			return "BBOX " + System.Convert.ToString(m_nIndex) + 
				" encompassing " + System.Convert.ToString(m_lMapFaces.Count) + " map faces ";
		}

		private void SetCenter()
		{
			m_d3MidPoint[0] = (m_MaxCorner[0] + m_MinCorner[0]) / 2;
			m_d3MidPoint[1] = (m_MaxCorner[1] + m_MinCorner[1]) / 2;
			m_d3MidPoint[2] = (m_MaxCorner[2] + m_MinCorner[2]) / 2;

			m_d3HalfDimensions[0] = Math.Abs(m_MaxCorner[0] - m_d3MidPoint[0]);
			m_d3HalfDimensions[1] = Math.Abs(m_MaxCorner[1] - m_d3MidPoint[1]);
			m_d3HalfDimensions[2] = Math.Abs(m_MaxCorner[2] - m_d3MidPoint[2]);
		}

		private void CreateFaces()
		{
            Face pFace = null;
			List<D3Vect> lFaceInitializer = new List<D3Vect>();
			double dNormalScale = 0.7;

			// MAX FACE
			lFaceInitializer.Add(m_lVertices[0]);
			lFaceInitializer.Add(m_lVertices[3]);
			lFaceInitializer.Add(m_lVertices[2]);
			lFaceInitializer.Add(m_lVertices[1]);
			pFace = new Face(lFaceInitializer, dNormalScale);
            pFace.GatherUniqueEdges(m_lEdgeLines);
			m_lFaces.Add(pFace);
			lFaceInitializer.Clear();

			// MIN FACE
			lFaceInitializer.Add(m_lVertices[5]);
			lFaceInitializer.Add(m_lVertices[6]);
			lFaceInitializer.Add(m_lVertices[7]);
			lFaceInitializer.Add(m_lVertices[4]);
			pFace = new Face(lFaceInitializer, dNormalScale);
            pFace.GatherUniqueEdges(m_lEdgeLines);
            m_lFaces.Add(pFace);
			lFaceInitializer.Clear();

			// MIN Y SIDE
			lFaceInitializer.Add(m_lVertices[6]);
			lFaceInitializer.Add(m_lVertices[0]);
			lFaceInitializer.Add(m_lVertices[1]);
			lFaceInitializer.Add(m_lVertices[7]);
			pFace = new Face(lFaceInitializer, dNormalScale);
            pFace.GatherUniqueEdges(m_lEdgeLines);
            m_lFaces.Add(pFace);
			lFaceInitializer.Clear();

			// MAX Y SIDE
			lFaceInitializer.Add(m_lVertices[3]);
			lFaceInitializer.Add(m_lVertices[5]);
			lFaceInitializer.Add(m_lVertices[4]);
			lFaceInitializer.Add(m_lVertices[2]);
			pFace = new Face(lFaceInitializer, dNormalScale);
            pFace.GatherUniqueEdges(m_lEdgeLines);
            m_lFaces.Add(pFace);
			lFaceInitializer.Clear();

			// MAX Z SIDE (TOP)
			lFaceInitializer.Add(m_lVertices[0]);
			lFaceInitializer.Add(m_lVertices[6]);
			lFaceInitializer.Add(m_lVertices[5]);
			lFaceInitializer.Add(m_lVertices[3]);
			pFace = new Face(lFaceInitializer, dNormalScale);
            pFace.GatherUniqueEdges(m_lEdgeLines);
            m_lFaces.Add(pFace);
			lFaceInitializer.Clear();

			// MIN Z SIDE (BOTTOM)
			lFaceInitializer.Add(m_lVertices[4]);
			lFaceInitializer.Add(m_lVertices[7]);
			lFaceInitializer.Add(m_lVertices[1]);
			lFaceInitializer.Add(m_lVertices[2]);
			pFace = new Face(lFaceInitializer, dNormalScale);
            pFace.GatherUniqueEdges(m_lEdgeLines);
            m_lFaces.Add(pFace);

			if (m_lFaces.Count != 6) 
				throw new Exception("Bounding box must have 6 sides. Found to have " + m_lFaces.Count.ToString());
			if (m_lEdgeLines.Count != 12) 
				throw new Exception("Bounding box must have 12 edges. Found to have " + m_lEdgeLines.Count.ToString());

#if DEBUG
			for (int i = 0; i < m_lFaces.Count; i++)
			{
				D3Vect normal = m_lFaces[i].GetNewNormal;
				for (int j = 0; j < m_lFaces.Count; j++)
				{
					if (i != j)
					{
                        if (normal == m_lFaces[j].GetNewNormal)
                            throw new System.Exception("Duplicate normal detected for two faces of a bounding box");
					}
				}
			}
#endif // DEBUG
		}

		public void Write(StreamWriter sw)
		{
			int nCounter = 0;
			sw.Write("[" + Convert.ToString(m_nGlobalIndex) + "\n");
			int nNumMapFaces = GetNumMapFaces;
			while(nCounter < nNumMapFaces) 
			{
				sw.Write(Convert.ToString(m_lMapFaces[nCounter].Index));
				if (nCounter % 50 == 0 && nCounter != 0 && nCounter + 1 < nNumMapFaces)
					sw.Write("\n");
				else if(nCounter + 1 < nNumMapFaces) 
					sw.Write(",");

				nCounter++;
			}
			
			sw.Write("\n]");
		}

		public static void Read(StreamReader sr, List<BoundingBox> lBoxes, List<Face> lMapFaces, ref int nLineCounter)
		{
			string sLine = sr.ReadLine(); // ex. "[34"
			nLineCounter++;
			int nGlobalIndex = Convert.ToInt32(sLine.Substring(1));
			int i;
			int nFaceIndex;

			sLine = sr.ReadLine();
			nLineCounter++;
			while(sLine != "]")
			{
				string[] psFaceIndexes = sLine.Split(new Char[] {','});
				for(i = 0; i < psFaceIndexes.Length; i++) 
				{
					nFaceIndex = Convert.ToInt32(psFaceIndexes[i]);
					lBoxes[nGlobalIndex].AddFace(lMapFaces[nFaceIndex], false);
				}
				sLine = sr.ReadLine();
				nLineCounter++;
			}
		}
	}
}
