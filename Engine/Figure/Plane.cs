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

using utilities;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace engine
{
	/// <summary>
	/// This class represents a plane in three dimensional space. A plane is defined by two entities.
	/// 1. the vector perpendicular to the surface
	/// 2. the distance from the origin to the surface along the vector defined in 1.
	/// </summary>
	public class Plane
	{
		protected double m_dDistanceToOriginAlongNormal;
		protected D3Vect m_d3Normal = new D3Vect();

		public void Initialize(D3Vect d3VecOne, D3Vect d3VecTwo, bool bReverse, D3Vect d3Point)
		{
			m_d3Normal = new D3Vect(d3VecOne, d3VecTwo);
			m_d3Normal.normalize();

			if (bReverse) m_d3Normal.Negate();

			m_dDistanceToOriginAlongNormal = calculateDistance(m_d3Normal, d3Point);
		}

		/// <summary>
		/// Return new vector that equals our normal
		/// </summary>
		public D3Vect GetNewNormal { get { return new D3Vect(m_d3Normal); } }

		/// <summary>
		/// Return pointer to normal
		/// </summary>
		public D3Vect GetNormal { get { return m_d3Normal; } }

		/// <summary>
		/// Get distance to origin along normal
		/// </summary>
		public double DistanceToOriginAlongNormal
		{
			get { return m_dDistanceToOriginAlongNormal; }
			set { m_dDistanceToOriginAlongNormal = value; }
		}

		override public string ToString()
		{
			return GetNormal.ToString() + ", " + Convert.ToString(DistanceToOriginAlongNormal);
		}

		/**********************************************************************************************************/

		/// <summary>
		/// Find distance from this face to the origin by plugging in the m_d3Normal and a point on the plane
		/// into the formula for a plane.
		/// </summary>
		public static double calculateDistance(D3Vect normal, D3Vect vertice)
		{
			return -1 * D3Vect.DotProduct(normal, vertice);
		}

		/// <summary>
		/// Get the current viewing frustrum as six Plane objects
		/// http://www.crownandcutlass.com/features/technicaldetails/frustum.html
		/// </summary>
		/// <returns>the planes in a list</returns>
		public static void ExtractFrustrum(List<Plane> lFrustrumPlanes)
		{
			Debug.Assert(lFrustrumPlanes.Count >= 6);

			double[] proj = new double[16];
			double[] modl = new double[16];
			double[] clip = new double[16];
			double t;

			/* Get the current PROJECTION matrix from OpenGL */
			GL.GetDouble(GetPName.ProjectionMatrix, proj);

			/* Get the current MODELVIEW matrix from OpenGL */
			GL.GetDouble(GetPName.ModelviewMatrix, modl);

			/* Combine the two matrices (multiply projection by modelview) */
			clip[ 0] = modl[ 0] * proj[ 0] + modl[ 1] * proj[ 4] + modl[ 2] * proj[ 8] + modl[ 3] * proj[12];
			clip[ 1] = modl[ 0] * proj[ 1] + modl[ 1] * proj[ 5] + modl[ 2] * proj[ 9] + modl[ 3] * proj[13];
			clip[ 2] = modl[ 0] * proj[ 2] + modl[ 1] * proj[ 6] + modl[ 2] * proj[10] + modl[ 3] * proj[14];
			clip[ 3] = modl[ 0] * proj[ 3] + modl[ 1] * proj[ 7] + modl[ 2] * proj[11] + modl[ 3] * proj[15];

			clip[ 4] = modl[ 4] * proj[ 0] + modl[ 5] * proj[ 4] + modl[ 6] * proj[ 8] + modl[ 7] * proj[12];
			clip[ 5] = modl[ 4] * proj[ 1] + modl[ 5] * proj[ 5] + modl[ 6] * proj[ 9] + modl[ 7] * proj[13];
			clip[ 6] = modl[ 4] * proj[ 2] + modl[ 5] * proj[ 6] + modl[ 6] * proj[10] + modl[ 7] * proj[14];
			clip[ 7] = modl[ 4] * proj[ 3] + modl[ 5] * proj[ 7] + modl[ 6] * proj[11] + modl[ 7] * proj[15];

			clip[ 8] = modl[ 8] * proj[ 0] + modl[ 9] * proj[ 4] + modl[10] * proj[ 8] + modl[11] * proj[12];
			clip[ 9] = modl[ 8] * proj[ 1] + modl[ 9] * proj[ 5] + modl[10] * proj[ 9] + modl[11] * proj[13];
			clip[10] = modl[ 8] * proj[ 2] + modl[ 9] * proj[ 6] + modl[10] * proj[10] + modl[11] * proj[14];
			clip[11] = modl[ 8] * proj[ 3] + modl[ 9] * proj[ 7] + modl[10] * proj[11] + modl[11] * proj[15];

			clip[12] = modl[12] * proj[ 0] + modl[13] * proj[ 4] + modl[14] * proj[ 8] + modl[15] * proj[12];
			clip[13] = modl[12] * proj[ 1] + modl[13] * proj[ 5] + modl[14] * proj[ 9] + modl[15] * proj[13];
			clip[14] = modl[12] * proj[ 2] + modl[13] * proj[ 6] + modl[14] * proj[10] + modl[15] * proj[14];
			clip[15] = modl[12] * proj[ 3] + modl[13] * proj[ 7] + modl[14] * proj[11] + modl[15] * proj[15];

			/* Extract the numbers for the RIGHT plane */
			lFrustrumPlanes[0].GetNormal.x = clip[ 3] - clip[ 0];
			lFrustrumPlanes[0].GetNormal.y = clip[7] - clip[4];
			lFrustrumPlanes[0].GetNormal.z = clip[11] - clip[8];
			lFrustrumPlanes[0].DistanceToOriginAlongNormal = clip[15] - clip[12];

			/* Normalize the result */
			t = Math.Sqrt(lFrustrumPlanes[0].GetNormal.x * lFrustrumPlanes[0].GetNormal.x + 
				lFrustrumPlanes[0].GetNormal.y * lFrustrumPlanes[0].GetNormal.y +
				lFrustrumPlanes[0].GetNormal.z * lFrustrumPlanes[0].GetNormal.z);
			lFrustrumPlanes[0].GetNormal.x /= t;
			lFrustrumPlanes[0].GetNormal.y /= t;
			lFrustrumPlanes[0].GetNormal.z /= t;
			lFrustrumPlanes[0].DistanceToOriginAlongNormal /= t;

			/* Extract the numbers for the LEFT plane */
			lFrustrumPlanes[1].GetNormal.x = clip[3] + clip[0];
			lFrustrumPlanes[1].GetNormal.y = clip[7] + clip[4];
			lFrustrumPlanes[1].GetNormal.z = clip[11] + clip[8];
			lFrustrumPlanes[1].DistanceToOriginAlongNormal = clip[15] + clip[12];

			/* Normalize the result */
			t = Math.Sqrt(lFrustrumPlanes[1].GetNormal.x * lFrustrumPlanes[1].GetNormal.x +
				lFrustrumPlanes[1].GetNormal.y * lFrustrumPlanes[1].GetNormal.y +
				lFrustrumPlanes[1].GetNormal.z * lFrustrumPlanes[1].GetNormal.z);
			lFrustrumPlanes[1].GetNormal.x /= t;
			lFrustrumPlanes[1].GetNormal.y /= t;
			lFrustrumPlanes[1].GetNormal.z /= t;
			lFrustrumPlanes[1].DistanceToOriginAlongNormal /= t;

			/* Extract the BOTTOM plane */
			lFrustrumPlanes[2].GetNormal.x = clip[3] + clip[1];
			lFrustrumPlanes[2].GetNormal.y = clip[7] + clip[5];
			lFrustrumPlanes[2].GetNormal.z = clip[11] + clip[9];
			lFrustrumPlanes[2].DistanceToOriginAlongNormal = clip[15] + clip[13];

			/* Normalize the result */
			t = Math.Sqrt(lFrustrumPlanes[2].GetNormal.x * lFrustrumPlanes[2].GetNormal.x +
				lFrustrumPlanes[2].GetNormal.y * lFrustrumPlanes[2].GetNormal.y +
				lFrustrumPlanes[2].GetNormal.z * lFrustrumPlanes[2].GetNormal.z);
			lFrustrumPlanes[2].GetNormal.x /= t;
			lFrustrumPlanes[2].GetNormal.y /= t;
			lFrustrumPlanes[2].GetNormal.z /= t;
			lFrustrumPlanes[2].DistanceToOriginAlongNormal /= t;

			/* Extract the TOP plane */
			lFrustrumPlanes[3].GetNormal.x = clip[3] - clip[1];
			lFrustrumPlanes[3].GetNormal.y = clip[7] - clip[5];
			lFrustrumPlanes[3].GetNormal.z = clip[11] - clip[9];
			lFrustrumPlanes[3].DistanceToOriginAlongNormal = clip[15] - clip[13];

			/* Normalize the result */
			t = Math.Sqrt(lFrustrumPlanes[3].GetNormal.x * lFrustrumPlanes[3].GetNormal.x +
				lFrustrumPlanes[3].GetNormal.y * lFrustrumPlanes[3].GetNormal.y +
				lFrustrumPlanes[3].GetNormal.z * lFrustrumPlanes[3].GetNormal.z);
			lFrustrumPlanes[3].GetNormal.x /= t;
			lFrustrumPlanes[3].GetNormal.y /= t;
			lFrustrumPlanes[3].GetNormal.z /= t;
			lFrustrumPlanes[3].DistanceToOriginAlongNormal /= t;

			/* Extract the FAR plane */
			lFrustrumPlanes[4].GetNormal.x = clip[3] - clip[2];
			lFrustrumPlanes[4].GetNormal.y = clip[7] - clip[6];
			lFrustrumPlanes[4].GetNormal.z = clip[11] - clip[10];
			lFrustrumPlanes[4].DistanceToOriginAlongNormal = clip[15] - clip[14];

			/* Normalize the result */
			t = Math.Sqrt(lFrustrumPlanes[4].GetNormal.x * lFrustrumPlanes[4].GetNormal.x +
				lFrustrumPlanes[4].GetNormal.y * lFrustrumPlanes[4].GetNormal.y +
				lFrustrumPlanes[4].GetNormal.z * lFrustrumPlanes[4].GetNormal.z);
			lFrustrumPlanes[4].GetNormal.x /= t;
			lFrustrumPlanes[4].GetNormal.y /= t;
			lFrustrumPlanes[4].GetNormal.z /= t;
			lFrustrumPlanes[4].DistanceToOriginAlongNormal /= t;

			/* Extract the NEAR plane */
			lFrustrumPlanes[5].GetNormal.x = clip[3] + clip[2];
			lFrustrumPlanes[5].GetNormal.y = clip[7] + clip[6];
			lFrustrumPlanes[5].GetNormal.z = clip[11] + clip[10];
			lFrustrumPlanes[5].DistanceToOriginAlongNormal = clip[15] + clip[14];

			/* Normalize the result */
			t = Math.Sqrt(lFrustrumPlanes[5].GetNormal.x * lFrustrumPlanes[5].GetNormal.x +
				lFrustrumPlanes[5].GetNormal.y * lFrustrumPlanes[5].GetNormal.y +
				lFrustrumPlanes[5].GetNormal.z * lFrustrumPlanes[5].GetNormal.z);
			lFrustrumPlanes[5].GetNormal.x /= t;
			lFrustrumPlanes[5].GetNormal.y /= t;
			lFrustrumPlanes[5].GetNormal.z /= t;
			lFrustrumPlanes[5].DistanceToOriginAlongNormal /= t;
		}
	}
}
