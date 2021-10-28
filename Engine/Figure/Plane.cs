﻿using utilities;
using System;

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
	}
}
