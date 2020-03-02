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

namespace obsvr.Test
{
	public class Watcher : Observer
	{
		public static int g_nNumUpdatedYelled = 0;
		public static int g_nNumUpdatedWhispered = 0;

		public override void Update(Change pChange)
		{
			if (pChange.GetCode == (int)Signaler.Signals.WHISPERED)
				g_nNumUpdatedWhispered++;
			else if (pChange.GetCode == (int)Signaler.Signals.YELLED)
				g_nNumUpdatedYelled++;
		}
	}
}
