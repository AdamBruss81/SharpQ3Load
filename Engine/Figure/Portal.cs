using System;
using utilities;

namespace engine
{
    class Transporter : Shape
    {
        private D3Vect m_d3TargetLocation = new D3Vect();
		private double m_dPHI = 0.0;
		private double m_Theta = 0.0;

        public Transporter(Shape s) : base(s)
        {

        }

        public double PHI
		{
			get { return m_dPHI; }
			set { m_dPHI = value; }
		}
		
		public double Theta
		{
			get { return m_Theta; }
			set { m_Theta = value; }
		}
	
		public D3Vect D3TargetLocation
		{
			get { return m_d3TargetLocation; }
			set { m_d3TargetLocation = value; }
		}
	}

	class Portal : Transporter
	{
        public Portal(Shape s) : base(s)
        {

        }
    }

    class Teleporter : Transporter
    {
        public Teleporter(Shape s) : base(s)
        {

        }
    }
}
