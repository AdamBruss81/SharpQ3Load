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

namespace engine
{
	public class Viewpoint
	{
		private D3Vect m_position = new D3Vect();
		private List<double> m_orientation = new List<double>();
		private string m_name;

		public string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}

		public D3Vect Position
		{
			get { return m_position; }
			set 
			{ 
				m_position = value;
				m_position[0] = m_position[0] * -1;
				double y = m_position[1];
				m_position[1] = m_position[2];
				m_position[2] = y;
			}
		}

		/// <summary>
		/// Returns a set of four numbers. The first three are the up vector. The last one is the theta value.
		/// </summary>
		public List<double> Orientation
		{
			get { return m_orientation; }
			set 
			{ 
				m_orientation = value;
				m_orientation[3] = m_orientation[3] - Math.PI / 2;
			}
		}
	}
}
