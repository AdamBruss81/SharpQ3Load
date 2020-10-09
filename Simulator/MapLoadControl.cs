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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using obsvr;
using System.IO;
using System.Threading;
using engine;
using System.Diagnostics;
using utilities;

namespace simulator
{		
	/// <summary>
	/// Progress Control for map loading
	/// </summary>
	public partial class MapLoadControl : UserControl
	{
		delegate void StringParameterDelegate(string value);

		int m_nMaxLineCount = 0;
		int m_nMaxShapes = 0;
		int m_nMaxBoundingBoxes = 0;
		int m_nProgressCutoffLoad = 0;
		int m_nProgressCutoffBoxes = 0;
		int m_nProgressiveProgressBarMin = 0;
		int m_nNumPeriods = 0;
		Int64 m_nSleepCounter = 0;
		bool m_bDoneLoadingMap = false;
		bool m_bDoneInitializingLists = false;
		bool m_bDoneCreatingBoundingBoxes = false;
		bool m_bInitializedBoxFlag = false;
		bool m_bLoading = false;
		MapUpdater m_pUpdater = null;
		string m_sDetails = "";
		Zipper m_zip = new Zipper();

		WaitCursor m_WaitCursor = null;

		/// <summary>
		/// Initialize map loader
		/// </summary>
		public MapLoadControl()
		{
			InitializeComponent();
			m_pUpdater = new MapUpdater(this);
		}

		/// <summary>
		/// Return whether the control is still loading
		/// </summary>
		public bool Loading
		{
			get { return m_bLoading; }
			set { m_bLoading = value; }
		}

		/// <summary>
		/// Become aware that the map is finished loading
		/// </summary>
		public void DoneLoadingMap()
		{
			m_bDoneLoadingMap = true;
			LOGGER.Info("Done loading " + SimulatorForm.static_theMap.ToString());
		}

		/// <summary>
		/// Become aware that lists are finished initializing
		/// </summary>
		public void DoneInitializingLists()
		{
			m_bDoneInitializingLists = true;
			LOGGER.Info("Done initializing lists for " + SimulatorForm.static_theMap.ToString());
		}

		/// <summary>
		/// Become aware that lists are finished initializing
		/// </summary>
		public void DoneCreatingBoundingBoxes()
		{
			m_bDoneCreatingBoundingBoxes = true;
			LOGGER.Info("Done creating boundingboxes for " + SimulatorForm.static_theMap.ToString());
		}

		//delegate void stringArg(string arg);

		/// <summary>
		/// Sets the details label
		/// </summary>
		public void Details(string sDetailMsg)
		{
			m_sDetails = sDetailMsg;
			m_nNumPeriods = 0;
		}

		private string GetDetails()
		{
			string details = m_sDetails + " ";
			for (int i = 0; i < m_nNumPeriods; i++)
			{
				details += ".";
			}
			return details;
		}

		/// <summary>
		/// Get/Set status label
		/// </summary>
		public string Status
		{
			get { return m_lblStatus.Text; }
			set { m_lblStatus.Text = value; }
		}

		private void SetProgressBar(int nInputMax, int nInputProgress, int nProgressValueMin, int nProgressValueMax)
		{
			double dDenom = ((double)nInputMax / (double)(nProgressValueMax - nProgressValueMin));
			if (dDenom == 0 || System.Double.IsNaN(dDenom)) dDenom = 1;
			int nVal = (int)((double)nInputProgress / dDenom) + nProgressValueMin;
			if (nVal < nProgressValueMin)
				throw new Exception("Invalid value for progress bar " + Convert.ToString(nVal));
			else if (nVal > nProgressValueMax)
				nVal = nProgressValueMax;
			if (m_progress.Value != nVal)
			{
				m_progress.Value = nVal;
				m_lblPercentage.Text = Convert.ToString(m_progress.Value) + "%";
			}
		}

		/// <summary>
		/// Ready for next map load
		/// </summary>
		public void Reset()
		{
			LOGGER.Info("Resetting progressbar member variables");

			m_nMaxLineCount = 0;
			m_nMaxShapes = 0;
			m_nMaxBoundingBoxes = 0;
			m_nProgressCutoffLoad = 0;
			m_nProgressCutoffBoxes = 0;
			m_nProgressiveProgressBarMin = 0;
			m_bDoneLoadingMap = false;
			m_bDoneInitializingLists = false;
			m_bDoneCreatingBoundingBoxes = false;
			m_bInitializedBoxFlag = false;
			m_bLoading = false;
			m_WaitCursor = null;

			m_progress.Value = 0;
		}

		public void UpdateProgress(string sMsg)
		{
			if (InvokeRequired)
			{
				Invoke(new StringParameterDelegate(UpdateProgress), new object[] { sMsg });
			}
			else
			{
				if(!string.IsNullOrEmpty(sMsg))
				{
					m_sDetails = sMsg;
				}

				string sDetails = GetDetails();
				if (m_lblDetail.Text != sDetails) m_lblDetail.Text = sDetails;
				m_nSleepCounter++;
				if (m_nSleepCounter % 25 == 0) m_nNumPeriods++;
				if (m_nNumPeriods > 4) m_nNumPeriods = 0;
				if (m_nSleepCounter == Int64.MaxValue) m_nSleepCounter = 0;

				/*LOGGER.Debug("Progress bar loop cycle: m_bDoneLoadingMap=" + Convert.ToString(m_bDoneLoadingMap) + ", " +
					"m_bDoneInitializingLists=" + Convert.ToString(m_bDoneInitializingLists) +
					", m_bDoneCreatingBoundingBoxes=" + Convert.ToString(m_bDoneCreatingBoundingBoxes)); */

				//Thread.Sleep(100);

				FigureList lFigList = SimulatorForm.static_theEngine.GetStaticFigList;

				if (lFigList.Count() > 0)
				{
					if (m_WaitCursor == null) m_WaitCursor = new WaitCursor();

					if (!m_bInitializedBoxFlag && !m_bDoneCreatingBoundingBoxes)
					{
						m_bDoneCreatingBoundingBoxes = lFigList[0].GetUpToDateBoundingBoxes;
						LOGGER.Info("Setting m_bDoneCreatingBoundingBoxes to " + Convert.ToString(m_bDoneCreatingBoundingBoxes));
						m_bInitializedBoxFlag = true;
					}

					if (!m_bDoneLoadingMap)
					{
						SetProgressBar(m_nMaxLineCount, lFigList[0].GetMapReadLinePosition, m_nProgressiveProgressBarMin, m_nProgressCutoffLoad);
					}
					else if (!m_bDoneCreatingBoundingBoxes)
					{
						if (m_nMaxBoundingBoxes == 0) m_nMaxBoundingBoxes = lFigList[0].GetNumBoundingBoxes;
						if (m_nProgressiveProgressBarMin == 0) m_nProgressiveProgressBarMin = m_nProgressCutoffLoad;
						SetProgressBar(m_nMaxBoundingBoxes, 1, m_nProgressiveProgressBarMin, m_nProgressCutoffBoxes);
					}
					else if (!m_bDoneInitializingLists)
					{
						if (m_nMaxShapes == 0) m_nMaxShapes = lFigList[0].GetNumShapes;
						if (m_nProgressiveProgressBarMin == 0) m_nProgressiveProgressBarMin = m_nProgressCutoffLoad + 1;
						else if (m_nProgressiveProgressBarMin == m_nProgressCutoffLoad) m_nProgressiveProgressBarMin = m_nProgressCutoffBoxes;
						SetProgressBar(m_nMaxShapes, lFigList[0].GetDisplayListCreationProgress, m_nProgressiveProgressBarMin, m_progress.Maximum);
					}
				}
			}
		}

		/// <summary>
		/// Start Progress
		/// </summary>
		public void Begin_MainThread()
		{
			LOGGER.Info("Entered Begin function in maploadprogress control");

			string sFullPath = m_zip.ExtractSoundTextureOther("levelshots/" +
				System.IO.Path.GetFileNameWithoutExtension(SimulatorForm.static_theMap.GetNick) + ".jpg");

			if (File.Exists(sFullPath)) {
				m_picLevelShot.Image = Image.FromFile(m_zip.ExtractSoundTextureOther("levelshots/" +
					System.IO.Path.GetFileNameWithoutExtension(SimulatorForm.static_theMap.GetNick) + ".jpg"));
			}
			else
			{
				m_picLevelShot.Image = Image.FromFile(m_zip.ExtractSoundTextureOther("menu/art/unknownmap.jpg"));                    
            }

			m_bLoading = true;

			Status = "-+ " + SimulatorForm.static_theMap.ToString() + " +-";
			m_progress.Maximum = 100;
			m_nProgressCutoffLoad = 25;
			m_nProgressCutoffBoxes = 70;
			m_nMaxLineCount = (int)utilities.stringhelper.CountLinesInFile(SimulatorForm.static_theMap.GetMapPathOnDisk);

			FigureList lFigList = SimulatorForm.static_theEngine.GetStaticFigList;
			while (lFigList.Count() == 0) Thread.Sleep(10);
			
			m_pUpdater.Subscribe(lFigList[0].GetSubject);

			LOGGER.Info("Entering while loop of mapload progress control");

			while (!m_bDoneLoadingMap || !m_bDoneInitializingLists || !m_bDoneCreatingBoundingBoxes)
			{				
				Thread.Sleep(20);
				Application.DoEvents();				
			}			

			Reset();

			if (m_WaitCursor != null) m_WaitCursor.Dispose();

			m_bLoading = false;

			LOGGER.Info("Exiting Begin function in maploadprogress control.");
		}
	}

    public class MapUpdater : Observer
    {
        MapLoadControl m_pMLC = null;

        public MapUpdater(MapLoadControl mlc)
        {
            m_pMLC = mlc;
        }

        public override void Update(Change pChange)
        {
            // m_pMLC.Update() ...
            if (pChange.GetCode == (int)(Figure.ESignals.SHAPE_READ))
            {
                m_pMLC.UpdateProgress(pChange.GetMsg);
            }
        }
    }
}
