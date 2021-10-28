using System;
using utilities;

namespace engine
{
	/// <summary>
	/// Represents an intersection on a face
	/// </summary>
	public class IntersectionInfo
	{
		Face m_face = null;
		D3Vect m_d3Intersection = new D3Vect(Double.MaxValue, Double.MaxValue, Double.MaxValue);
		double m_dDistanceFromCam = Double.MaxValue;

		/// <summary>
		/// Initialize an intersection
		/// </summary>
		/// <param name="face">face that has been intersected</param>
		/// <param name="d3Intersection">the point on the face where the intersection happened</param>
		public IntersectionInfo(Face face, D3Vect d3Intersection) 
		{
			m_face = face;
			m_d3Intersection = new D3Vect(d3Intersection);
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public IntersectionInfo() {}

		/// <summary>
		/// Get and set the face of intersection
		/// </summary>
		public Face Face
		{ 
			get { return m_face; } 
			set { m_face = value; }
		}
		
		/// <summary>
		/// Get and set the intersection point
		/// </summary>
		public D3Vect Intersection 
		{
			get { return m_d3Intersection; }
			set { m_d3Intersection = value; }
		}

		/// <summary>
		/// Return the distance of this intersection from the MovableCamera 
		/// </summary>
		public double DistanceFromCam
		{
			get { return m_dDistanceFromCam; }
			set { m_dDistanceFromCam = value; }
		}
	}
}
