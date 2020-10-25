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
using System.Linq;
using System.Text;

namespace obsvr
{
    public interface IObserverHelper
    {
		void Notify(Change pChange);
    }

    public class Communicator : Observer
	{
		private Subject m_pSubject = new Subject();
		private IObserverHelper m_pHelper = null;

		public void Notify(int nCode)
		{
			m_pSubject.Notify(m_pSubject, nCode, "", true);
		}

		public void Notify(string sMessage, int nCode)
		{
			m_pSubject.Notify(m_pSubject, nCode, sMessage, true);
		}

		public void SetListener(IObserverHelper iHelper)
        {
			m_pHelper = iHelper;
        }

		public override void Update(Change pChange) 
		{
			if(m_pHelper != null)
            {
				m_pHelper.Notify(pChange);
            }
		}

		public Subject GetSubject
		{
			get { return m_pSubject; }
		}
	}
}
