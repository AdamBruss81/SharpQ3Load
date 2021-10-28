using System.Collections.Generic;
using utilities;

namespace engine
{
	class FaceEdges
	{
		List<D3Vect> m_lEdgeVectors = new List<D3Vect>();
		List<Edge> m_lEdgeLines = new List<Edge>();
		List<D3Vect> m_lInwardEdgeNormals = new List<D3Vect>();
		List<double> m_lInwardPlaneDistances = new List<double>();
		List<D3Vect> m_lMidpoints = new List<D3Vect>();

		public List<D3Vect> EdgeVectors { get { return m_lEdgeVectors; } }
		public List<Edge> EdgeLines { get { return m_lEdgeLines; } }
		public List<D3Vect> InwardNormals { get { return m_lInwardEdgeNormals; } }
		public List<double> InwardPlaneDistances { get { return m_lInwardPlaneDistances; } }
		public List<D3Vect> Midpoints { get { return m_lMidpoints; } }

		public int Count { get { return m_lEdgeLines.Count; } }

		public void Add(D3Vect v1, D3Vect v2, D3Vect basisNormal) 
		{
			D3Vect vec = v2 - v1;
			vec.normalize();
			m_lEdgeVectors.Add(vec);
			m_lEdgeLines.Add(new Edge(v1, v2));
			D3Vect inward = new D3Vect(basisNormal, vec);
			inward.normalize();
			m_lInwardEdgeNormals.Add(inward);
			m_lInwardPlaneDistances.Add(Plane.calculateDistance(m_lInwardEdgeNormals[m_lInwardEdgeNormals.Count - 1], 
				m_lEdgeLines[m_lEdgeLines.Count - 1].Vertice1));
			m_lMidpoints.Add(D3Vect.MidPoint(v1, v2));
		}

		public D3Vect GetEdgeVector(int index) 
		{ 
			return m_lEdgeVectors[index];
		}

		public Edge GetEdgeLine(int index) 
		{ 
			return m_lEdgeLines[index];
		}

		public D3Vect GetInwardNormal(int index) 
		{ 
			return m_lInwardEdgeNormals[index];
		}

		public double GetPlaneDistance(int index)
		{
			return m_lInwardPlaneDistances[index];
		}

		public D3Vect GetMidpoint(int index)
		{
			return m_lMidpoints[index];
		}
	}
}
