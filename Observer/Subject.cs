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
	public class Subject
	{
		List<Observer> m_lObservers = new List<Observer>();
		static List<Change> g_lAccumulatedChanges = new List<Change>();
		static bool g_bDelayNotifications = false;
		List<Observer> m_lpTempObserversDetached = new List<Observer>();
		List<Observer> m_lpTempObserversAttached = new List<Observer>();
		bool m_bNotifying = false;

		~Subject()
		{
			foreach(Observer ob in m_lObservers)
			{
				ob.Unsubscribe(this, false); // last false argument is to prevent call to Remove
			}
		}

		public void Add(Observer pObserver)
		{
#if DEBUG
			// a particular observer can only be in list once.
			for (int i = 0; !m_bNotifying && i < m_lObservers.Count; i++) 
			{
				if (m_lObservers[i] == pObserver) {
					throw new Exception("Tried to add same observer twice");
				}
			}
#endif // DEBUG

			if (m_bNotifying) 
				m_lpTempObserversAttached.Add(pObserver);
			else 
				m_lObservers.Add(pObserver);
		}

		public void Remove(Observer pObserver)
		{
			// a particular observer can only be in list once.
			foreach(Observer ob in m_lObservers) 
			{
				if (ob == pObserver) {
					if (m_bNotifying) m_lpTempObserversDetached.Add(pObserver);
					else m_lObservers.Remove(ob);
					return;
				}
			}
			throw new Exception("Unable to find the observer to remove");
		}

		public void Notify(int nCode)
		{
			Notify(this, nCode, true);
		}

		public void Notify(Subject pOriginatingSubject, int nCode, bool bOnlyIssueUpdates)
		{
			if (!bOnlyIssueUpdates ) 
			{
				if (!g_bDelayNotifications ) 
				{
					g_bDelayNotifications = true;
					Notify(pOriginatingSubject, nCode, false);
					while (!(g_lAccumulatedChanges.Count == 0)) 
					{
						Change pChange = g_lAccumulatedChanges[0];
						// call this notification will spawn other notifies - these will get added to g_lAccumulatedChanges
						pChange.GetCallerSubject.Notify(pChange.GetOriginatingSubject, pChange.GetCode, true);
						g_lAccumulatedChanges.RemoveAt(0);
					}
					g_bDelayNotifications = false;
				}
				else 
				{
					Change aChange = new Change(this, pOriginatingSubject, nCode);
					g_lAccumulatedChanges.Add(aChange);
				}
			}
			else 
			{
				// we jump through some hoops here to make sure that subscribes and unsubscribes can happen as a result
				// of Update calls and everything is still okay
				m_bNotifying = true;
				Change aChange = new Change(this, pOriginatingSubject, nCode);
				foreach(Observer ob in m_lObservers)
				{
					if(m_lpTempObserversDetached.IndexOf(ob) != -1)
						continue;
				
					ob.Update(aChange);
				}
				m_bNotifying = false;
				
				foreach(Observer ob in m_lpTempObserversDetached)
				{
					Remove(ob);
				}

				foreach(Observer ob in m_lpTempObserversAttached) 
				{
					Add(ob);
					ob.Update(aChange);
				}

				m_lpTempObserversDetached.Clear();
				m_lpTempObserversAttached.Clear();
			}
		}
	}
}
