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
using System.IO;

namespace engine
{
	/// <summary>
	/// Represents a vrml quake map
	/// </summary>
	public class MapInfo
	{
		private string m_sPath;
		private string m_sNick;
		private int m_nNumber;
		private string m_sLongMapName;
		private string m_sMapPathOnDisk;
		private bool m_bExtractedFromZip;
		
		public MapInfo(string sPath, string sNick, string sLongName, int nMapNumber)
		{
			m_sPath = sPath;
			m_sNick = sNick;
			m_nNumber = nMapNumber;
			m_sLongMapName = sLongName;
			m_sMapPathOnDisk = "";
			m_bExtractedFromZip = true;
		}

		public MapInfo(string sFullPath)
		{
			m_sPath = Path.GetFileName(sFullPath);
			m_sNick = null;
			m_nNumber = -1;
			m_sLongMapName = null;
			m_sMapPathOnDisk = sFullPath;
			m_bExtractedFromZip = false;
		}

		public MapInfo()
		{
			m_sPath = "";
			m_sNick = "";
			m_sLongMapName = "";
			m_nNumber = -1;
			m_bExtractedFromZip = true;
		}

		public void ConvertToWRL(string wrlPath)
        {
			m_sPath = Path.GetFileName(wrlPath);
			m_sMapPathOnDisk = wrlPath;
		}

		public override string ToString()
		{
			if (m_sLongMapName == null)
			{
				if (m_sNick == null)
					return m_sPath;
				else
					return m_sNick;
			}
			else
				return m_sLongMapName; 
		}

		public void CleanUpMap()
		{
			if(m_bExtractedFromZip) File.Delete(GetMapPathOnDisk);
		}

		public bool ExtractedFromZip
		{
			get { return m_bExtractedFromZip; }
		}
		public int GetMapNumber { get { return m_nNumber; } }
		public string GetPath { get { return m_sPath; }	}
		public string GetNick {	get { return m_sNick; }	}
		public string GetLongMapName { get { return m_sLongMapName; } }
		public string GetMapPathOnDisk
		{
			get { return m_sMapPathOnDisk; }
			set { m_sMapPathOnDisk = value; }
		}
	}
}
