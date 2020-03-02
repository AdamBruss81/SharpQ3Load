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
	public class Observer
	{	
		List<Subject> m_lSubjects = new List<Subject>();

		~Observer()
		{
			UnsubscribeAll();
		}

		public virtual void Update(Change pChange) {}

		public void Subscribe(Subject pSubject)
		{
			// a particular subject can only be in list once.
			foreach(Subject subject in m_lSubjects)
			{
				if (subject == pSubject) {
					throw new Exception("Already subscribe to subject");
				}
			}

			m_lSubjects.Add(pSubject);
			pSubject.Add(this);
		}

		public void Unsubscribe(Subject pSubject, bool bDetachFromSubject)
		{
			// a particular observer can only be in list once.
			foreach(Subject subject in m_lSubjects)
			{
				if (subject == pSubject) {
					if (bDetachFromSubject) pSubject.Remove(this);
					m_lSubjects.Remove(pSubject);
					return;
				}
			}
			throw new Exception("Could not find subject in list of subscriptions");
		}

		public void UnsubscribeAll()
		{
			foreach(Subject subject in m_lSubjects)
			{
				subject.Remove(this);
			}
			m_lSubjects.Clear();
		}

		public int GetNumSubjects
		{
			get { return m_lSubjects.Count; }
		}
	};
}
