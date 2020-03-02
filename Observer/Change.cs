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

namespace obsvr
{
	public class Change
	{
		int m_nCode = -1;
		Subject m_pCallerSubject = null;
		Subject m_pOriginatingSubject = null;

		public Change(Subject pCallerSubject, Subject pOriginatingSubject, int nCode)
		{ 
			m_pCallerSubject = pCallerSubject; 
			m_pOriginatingSubject = pOriginatingSubject; 
			m_nCode = nCode; 
		}

		public Subject GetCallerSubject
		{
			get { return m_pCallerSubject; }
		}

		public Subject GetOriginatingSubject
		{
			get { return m_pOriginatingSubject; }
		}

		public int GetCode 
		{
			get { return m_nCode; }
		}
	
		public void Set(Subject pCallerSubject, Subject pOriginatingSubject, int nCode)
		{ 
			m_pCallerSubject = pCallerSubject; 
			m_pOriginatingSubject = pOriginatingSubject; 
			m_nCode = nCode;
		}		
	};
}
