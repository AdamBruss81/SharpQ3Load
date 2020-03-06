﻿using System;
using Tao.OpenGl;
using Tao.FreeGlut;

#pragma warning disable 0660
#pragma warning disable 0661

namespace utilities
{
	public class Edge
	{
		D3Vect m_d3V1;
		D3Vect m_d3V2;

		public Edge()
		{
			m_d3V1 = new D3Vect();
			m_d3V2 = new D3Vect();
		}

		public Edge(D3Vect v1, D3Vect v2)
		{
			m_d3V1 = v1;
			m_d3V2 = v2;
		}

		public void Clear()
		{
			m_d3V1.Zero();
			m_d3V2.Zero();
		}

		public bool Empty
		{
			get { return m_d3V1.Empty && m_d3V2.Empty; }
		}

		public D3Vect Vertice1
		{
			get { return m_d3V1; }
			set { m_d3V1 = value; }
		}

		public D3Vect Vertice2
		{
			get { return m_d3V2; }
			set { m_d3V2 = value; }
		}

		public bool LineEquals(Edge d1)
		{
			if (d1.Vertice1 == Vertice1)
			{
				return d1.Vertice2 == Vertice2;
			}
			else if (d1.Vertice1 == Vertice2)
			{
				return d1.Vertice2 == Vertice1;
			}
			else return false;
		}

		public void Draw(Color col, bool bGlut)
		{
			if (!Empty)
			{
				sgl.PUSHATT(Gl.GL_CURRENT_BIT);
				Gl.glColor3ubv(col.GetColor);

				if (!bGlut)
				{
					Gl.glBegin(Gl.GL_LINES);
					{
						Gl.glVertex3dv(Vertice1.Vect);
						Gl.glVertex3dv(Vertice2.Vect);
					}
					Gl.glEnd();
				}
				else
				{
					// http://www.euclideanspace.com/maths/algebra/vectors/angleBetween/index.htm
					D3Vect zup = new D3Vect(0, 0, 1);
					D3Vect ray = Vertice2 - Vertice1;
					ray.normalize();
					double rotAngle = Math.Acos(D3Vect.DotProduct(ray, zup)) * GLB.RadToDeg;
					if (Math.Abs(rotAngle) < 0.001) rotAngle = 0.0;
					D3Vect rotAxis = new D3Vect(zup, ray);
					sgl.PUSHMAT();
					Gl.glTranslated(Vertice1[0], Vertice1[1], Vertice1[2]);
					Gl.glRotated(rotAngle, rotAxis[0], rotAxis[1], rotAxis[2]);
					Glut.glutSolidCylinder(0.01, (Vertice2 - Vertice1).Length, 20, 10);
					sgl.POPMAT();
				}

				sgl.POPATT();
			}
		}

        public static bool operator ==(Edge d1, Edge d2)
        {
            return d1.Vertice1 == d2.Vertice1 && d1.Vertice2 == d2.Vertice2;
        }

        public static bool operator !=(Edge d1, Edge d2)
        {
            return !(d1.Vertice1 == d2.Vertice1 && d1.Vertice2 == d2.Vertice2);
        }
	}
}
