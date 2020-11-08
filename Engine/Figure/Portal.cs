using System;
using utilities;

namespace engine
{
    class Transporter : Shape
    {
        private D3Vect m_d3TargetLocation = new D3Vect();
		private double m_dPHI = 0.0;
		private double m_Theta = 0.0;
		private uint m_popoutPowerMS = 300;
		private D3Vect m_d3Lookat = new D3Vect();

		public Transporter(Shape s) : base(s)
        {
            m_d3Lookat.SetXYZ(0, -.1, 0); // default
        }

        public utilities.D3Vect D3Lookat
        {
            get { return m_d3Lookat; }
            set { m_d3Lookat = value; }
        }

        public uint PopoutPowerMS
        {
            get { return m_popoutPowerMS; }
            set { m_popoutPowerMS = value; }
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

    class Jumppad : Shape
    {
        public Jumppad(Shape s) : base(s) { }

        private float m_fLaunchPower = 100; // so low that it will be obvious in game so i can fix it
        private int m_nIndex = -1; // this is initially for determining quickly at runtime which jumppad i hit to help in setting their jump values
        protected double dRotationAmountFromFaceNormalToUpZRad = (Math.PI / 8.0);

        public double RotationAmountFromFaceNormalToUpZRad
        {
            get { return dRotationAmountFromFaceNormalToUpZRad; }
            set { dRotationAmountFromFaceNormalToUpZRad = value; }
        }

        public int Index
        {
            get { return m_nIndex; }
            set { m_nIndex = value; }
        }

        public float LaunchPower
        {
            get { return m_fLaunchPower; }
            set { m_fLaunchPower = value; }
        }
    }

    class LaunchPad : Jumppad
    {
        public LaunchPad(Shape s) : base(s)
        {
            dRotationAmountFromFaceNormalToUpZRad = 90 * GLB.DegToRad; 
        }
    }
}
