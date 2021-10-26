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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using utilities;
using engine;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.Win32;

#pragma warning disable CS1591

namespace sharpq3load_ui
{
	/// <summary>
	/// Select a map from this form to load
	/// </summary>
	public partial class MapChooserForm : Form
	{
		private MapInfo m_chosenMap = null;
		private Zipper m_zipper = new Zipper();
		Dictionary<string, string> m_LongMapNames = null;
		bool m_bChoseMap = false;
		bool m_bExittedProgram = false;

		/// <summary>
		/// Initialize a MapChooser
		/// </summary>
		public MapChooserForm()
		{
			InitializeComponent();
			Initialize();
		}

		/// <summary>
		/// Reads in all Quake 3 map data
		/// </summary>
		private void Initialize()
		{
			LoadArenasFile();

			List<string> lMapPathsInZip = GetMapPathsInZip();

			for (int i = 0; i < lMapPathsInZip.Count; i++)
				AddMapItem(lMapPathsInZip[i]);

			Sort(m_lstctf);
			Sort(m_lstdm);
			Sort(m_lsttrny);

			m_lsttest.Sort();

			const string userRoot = "HKEY_CURRENT_USER";
			const string subkey = "AppEvents\\Schemes\\Apps\\.Default\\CCSelect\\.current";
			const string keyName = userRoot + "\\" + subkey;

			string sVal = (string)Registry.GetValue(keyName, "", "");

			if (sVal == "")
			{
				Registry.SetValue(keyName, "", "");
			}
		}

		public void ClearChosenMap()
		{
			m_bChoseMap = false;
		}

		private void LoadArenasFile()
		{
			string sFullPath = m_zipper.ExtractFromPakToDefaultTempDir("scripts/arenas.txt");
			FileInfo arenas = new FileInfo(sFullPath);
			StreamReader sr = arenas.OpenText();
			m_LongMapNames = new Dictionary<string, string>();
			string sLine = "", sNick = "", sLong = "";
			string[] sTokens = null;

			int nCounter = 0;
			while (stringhelper.LookFor(sr, "{", ref nCounter))
			{
				stringhelper.FindFirstOccurence(sr, "map", ref sLine, ref nCounter);
				sTokens = stringhelper.Tokenize(sLine, '"');
				sNick = sTokens[1];
				stringhelper.FindFirstOccurence(sr, "longname", ref sLine, ref nCounter);
				sTokens = stringhelper.Tokenize(sLine, '"');
				sLong = sTokens[1];
				m_LongMapNames.Add(sNick, sLong);
			}

			sr.Close();
			File.Delete(sFullPath);
		}

		/// <summary>
		/// Sort by map number
		/// </summary>
		/// <param name="list">The list view to sort</param>
		private void Sort(System.Windows.Forms.ListView list)
		{
			List<MapInfo> lMaps = new List<MapInfo>();
			for (int i = 0; i < list.Items.Count; i++)
			{
				lMaps.Add((MapInfo)list.Items[i].Tag);
			}

			lMaps.Sort(new MapSorter());

			list.Clear();

			string sLabel;
			for (int j = 0; j < lMaps.Count; j++)
			{
				MapInfo curmap = lMaps[j];
				if (curmap.GetLongMapName != "") sLabel = curmap.GetLongMapName;
				else sLabel = curmap.GetNick;
				ListViewItem item = new ListViewItem(sLabel);
				item.Tag = curmap;
				list.Items.Add(item);
			}
		}

		private List<string> GetMapPathsInZip()
		{
			List<string> lPaths = new List<string>();
			using (ZipFile zipFile = new ZipFile(PATHS.GetMapsZipFile))
			{
				for (int i = 0; i < zipFile.Count; ++i)
				{
					ZipEntry e = zipFile[i];
					if (e.IsFile)
					{
						lPaths.Add(e.Name);
					}
				}
			}
			return lPaths;
		}

		private void AddMapItem(string sMapLocationInZip)
		{
			ListViewItem item = new ListViewItem();

			string sMapNameWithExtension = Path.GetFileName(sMapLocationInZip);
			string[] tokens = stringhelper.Tokenize(sMapNameWithExtension, '.');
			string sMapName = tokens[0];
			
			string sMapNum = sMapName.Substring(sMapName.Length - 2);
			if (!Char.IsNumber(sMapNum[0]))
				sMapNum = sMapNum.Substring(1);

			bool bQ3Map = false;
			if (sMapName.Substring(0, 2) == "q3")
			{
				bQ3Map = true;
				if (!m_LongMapNames.ContainsKey(sMapName)) 
					throw new Exception("Unable to find " + sMapName + " in dictionary of maps.");
				item.Tag = new MapInfo(sMapLocationInZip, sMapName, m_LongMapNames[sMapName], System.Convert.ToInt32(sMapNum));
				item.Text = m_LongMapNames[sMapName];
			}
			else
			{
				item.Tag = new MapInfo(sMapLocationInZip, sMapName, "", -1);
				item.Text = sMapName;
			}

			if (bQ3Map)
			{
				if (sMapName[2] == 'd')
					m_lstdm.Items.Add(item);
				else if (sMapName[2] == 'c')
					m_lstctf.Items.Add(item);
				else if (sMapName[2] == 't')
					m_lsttrny.Items.Add(item);
				else
					m_lsttest.Items.Add(item);
			}
			else
				m_lsttest.Items.Add(item);
		}

		private void ChooseMap(System.Windows.Forms.ListView list)
		{
			if (list.SelectedItems.Count > 0)
			{
				m_chosenMap = (MapInfo)(list.SelectedItems[0].Tag);
				m_bChoseMap = true;
				Close();
			}
		}

		/// <summary>
		/// Return the map that was chosen
		/// </summary>
		public MapInfo GetChosenMap
		{
			get { return m_chosenMap; }
		}

		private void m_lstdm_MouseClick(object sender, MouseEventArgs e)
		{
			ChooseMap(m_lstdm);
		}

		private void m_lstctf_MouseClick(object sender, MouseEventArgs e)
		{
			ChooseMap(m_lstctf);
		}

		private void m_lsttrny_MouseClick(object sender, MouseEventArgs e)
		{
			ChooseMap(m_lsttrny);
		}

		private void m_lsttest_MouseClick(object sender, MouseEventArgs e)
		{
			ChooseMap(m_lsttest);
		}

		private void m_lstdm_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyData == Keys.Enter || e.KeyData == Keys.Space)
				ChooseMap(m_lstdm);
		}

		private void m_lstctf_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter || e.KeyData == Keys.Space)
				ChooseMap(m_lstctf);
		}

		private void m_lsttrny_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter || e.KeyData == Keys.Space)
				ChooseMap(m_lsttrny);
		}

		private void m_lsttest_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter || e.KeyData == Keys.Space)
				ChooseMap(m_lsttest);
		}

        private void GetMapFromFile(bool bCollisionDetectionOn)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "PK3 Files (*.pk3)|*.pk3|VRML Files (*.wrl)|*.wrl";
            DialogResult result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                string sFile = dlg.FileName;
                m_chosenMap = new MapInfo(sFile);
				m_chosenMap.CollisionDetection = bCollisionDetectionOn;
                m_bChoseMap = true;
                Close();
            }
        }

        private void m_btnFromFile_Click(object sender, EventArgs e)
		{
            GetMapFromFile(true);
        }

        private void MapChooserForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!m_bChoseMap) m_chosenMap = null;
		}

        private void loadCustomMapNOCDToolStripMenuItem_Click(object sender, EventArgs e)
        {
			GetMapFromFile(false);
        }

		private void m_tsbtnExit_Click(object sender, EventArgs e)
		{
			m_chosenMap = null;
			Close();

			m_bExittedProgram = true;
		}

		public bool GetExittedProgram() { return m_bExittedProgram; }

		private void showHelp_Click(object sender, EventArgs e)
		{
            InfoForm nfo = new InfoForm();
            nfo.ShowDialog(this);
        }
	}

	/// <summary>
	/// Sorts mapinfos based on the map number
	/// </summary>
	public class MapSorter : IComparer<MapInfo>
	{
		/// <summary>
		/// Sort by mapnumber
		/// </summary>
		/// <param name="first">first map</param>
		/// <param name="second">second map</param>
		/// <returns>-1 if first less than second, 0 if equal, 1 otherwise</returns>
		public int Compare(MapInfo first, MapInfo second)
		{
			if (first.GetMapNumber < second.GetMapNumber) return -1;
			else if (first.GetMapNumber == second.GetMapNumber) return 0;
			else return 1;
		}
	}
}
