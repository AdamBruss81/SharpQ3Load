﻿//*===================================================================================
//* ----||||Simulator||||----
//*
//* By Adam Bruss and Scott Nykl
//*
//* Scott participated in Fall of 2005. Adam has participated from fall 2005 
//* until the present.
//*
//* Loads in quake 3 m_maps. Three modes of interaction are Player, Ghost and Spectator.
//*===================================================================================
using System.IO;
using System;
using Microsoft.VisualBasic.FileIO;
using System.Reflection;

namespace engine
{
	/// <summary>
	/// State machine switches
	/// </summary>
	public class STATE
	{
		private static bool m_bDebuggingMode = false;
		private static bool m_bShowDebuggingFaces = false;
		private static bool m_bDrawFaceNormals = false;
		private static bool m_bAllowPrinting = false;
		private static bool m_bGravity = true;

		public static bool AllowPrinting
		{
			get { return m_bAllowPrinting; }
			set { m_bAllowPrinting = value; }
		}

		public static bool DebuggingMode
		{
			get { return m_bDebuggingMode; }
			set { m_bDebuggingMode = value; }
		}

		public static bool ShowDebuggingFaces
		{
			get { return m_bShowDebuggingFaces; }
			set { m_bShowDebuggingFaces = value; }
		}

		public static bool DrawFaceNormals
		{
			get { return m_bDrawFaceNormals; }
			set { m_bDrawFaceNormals = value; }
		}

		public static bool Gravity
		{
            get { return m_bGravity; }
            set { m_bGravity = value; }
        }
	}

	public class LOGGER
	{
		private static StreamWriter m_writer = null;

		private static void Write(string sType, string sMessage)
		{
			if (m_writer == null) m_writer = new StreamWriter(PATHS.GetLogFile);

			m_writer.Write(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss:ffff") + " : " + sType + " : ");
			m_writer.WriteLine(sMessage);
			m_writer.Flush();
		}

		public static void Info(string sInfo)
		{
			Write("INFO", sInfo);
		}

		public static void Debug(string sDebugMsg)
		{
#if DEBUG
			Write("DEBUG", sDebugMsg);
#endif // DEBUG
		}

		public static void Error(string sError)
		{
			Write("ERROR", sError);
		}

		public static void Warning(string sWarning)
		{
			Write("WARNING", sWarning);
		}

		public static void Close()
		{
			if (m_writer != null) m_writer.Close();
		}
	}

	public class NAMES
	{
		public static string GetPak0Name
		{
			get { return "baseq3/pak0.pk3"; }
		}

        public static string GetLightmapName
        {
            get { return "lightmaps.zip"; }
        }

        public static string GetMapsName
		{
			get { return "maps.zip"; }
		}

		public static string GetTempFolderName
		{
			get { return "Simulator"; }
		}

		public static string GetLogFileName
		{
			get { return "Simulator-Log.txt"; }
		}
	}

	public class PATHS
	{
		public static string GetTempDir
		{
			get { return Path.Combine(Path.GetTempPath(), NAMES.GetTempFolderName); }
		}

		public static string GetProgramDataDir
		{
			get { return SpecialDirectories.AllUsersApplicationData; }
		}

		public static string GetLogFile
		{
			get { return Path.Combine(SpecialDirectories.AllUsersApplicationData, NAMES.GetLogFileName); }
		}

		public static string GetProgramDataMapsZipFile
		{
			get
			{
				return Path.Combine(GetProgramDataDir, NAMES.GetMapsName);
			}
		}

		public static string GetPak0Path
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), NAMES.GetPak0Name);
			}
		}

		public static string GetLightmapPath
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), NAMES.GetLightmapName);
			}
		}

		public static string GetSourceMapsZipFile
		{
			get { return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), NAMES.GetMapsName); }
		}
	}

	public class SignalStarts
	{
		public const int g_nFigureStart = 100;
		public const int g_nShapeStart = 200;
		public const int g_nEngineStart = 300;
	}

	public class GameGlobals
	{
		public static System.Diagnostics.Stopwatch m_SW = new System.Diagnostics.Stopwatch();
		public static utilities.D3Vect m_CamPosition = new utilities.D3Vect();
		public static float m_fFrameStartElapsedS = 0f;
		public static long m_fFrameStartElapsedMS = 0;
		public static float[] m_SinTable = new float[1024];

		public static long GetElapsedMS() { return m_fFrameStartElapsedMS; }
		public static float GetElapsedS() { return m_fFrameStartElapsedS; }
		public static string GetBaseLightmapScale() { return "2.5"; }
		public static string GetBaseVertexColorScale() { return "3.0"; }

		public static bool IsPortalEntry(string sPath) { return sPath.Contains("sfx/portal_sfx.jpg"); }

		public static void InitTables()
		{
			for (int i = 0; i < 1024; i++)
			{
				// sin( DEG2RAD( i * 360.0f / ( ( float ) ( FUNCTABLE_SIZE - 1 ) ) ) );
				m_SinTable[i] = (float)Math.Sin(utilities.GLB.GoDegToRad(i * 360.0 / 1023.0));
			}
		}
	}
}