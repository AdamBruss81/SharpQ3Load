using System;

#pragma warning disable 0660
#pragma warning disable 0661

namespace utilities
{
	/// <summary>
	/// A D3Vect is a container class for an array of length 3 of doubles.
	/// Various constructors exist to easily populate the internal array
	/// with three doubles.
	/// </summary>
	public class D3Vect
	{
		private double[] vect;
		string m_sToStringed;

		/// <summary>
		/// Creates a D3Vect and initializes the doubles to the passed parameters.
		/// </summary>
		/// <param m_DisplayName="in0"></param>
		/// <param m_DisplayName="in1"></param>
		/// <param m_DisplayName="in2"></param>
		public D3Vect(double in0, double in1, double in2)
		{ vect = new double[] { in0, in1, in2 }; }

		/// <summary>
		/// Creates a D3Vect from a string where the values are separated by
		/// ' ' or ','.
		/// </summary>
		/// <param m_DisplayName="s"></param>
		public D3Vect(string s)
		{
			vect = new double[3];
			string[] tokens = s.TrimStart().Split(new Char[] { ' ', ',' });
			for (int i = 0; i < 3; i++)
				vect[i] = Convert.ToDouble(tokens[i]);
		}

		public void SetAll(double dVal) { vect[0] = dVal; vect[1] = dVal; vect[2] = dVal; }

		/// <summary>
		/// Creates a D3Vect and initializes the doubles to zero.
		/// </summary>
		public D3Vect() { vect = new double[] { 0.0, 0.0, 0.0 }; }

		/// <summary>
		/// Creates a Dvect out of the cross product of the sent vectors 
		/// </summary>
		public D3Vect(D3Vect firstVec, D3Vect secondVec)
		{
			double normalx, normaly, normalz;
			normalx = firstVec[1] * secondVec[2] - firstVec[2] * secondVec[1];
			normaly = firstVec[2] * secondVec[0] - firstVec[0] * secondVec[2];
			normalz = firstVec[0] * secondVec[1] - firstVec[1] * secondVec[0];
			vect = new double[] { normalx, normaly, normalz };
		}

		public D3Vect(D3Vect v)
		{
			vect = new double[] { v.x, v.y, v.z };
		}

		public void SetXYZ(double x, double y, double z) { vect[0] = x; vect[1] = y; vect[2] = z; }

		public double x
		{
			get { return vect[0]; }
			set { vect[0] = value; }
		}

		public double y
		{
			get { return vect[1]; }
			set { vect[1] = value; }
		}

		public double z
		{
			get { return vect[2]; }
			set { vect[2] = value; }
		}

		public void Zero()
		{
			for (int i = 0; i < 3; i++)
				vect[i] = 0.0;
		}

		/// <summary>
		/// Copy incoming vec to this
		/// </summary>
		/// <param name="vec">vector to copy from</param>
		public void Copy(D3Vect vec)
		{
			vect[0] = vec[0];
			vect[1] = vec[1];
			vect[2] = vec[2];
		}

		public bool Empty
		{
			get { return vect[0] == 0.0 && vect[1] == 0.0 && vect[2] == 0.0; }
		}

		public void Negate()
		{
			for (int i = 0; i < 3; i++)
				vect[i] *= -1;
		}

		/// <summary>
		/// Turn into a unit vector
		/// </summary>
		public void normalize()
		{
			double length = Length;
			vect[0] = vect[0]/length;
			vect[1] = vect[1]/length;
			vect[2] = vect[2]/length;            
		}

		public double Length
		{
			get { return Math.Sqrt((vect[0] * vect[0]) + (vect[1] * vect[1]) + (vect[2] * vect[2])); }
			set 
			{
				if (value == 0.0) Zero();
				else if (Empty) return;
				else
				{
					double length = Length;
					vect[0] = vect[0] / (length / value);
					vect[1] = vect[1] / (length / value);
					vect[2] = vect[2] / (length / value);
				}
			}
		}

		public void Scale(double dScale)
		{
			for (int i = 0; i < 3; i++)
				vect[i] *= dScale;
		}

		/// <summary>
		/// Returns an array of length 3 containg doubles.
		/// </summary>
		public double[] Vect { get { return vect; } }
		public float[] VectFloat()
		{
			float[] v = { Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z) };
			return v;
		}

		public double this[int index]
		{
			get { return vect[index]; }
			set { vect[index] = value; }
		}

		public override string ToString()
		{
			m_sToStringed = String.Format("{0:0.00}", vect[0]) + ", " + String.Format("{0:0.00}", vect[1]) + ", " + String.Format("{0:0.00}", vect[2]);
			return m_sToStringed;
		}

		public static D3Vect Mult(double[] matrix, D3Vect d3Vector)
		{
			D3Vect d3Result = new D3Vect();

			d3Result.x = d3Vector.x * matrix[0] + d3Vector.y * matrix[4] + d3Vector.z * matrix[8];
			d3Result.y = d3Vector.x * matrix[1] + d3Vector.y * matrix[5] + d3Vector.z * matrix[9];
			d3Result.z = d3Vector.x * matrix[2] + d3Vector.y * matrix[6] + d3Vector.z * matrix[10];

			return d3Result;
		}

		public static D3Vect MidPoint(D3Vect v1, D3Vect v2)
		{
			return new D3Vect((v1[0] + v2[0]) / 2, (v1[1] + v2[1]) / 2, (v1[2] + v2[2]) / 2);
		}

		/// <summary>
		/// Returns the dot product of two vectors
		/// </summary>
		/// <param name="v1">first vec</param>
		/// <param name="v2">second vec</param>
		/// <returns>dot product</returns>
		public static double DotProduct(D3Vect v1, D3Vect v2)
		{
			return ((v1[0] * v2[0]) + (v1[1] * v2[1]) + (v1[2] * v2[2]));
		}

		public static bool Equals(D3Vect v1, D3Vect v2)
		{
			return v1.x == v2.x && v1.y == v2.y && v1.z == v2.z;
		}

        public static D3Vect operator +(D3Vect v1, D3Vect v2)
		{
			D3Vect sum = new D3Vect();
			for ( int i = 0; i < 3; i++ )
				sum[i] = v1[i] + v2[i];

			return sum;
		}

        public static D3Vect operator -(D3Vect v1, D3Vect v2)
        {
            D3Vect difference = new D3Vect();
            for (int i = 0; i < 3; i++)
				difference[i] = v1[i] - v2[i];

			return difference;
        }

        public static D3Vect operator *(D3Vect v1, double dMult)
        {
            D3Vect product = new D3Vect();
            for (int i = 0; i < 3; i++)
                product[i] = v1[i] * dMult;

            return product;
        }

		public static D3Vect operator /(D3Vect v1, double dDivide)
		{
			D3Vect division = new D3Vect();
			for (int i = 0; i < 3; i++)
				division[i] = v1[i] / dDivide;

			return division;
		}
	}
}
