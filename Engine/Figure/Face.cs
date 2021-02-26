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
using OpenTK.Graphics.OpenGL;

namespace engine
{
	/// <summary>
	/// Open GL set of vertices to be drawn using a display list. The vertices form a closed polygon.
	/// </summary>
    public class Face : Plane
	{
		List<List<DPoint>> m_lTextureCoordinates;
		List<D3Vect> m_lVertices;
		List<D3Vect> m_lVertColors;
		List<int> m_lIndices;
		FaceEdges m_edges = new FaceEdges();
        Shape m_pParentShape = null;
		System.Threading.Mutex canMoveLock = new System.Threading.Mutex();

		D3Vect m_d3IntersectRay = new D3Vect(); // memory placeholder
		D3Vect m_d3Intersection = new D3Vect(); // memory placeholder
		D3Vect m_d3MidPoint = new D3Vect();

        Color m_NormalCapColor = new Color(0, 255, 0);
		Color m_NormalStemColor = new Color(0, 100, 0);

        EPointClassification m_pointclass;
        EPointClassification m_previousClass = EPointClassification.NONE;

        double m_nIntersectAdjuster;
		double m_dVisualNormalScale = 1.0;

		int m_nWireframeDrawList;
		int m_nDrawNormalList;
		int m_nIndex = -1;
		int m_nNumberOfBoundingBoxHolders = 0;		
	
		Shape.ETextureType m_TextureType;

		enum EPointClassification { NONE, PLANE_FRONT, PLANE_BACK, PLANE_COINCIDE };

		/// <summary>
		/// Creates a face from the passed in string.
		/// </summary>
		/// <param m_DisplayName="s">a string of integer indicies which reference the 
		/// corresponding m_verticies used to draw the face.</param>
		public Face( List<D3Vect> lVerts, List<List<DPoint>> lTextCoords, List<D3Vect> lVertColors, Color normalCap, Color normalStem, int index, List<int> lIndices )
		{
			m_TextureType = lTextCoords.Count == 2 ? Shape.ETextureType.MULTI : Shape.ETextureType.SINGLE;
			m_lTextureCoordinates = new List<List<DPoint>>();
			m_lTextureCoordinates.Add(new List<DPoint>(lTextCoords[0]));
			if (m_TextureType == Shape.ETextureType.MULTI)
				m_lTextureCoordinates.Add(new List<DPoint>(lTextCoords[1]));
			m_lVertices = new List<D3Vect>(lVerts);
			m_lVertColors = new List<D3Vect>(lVertColors);
			m_lIndices = new List<int>(lIndices);
			m_NormalCapColor = normalCap;
			m_NormalStemColor = normalStem;
			m_nIndex = index;
			DoFacePrecalculations(false);
		}

		/// <summary>
		/// Create face with vertices. Do vector calculations right away.
		/// This constructor is for manually creating a face.
		/// </summary>
		/// <param name="vertices">vertices to define this face</param>
		public Face(List<D3Vect> vertices, double dVisualNormalScale)
		{
			m_dVisualNormalScale = dVisualNormalScale;
			m_lVertices = new List<D3Vect>(vertices);
			m_TextureType = Shape.ETextureType.NONE;
			DoFacePrecalculations(false);
		}

		public D3Vect GetMidpoint() { return m_d3MidPoint; }

		public uint GetIndice(int i) { return (uint)m_lIndices[i]; }
		public void SetIndices(List<int> lIndices) { m_lIndices = lIndices; } // used for merged shapes when sorting

		/// <summary>
		/// Return the number of vertices in this face.
		/// </summary>
		public int Count { get { return m_lVertices.Count; } }

		public int Index { get { return m_nIndex; } }

		public int NumberOfBoundingBoxHolders
		{
			get { return m_nNumberOfBoundingBoxHolders; }
			set 
			{ 
				m_nNumberOfBoundingBoxHolders = value;
			}
		}

        public void SetParentShape(Shape s)
        {
			m_pParentShape = s;
		}

        public Shape GetParentShape()
        {
            return m_pParentShape;
        }

		public List<Edge> GetEdges { get { return m_edges.EdgeLines; } }
		public List<D3Vect> GetEdgeVectors { get { return m_edges.EdgeVectors; } }
		public List<D3Vect> GetVertices { get { return m_lVertices; }	}

		/// <summary>
		/// Returns the vertice at the specified index 
		/// </summary>
		/// <param name="index">Index into the m_faces list of vertices</param>
		/// <returns>D3Vect containing x,y,z coordinates of the vertice</returns>
		public D3Vect this[int index]
		{
			get { return new D3Vect(m_lVertices[index][0], m_lVertices[index][1], m_lVertices[index][2]); }
		}

		// http://www.gamespp.com/algorithms/collisionDetection.html
		private EPointClassification ClassifyPoint(D3Vect position, D3Vect normal, double distance)
		{
			double dScalar = D3Vect.DotProduct(normal, position) + distance;

			if (dScalar > 0.0) return EPointClassification.PLANE_FRONT;
			else if (dScalar < 0.0) return EPointClassification.PLANE_BACK;
			else return EPointClassification.PLANE_COINCIDE;
		}

		/// <summary>
		/// Returns if a face is inbetween the position point and the destination point
		/// http://www.gamespp.com/algorithms/collisionDetection.html
		/// </summary>      
		public bool CanMove(D3Vect position, D3Vect dest, IntersectionInfo intersection, bool bTestAll)
		{
			if (!bTestAll && m_pParentShape != null && m_pParentShape.NonSolid()) return true;

			bool bCanMove = false;

			canMoveLock.WaitOne();

			if (ClassifyPoint(position, GetNormal, DistanceToOriginAlongNormal) != ClassifyPoint(dest, GetNormal, DistanceToOriginAlongNormal))
			{
				m_d3IntersectRay[0] = dest[0] - position[0];
				m_d3IntersectRay[1] = dest[1] - position[1];
				m_d3IntersectRay[2] = dest[2] - position[2];

				double denom = D3Vect.DotProduct(GetNormal, m_d3IntersectRay);

				if (denom == 0)
				{
					canMoveLock.ReleaseMutex();
					return false; // means infinite number of points on plane
				}

				m_nIntersectAdjuster = -(D3Vect.DotProduct(GetNormal, position) + DistanceToOriginAlongNormal) / denom;

				m_d3IntersectRay[0] = m_d3IntersectRay[0] * m_nIntersectAdjuster;
				m_d3IntersectRay[1] = m_d3IntersectRay[1] * m_nIntersectAdjuster;
				m_d3IntersectRay[2] = m_d3IntersectRay[2] * m_nIntersectAdjuster;

				m_d3Intersection[0] = position[0] + m_d3IntersectRay[0];
				m_d3Intersection[1] = position[1] + m_d3IntersectRay[1];
				m_d3Intersection[2] = position[2] + m_d3IntersectRay[2];

				m_previousClass = EPointClassification.NONE;
				for (int i = 0; i < Count; i++)
				{
					m_pointclass = ClassifyPoint(m_d3Intersection, m_edges.GetInwardNormal(i), m_edges.GetPlaneDistance(i));
					if (m_previousClass != EPointClassification.NONE && m_pointclass != m_previousClass)
					{
						bCanMove = true;
						break;
					}
					m_previousClass = m_pointclass;
				}

				if (!bCanMove && (Object)intersection != null)
				{
					intersection.Intersection.Copy(m_d3Intersection);
					intersection.Face = this;
				}
			}
			else
				bCanMove = true;

			canMoveLock.ReleaseMutex();

			return bCanMove;
		}

		public void InitializeLists()
		{
			if (m_TextureType != Shape.ETextureType.NONE)
			{
				// wireframe
				m_nWireframeDrawList = GL.GenLists(1);
				GL.NewList(m_nWireframeDrawList, ListMode.Compile);
				GL.Begin(PrimitiveType.LineLoop);
				{
					for (int j = 0; j < m_lVertices.Count; j++)
						GL.Vertex3(m_lVertices[j].Vect);
				}
				GL.End();
				GL.EndList();
			}

			m_nDrawNormalList = GL.GenLists(1);
			GL.NewList(m_nDrawNormalList, ListMode.Compile);
			DrawNormals();
			GL.EndList();
		}

		/// <summary>
		/// find the m_d3Normal using two vertices using the right hand rule.
		/// The vertices to be used will be the last two in the list.
		/// </summary>
		private void DoFacePrecalculations(bool bReverseNormal)
		{
			D3Vect d3Sum = new D3Vect();
			foreach(D3Vect d3 in m_lVertices) {
				d3Sum += d3;
			}

			m_d3MidPoint = d3Sum / m_lVertices.Count;

			Initialize(m_lVertices[1] - m_lVertices[0], m_lVertices[2] - m_lVertices[0], bReverseNormal, m_lVertices[0]);

			int nIndex;
			for (int j = 0; j < Count; j++)
			{
				if (j == Count - 1)
					nIndex = 0;
				else
					nIndex = j + 1;

				m_edges.Add(m_lVertices[j], m_lVertices[nIndex], GetNormal);
			}
		}

		public void Delete()
		{
			GL.DeleteLists(m_nWireframeDrawList, 1);
			GL.DeleteLists(m_nDrawNormalList, 1);
		}

		/// <summary>
		/// Draw this face
		/// </summary>
		public void Draw(Engine.EGraphicsMode mode)
		{
            if (mode == Engine.EGraphicsMode.WIREFRAME)
                GL.CallList(m_nWireframeDrawList);

            if (STATE.DebuggingMode && STATE.DrawFaceNormals)
            {
                GL.CallList(m_nDrawNormalList);
            }
        }

		/// <summary>
		/// Draw normal from midpoint of face
		/// </summary>
		public void DrawNormals()
		{
			/*for(int i = 0; i < m_edges.Count; i++)
			{
				DrawNormal(m_edges.GetInwardNormal(i), m_edges.GetMidpoint(i));
			}*/

			DrawNormal(GetNormal, m_d3MidPoint);
		}

		public static void DrawNormalStatic(D3Vect normal, D3Vect startPt, double dVisualNormalScale, Color stemColor, Color capColor)
		{
            // draw line from midpoint to midpoint plus normal
            D3Vect normEnd = startPt + normal * dVisualNormalScale;

            sgl.PUSHATT(AttribMask.CurrentBit | AttribMask.LineBit | AttribMask.TextureBit);

            GL.Disable(EnableCap.Texture2D);
            GL.Color3(stemColor.GetColor);

            sgl.PUSHMAT();
            // http://www.euclideanspace.com/maths/algebra/vectors/angleBetween/index.htm
            D3Vect zup = new D3Vect(0, 0, 1);
            double rotAngle = Math.Acos(D3Vect.DotProduct(normal, zup)) * GLB.RadToDeg;
            if (Math.Abs(rotAngle) < 0.001)
                rotAngle = 0.0;
            D3Vect rotAxis = new D3Vect(zup, normal);
            GL.Translate(startPt[0], startPt[1], startPt[2]);
            GL.Rotate(rotAngle, rotAxis[0], rotAxis[1], rotAxis[2]);
            //Glut.glutSolidCylinder(0.01, (startPt - normEnd).Length, 7, 1);
            sgl.POPMAT();

            GL.Color3(capColor.GetColor);
            sgl.PUSHMAT();
            GL.Translate(normEnd[0], normEnd[1], normEnd[2]);
            GL.Rotate(rotAngle, rotAxis[0], rotAxis[1], rotAxis[2]);
            //Glut.glutSolidCone(0.01, 0.3, 7, 1);
            sgl.POPMAT();

            sgl.POPATT();
        }

		public void DrawNormal(D3Vect normal, D3Vect start)
		{
			DrawNormalStatic(normal, start, m_dVisualNormalScale, m_NormalStemColor, m_NormalCapColor);			
		}		   

		/// <summary>
		/// Fill out lEdges with new edges from this
		/// </summary>
		/// <param name="lEdges">A reference parameter of unique directional lines</param>
        public void GatherUniqueEdges(List<Edge> lEdges, bool bIncludeDups = true)
        {
			bool bFound;

            for(int i = 0; i < m_edges.Count; i++) 
			{
				bFound = false;
				for (int j = 0; j < lEdges.Count; j++)
				{
					if(m_edges.GetEdgeLine(i).LineEquals(lEdges[j])) 
					{
						if(!bIncludeDups)
                        {
							lEdges.RemoveAt(j); // this is not perfect. if this edge comes up again in m_edges it will get added.
							// but i think it's enough for my needs as this will not happen in my case(autosprite2)
                        }
						bFound = true;
						break;
					}
				}
				if(!bFound) 
					lEdges.Add(m_edges.GetEdgeLine(i));
            }
        }

		public void SetNormalCap(Color color)
		{
			m_NormalCapColor = color;
		}

		public void SetNormalStem(Color color)
		{
			m_NormalStemColor = color;
		}
	}
}
