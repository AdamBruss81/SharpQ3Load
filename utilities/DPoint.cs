using System;

namespace utilities
{
    /// <summary>
    /// A DPoint is a container class for an array of length 2 of doubles.
    /// Various constructors exist to easily populate the internal array 
    /// with two doubles.
    /// </summary>
    public class DPoint
    {
        private double[] vect;

        /// <summary>
        /// Creates a DPoint and initializes the doubles to zero.
        /// </summary>
        public DPoint() { vect = new double[] { 0.0, 0.0 }; } 

        /// <summary>
        /// Creates a DPoint and initialized the doubles to the passed parameters.
        /// </summary>
        public DPoint( double in0, double in1 ) 
        { 
            vect = new double[] { in0, in1 };
        }

        /// <summary>
        /// Creates a DPoint from a string where the values are separated by
        /// ' ' or ','.
        /// </summary>
        /// <param m_DisplayName="s"></param>
        public DPoint( string s ) 
        {
            vect = new double[2];
            string [] tokens = s.TrimStart().Split( new Char[] {' ', ',' } );
            for( int i = 0; i < 2; i++ )
            vect[i] = double.Parse( tokens[i] );
        }

		/// <summary>
		/// Returns an array of length 2 containg doubles.
		/// </summary>
		public double[] Vect 
		{ 
			get { return vect; } 
		}

		/// <summary>
		/// Returns value at index or sets a value at index
		/// </summary>
		/// <param name="index">index of coordinate in point</param>
		/// <returns></returns>
        public double this[int index]
        {
            get { return vect[index]; }
            set { vect[index] = value; }
        }     
    }
}
