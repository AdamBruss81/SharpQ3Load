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
using NUnit.Framework;

namespace obsvr.Test
{
	[TestFixture]
	public class ObserverTest
	{
		[Test]
		public void TestSubscriptionsAndNotifications()
		{
			Watcher watcher = new Watcher();
			Signaler signaler1 = new Signaler();
			Signaler signaler2 = new Signaler();
			Signaler signaler3 = new Signaler();
			Signaler signaler4 = new Signaler();

			watcher.Subscribe(signaler1);
			watcher.Subscribe(signaler2);
			watcher.Subscribe(signaler3);
			watcher.Subscribe(signaler4);

			Assert.AreEqual(0, Watcher.g_nNumUpdatedYelled);
			Assert.AreEqual(0, Watcher.g_nNumUpdatedWhispered);

			signaler1.Notify((int)Signaler.Signals.YELLED);

			Assert.AreEqual(1, Watcher.g_nNumUpdatedYelled);

			signaler1.Notify((int)Signaler.Signals.WHISPERED);

			Assert.AreEqual(1, Watcher.g_nNumUpdatedWhispered);

			signaler2.Notify((int)Signaler.Signals.WHISPERED);
			signaler3.Notify((int)Signaler.Signals.WHISPERED);
			signaler4.Notify((int)Signaler.Signals.WHISPERED);

			Assert.AreEqual(4, Watcher.g_nNumUpdatedWhispered);

			signaler2.Notify((int)Signaler.Signals.YELLED);
			signaler3.Notify((int)Signaler.Signals.YELLED);
			signaler4.Notify((int)Signaler.Signals.YELLED);

			Assert.AreEqual(4, Watcher.g_nNumUpdatedYelled);

			watcher.Unsubscribe(signaler1, true);
			Assert.AreEqual(3, watcher.GetNumSubjects);
			Assert.Throws<System.Exception>(delegate{watcher.Unsubscribe(signaler1, true);});
			Assert.Throws<System.Exception>(delegate{watcher.Subscribe(signaler2);});
			Assert.AreEqual(3, watcher.GetNumSubjects);

			watcher.UnsubscribeAll();
			Assert.AreEqual(0, watcher.GetNumSubjects);
		}
	}
}
