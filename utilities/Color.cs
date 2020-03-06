using System;

namespace utilities 
{
	public struct Color
	{
		private byte[] m_byColor;
		private byte[] m_by3Color;

		public Color(byte _red, byte _green, byte _blue, byte _alpha)
		{
			m_byColor = new byte[] { _red, _green, _blue, _alpha };
			m_by3Color = new byte[3];
		}

		public Color(byte _red, byte _green, byte _blue)
		{
			m_byColor = new byte[] { _red, _green, _blue, 255 };
			m_by3Color = new byte[3];
		}

		public byte[] GetAlphaColor
		{
			get { return m_byColor; }
		}

		public byte[] GetColor
		{
			get 
			{
				m_by3Color[0] = Red;
				m_by3Color[1] = Green;
				m_by3Color[2] = Blue;
				return m_by3Color;
			}
		}

		public byte Red
		{
			get { return m_byColor[0]; }
			set { m_byColor[0] = value; }
		}

		public byte Green
		{
			get { return m_byColor[1]; }
			set { m_byColor[1] = value; }
		}

		public byte Blue
		{
			get { return m_byColor[2]; }
			set { m_byColor[2] = value; }
		}

		public byte Alpha
		{
			get { return m_byColor[3]; }
			set { m_byColor[3] = value; }
		}

		new public string ToString()
		{
			return Convert.ToString(Red) + ", " + Convert.ToString(Green) + ", " + Convert.ToString(Blue);
		}

		public string ToStringAlpha()
		{
			return Convert.ToString(Red) + ", " + Convert.ToString(Green) + ", " + Convert.ToString(Blue) + ", " + Convert.ToString(Alpha);
		}
	}
}