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
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;
using System.Collections.Generic;
using System.Diagnostics;
using utilities;

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
		FaceEdges m_edges = new FaceEdges();

		D3Vect m_d3IntersectRay = new D3Vect(); // memory placeholder
		D3Vect m_d3Intersection = new D3Vect(); // memory placeholder
		D3Vect m_d3MidPoint = new D3Vect();

		Color m_NormalCapColor = new Color(0, 255, 0);
		Color m_SolidColor = new Color(140,140,140);
		Color m_NormalStemColor = new Color(0, 100, 0);

		bool m_bDrawSolidColor = false;
		bool m_bRenderedThisPass = false;
		
		double m_nIntersectAdjuster;
		double m_dVisualNormalScale = 1.0;

		int m_nOneTextureFaceDrawList;
		int m_nOneTextureFaceDrawListWhite;
		int m_TwoTextureFaceDrawList;
		int m_nWireframeDrawList;
		int m_nSolidColorDrawList;
		int m_nDrawNormalList;
		int m_nIndex = -1;
		int m_nNumberOfBoundingBoxHolders = 0;		
		int m_nNumberOfVisibleBoundingBoxes = 0;
	
		Shape.ETextureType m_TextureType;

		enum EPointClassification { NONE, PLANE_FRONT, PLANE_BACK, PLANE_COINCIDE };

		/// <summary>
		/// Creates a face from the passed in string.
		/// </summary>
		/// <param m_DisplayName="s">a string of integer indicies which reference the 
		/// corresponding m_verticies used to draw the face.</param>
		public Face( List<D3Vect> lVerts, List<List<DPoint>> lTextCoords, List<D3Vect> lVertColors, Color normalCap, Color normalStem, int index )
		{
			m_TextureType = lTextCoords.Count == 2 ? Shape.ETextureType.MULTI : Shape.ETextureType.SINGLE;
			m_lTextureCoordinates = new List<List<DPoint>>();
			m_lTextureCoordinates.Add(new List<DPoint>(lTextCoords[0]));
			if (m_TextureType == Shape.ETextureType.MULTI)
				m_lTextureCoordinates.Add(new List<DPoint>(lTextCoords[1]));
			m_lVertices = new List<D3Vect>(lVerts);
			m_lVertColors = new List<D3Vect>(lVertColors);
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
				m_nNumberOfVisibleBoundingBoxes = value; 
			}
		}

		public int NumberOfVisibleBoundingBoxes
		{
			get { return m_nNumberOfVisibleBoundingBoxes; }
			set 
			{ 
				m_nNumberOfVisibleBoundingBoxes = value;
				if (m_nNumberOfVisibleBoundingBoxes < 0)
					throw new Exception("Face cannot have negative number of visible bounding box holders");
				else if (m_nNumberOfVisibleBoundingBoxes == 0)
					RenderedThisPass = true;
			}
		}

		public bool DrawSolidColor
		{
			get { return m_bDrawSolidColor; }
			set { m_bDrawSolidColor = value; }
		}

		public List<Edge> GetEdges { get { return m_edges.EdgeLines; } }
		public List<D3Vect> GetEdgeVectors { get { return m_edges.EdgeVectors; } }
		public List<D3Vect> GetVertices { get { return m_lVertices; }	}

		public bool RenderedThisPass
		{
			get { return m_bRenderedThisPass; }
			set { m_bRenderedThisPass = value; }
		}

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
		public bool CanMove(D3Vect position, D3Vect dest, IntersectionInfo intersection)
		{
			bool bCanMove = false;

			EPointClassification positionClass = ClassifyPoint(position, GetNormal, DistanceToOriginAlongNormal);
			EPointClassification destinationClass = ClassifyPoint(dest, GetNormal, DistanceToOriginAlongNormal);

			if (positionClass != destinationClass)
			{
				m_d3IntersectRay[0] = dest[0] - position[0];
				m_d3IntersectRay[1] = dest[1] - position[1];
				m_d3IntersectRay[2] = dest[2] - position[2];

				double denom = D3Vect.DotProduct(GetNormal, m_d3IntersectRay);

				if(denom == 0) 
					return false; // means infinite number of points on plane

				m_nIntersectAdjuster = -(D3Vect.DotProduct(GetNormal, position) + DistanceToOriginAlongNormal) / denom;

				m_d3IntersectRay[0] = m_d3IntersectRay[0] * m_nIntersectAdjuster;
				m_d3IntersectRay[1] = m_d3IntersectRay[1] * m_nIntersectAdjuster;
				m_d3IntersectRay[2] = m_d3IntersectRay[2] * m_nIntersectAdjuster;

				m_d3Intersection[0] = position[0] + m_d3IntersectRay[0];
				m_d3Intersection[1] = position[1] + m_d3IntersectRay[1];
				m_d3Intersection[2] = position[2] + m_d3IntersectRay[2];

				EPointClassification pointclass;
				EPointClassification previousClass = EPointClassification.NONE;
				for (int i = 0; i < Count; i++)
				{
					pointclass = ClassifyPoint(m_d3Intersection, m_edges.GetInwardNormal(i), m_edges.GetPlaneDistance(i));
					if (previousClass != EPointClassification.NONE && pointclass != previousClass)
					{
						bCanMove = true;
						break;
					}
					previousClass = pointclass;
				}

				if (!bCanMove && (Object)intersection != null)
				{
					intersection.Intersection.Copy(m_d3Intersection);
					intersection.Face = this;
				}
			}
			else
				bCanMove = true;

			return bCanMove;
		}

		public void InitializeLists()
		{
			GenDebugDrawList();

			if (m_TextureType == Shape.ETextureType.MULTI)
			{
				m_TwoTextureFaceDrawList = Gl.glGenLists(1);
				Gl.glNewList(m_TwoTextureFaceDrawList, Gl.GL_COMPILE);
				Gl.glBegin(Gl.GL_POLYGON);
				{
					for (int i = 0; i < m_lVertices.Count; i++)
					{
						Gl.glMultiTexCoord2dv(Gl.GL_TEXTURE1, m_lTextureCoordinates[1][i].Vect);
						Gl.glMultiTexCoord2dv(Gl.GL_TEXTURE0, m_lTextureCoordinates[0][i].Vect);
						Gl.glVertex3dv(m_lVertices[i].Vect);
					}
				}
				Gl.glEnd();
				Gl.glEndList();

				GenSingleTextureDisplayLists(1);
			}
			else if (m_TextureType == Shape.ETextureType.SINGLE)
			{
				GenSingleTextureDisplayLists(0);
			}
			if (m_TextureType != Shape.ETextureType.NONE)
			{
				m_nWireframeDrawList = Gl.glGenLists(1);
				Gl.glNewList(m_nWireframeDrawList, Gl.GL_COMPILE);
				Gl.glBegin(Gl.GL_LINE_LOOP);
				{
					for (int j = 0; j < m_lVertices.Count; j++)
						Gl.glVertex3dv(m_lVertices[j].Vect);
				}
				Gl.glEnd();
				Gl.glEndList();
			}

			m_nDrawNormalList = Gl.glGenLists(1);
			Gl.glNewList(m_nDrawNormalList, Gl.GL_COMPILE);
			DrawNormals();
			Gl.glEndList();
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
			Gl.glDeleteLists(m_TwoTextureFaceDrawList, 1);
			Gl.glDeleteLists(m_nWireframeDrawList, 1);
			Gl.glDeleteLists(m_nDrawNormalList, 1);
			Gl.glDeleteLists(m_nSolidColorDrawList, 1);
			Gl.glDeleteLists(m_nOneTextureFaceDrawList, 1);
			Gl.glDeleteLists(m_nOneTextureFaceDrawListWhite, 1);
		}

		private void GenDebugDrawList()
		{
			m_nSolidColorDrawList = Gl.glGenLists(1);
			Gl.glNewList(m_nSolidColorDrawList, Gl.GL_COMPILE);
			sgl.PUSHATT(Gl.GL_ALL_ATTRIB_BITS);
			Gl.glColor3ubv(m_SolidColor.GetColor);
			Gl.glBegin(Gl.GL_POLYGON);
			{
				for (int i = 0; i < m_lVertices.Count; i++)
				{
					Gl.glVertex3dv(m_lVertices[i].Vect);
				}
			}
			Gl.glEnd();
			sgl.POPATT();
			Gl.glEndList();
		}

		private void GenSingleTextureDisplayLists(int TexCoordSetIndex)
		{
			m_nOneTextureFaceDrawList = Gl.glGenLists(1);
			Gl.glNewList(m_nOneTextureFaceDrawList, Gl.GL_COMPILE);
			Gl.glBegin(Gl.GL_POLYGON);
			{
				for (int i = 0; i < m_lVertices.Count; i++)
				{
					Gl.glColor3dv(m_lVertColors[i].Vect);
					Gl.glTexCoord2dv(m_lTextureCoordinates[TexCoordSetIndex][i].Vect);
					Gl.glVertex3dv(m_lVertices[i].Vect);
				}
			}
			Gl.glEnd();
			Gl.glEndList();

			m_nOneTextureFaceDrawListWhite = Gl.glGenLists(1);
			Gl.glNewList(m_nOneTextureFaceDrawListWhite, Gl.GL_COMPILE);
            Gl.glBegin(Gl.GL_POLYGON);
            {
                for (int i = 0; i < m_lVertices.Count; i++)
                {
                    Gl.glTexCoord2dv(m_lTextureCoordinates[TexCoordSetIndex][i].Vect);
                    Gl.glVertex3dv(m_lVertices[i].Vect);
                }
            }
            Gl.glEnd();
			Gl.glEndList();
		}

		/// <summary>
		/// Draw this face
		/// </summary>
		public void Draw(Engine.EGraphicsMode mode, ref int nRendered)
		{
			if (DrawSolidColor)
				Gl.glCallList(m_nSolidColorDrawList);
			else if (mode == Engine.EGraphicsMode.WIREFRAME)
				Gl.glCallList(m_nWireframeDrawList);
			else if (mode == Engine.EGraphicsMode.MULTI_TEXTURE_WHITE)
			{
				if (m_TextureType == Shape.ETextureType.MULTI)
					Gl.glCallList(m_TwoTextureFaceDrawList);
				else if (m_TextureType == Shape.ETextureType.SINGLE)
					Gl.glCallList(m_nOneTextureFaceDrawList);
			}
			else if (mode == Engine.EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR)
				Gl.glCallList(m_nOneTextureFaceDrawList);
            else if (mode == Engine.EGraphicsMode.SINGLE_WHITE)
            {
                Gl.glCallList(m_nOneTextureFaceDrawListWhite);
            }

			if (STATE.DebuggingMode && STATE.DrawFaceNormals)
			{
				Gl.glCallList(m_nDrawNormalList);
			}

			nRendered++;

			RenderedThisPass = true;
		}

		/// <summary>
		/// Draw normal from midpoint of face
		/// </summary>
		public void DrawNormals()
		{
			for(int i = 0; i < m_edges.Count; i++)
			{
				DrawNormal(m_edges.GetInwardNormal(i), m_edges.GetMidpoint(i));
			}
			DrawNormal(GetNormal, m_d3MidPoint);
		}

		private void DrawNormal(D3Vect normal, D3Vect start)
		{
			// draw line from midpoint to midpoint plus normal
			D3Vect normEnd = start + normal * m_dVisualNormalScale;

			sgl.PUSHATT(Gl.GL_CURRENT_BIT | Gl.GL_LINE_BIT | Gl.GL_TEXTURE_BIT);

			Gl.glDisable(Gl.GL_TEXTURE_2D);
			Gl.glColor3ubv(m_NormalStemColor.GetColor);

			sgl.PUSHMAT();
			// http://www.euclideanspace.com/maths/algebra/vectors/angleBetween/index.htm
			D3Vect zup = new D3Vect(0, 0, 1);
			double rotAngle = Math.Acos(D3Vect.DotProduct(normal, zup)) * GLB.RadToDeg;
			if (Math.Abs(rotAngle) < 0.001) 
				rotAngle = 0.0;
			D3Vect rotAxis = new D3Vect(zup, normal);
			Gl.glTranslated(start[0], start[1], start[2]);
			Gl.glRotated(rotAngle, rotAxis[0], rotAxis[1], rotAxis[2]);
			Glut.glutSolidCylinder(0.05, (start - normEnd).Length, 7, 1);
			sgl.POPMAT();

			Gl.glColor3ubv(m_NormalCapColor.GetColor);
			sgl.PUSHMAT();
			Gl.glTranslated(normEnd[0], normEnd[1], normEnd[2]);
			Gl.glRotated(rotAngle, rotAxis[0], rotAxis[1], rotAxis[2]);
			Glut.glutSolidCone(0.05, 0.3, 7, 1);
			sgl.POPMAT();

			sgl.POPATT();
		}		   

		/// <summary>
		/// Fill out lEdges with new edges from this
		/// </summary>
		/// <param name="lEdges">A reference parameter of unique directional lines</param>
        public void GatherUniqueEdges(List<Edge> lEdges)
        {
			bool bFound;

            for(int i = 0; i < m_edges.Count; i++) 
			{
				bFound = false;
				for (int j = 0; j < lEdges.Count; j++)
				{
					if(m_edges.GetEdgeLine(i).LineEquals(lEdges[j])) {
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
