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

using System;using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Tao.Platform.Windows;
using utilities;
using System.IO;

namespace engine
{
	[TestFixture]
	public class EngineTest
	{
		OpenGLControlModded.simpleOpenGlControlEx m_Window = null;
		Zipper m_zipper = new Zipper();
		MapInfo m_map = null;
		string m_sMapPathOnDisk = "";
		bool m_bGlutInitted = false;

		[SetUp]
		public void Setup()
		{
            if(!STATE.UnitTesting) STATE.UnitTesting = true;

			m_Window = new OpenGLControlModded.simpleOpenGlControlEx();			
			m_Window.Width = 1920;
			m_Window.Height = 1200;
			m_Window.InitializeContexts();

			if (!m_bGlutInitted)
			{
				Tao.FreeGlut.Glut.glutInit();
				m_bGlutInitted = true;
			}

			m_map = new MapInfo("Test/test_bigbox.wrl", "", "", 0);
			m_sMapPathOnDisk = m_zipper.ExtractMap("Test/test_bigbox.wrl");
			m_map.GetMapPathOnDisk = m_sMapPathOnDisk;
		}

		[TearDown]
		public void TearDown()
		{
			File.Delete(m_sMapPathOnDisk);
		}

		[Test]
		public void TestMapLoad()
		{
			Player player = new Player(m_Window);

			player.LoadMap(m_map);
			player.Initialize();
			Assert.AreEqual(1, player.GetStaticFigList.Count());
			Figure fig = player.GetStaticFigList[0];
			Assert.AreEqual(12, fig.GetNumFaces);
		}

		[Test]
		public void TestIntersection()
		{
			Player player = new Player(m_Window);
			player.LoadMap(m_map);
			player.Initialize();

			MovableCamera cam = player.Cam;
			cam.PHI_DEG = 96;
			cam.THETA_DEG = -180;
			cam.Position = new D3Vect(6.36, -5.07, 2.51);

			List<IntersectionInfo> intersections = player.GetIntersectionInfos;
			Assert.AreEqual(0, intersections.Count);
			player.RightMouseDown();
			Assert.AreEqual(2, intersections.Count);
			bool bIndexSeven = false, bIndexTen = false;
			foreach(IntersectionInfo i in intersections) {
				if(i.Face.Index == 7) bIndexSeven = true;
				else if(i.Face.Index == 10) bIndexTen = true;
			}
			Assert.IsTrue(bIndexTen && bIndexSeven);
		}

		[Test]
		public void TestCanMove()
		{
			Player player = new Player(m_Window);
			player.LoadMap(m_map);
			player.Initialize();

			MovableCamera cam = player.Cam;

			cam.PHI_DEG = 90;
			cam.THETA_DEG = -180;
			cam.Position = new D3Vect(.64, -5.73, 1.52);
			Assert.IsFalse(player.MoveForward());

			cam.PHI_DEG = 140;
			cam.THETA_DEG = 96;
			cam.Position = new D3Vect(-5.71, -6.82, .86);
			Assert.IsFalse(player.MoveForward());

			cam.PHI_DEG = 81;
			cam.THETA_DEG = -5;
			cam.Position = new D3Vect(-4.14, -.82, .55);
			Assert.IsTrue(player.MoveForward());

			cam.PHI_DEG = 108;
			cam.THETA_DEG = -137;
			cam.Position = new D3Vect(-10.37, -10.45, .57);
			Assert.IsFalse(player.MoveForward());

			cam.PHI_DEG = 178;
			cam.THETA_DEG = -77;
			cam.Position = new D3Vect(-7.76, -4.12, 7.07);
			Assert.IsTrue(player.MoveForward());
		}
	}
}
