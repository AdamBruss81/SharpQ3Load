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
using System.IO;
using System.Collections.Generic;
using utilities;
using Tao.OpenGl;
using obsvr;

namespace engine
{
	/// <summary>
	/// Summary description for Shape.
	/// </summary>
    public class Shape : Subject
	{
		public enum ESignals { FACE_CREATED = SignalStarts.g_nShapeStart };
		public enum ETextureType { NONE, SINGLE, MULTI };

		List<Texture> m_lTextures;
        List<Face> m_lFaces = new List<Face>();
		List<List<int>> m_lCoordinateIndexes = new List<List<int>>();
        List<D3Vect> m_lMeshCoordinates = new List<D3Vect>();
		List<List<DPoint>> m_lTexCoordinates = new List<List<DPoint>>();
        List<D3Vect> m_lVerticeColors = new List<D3Vect>();

		const string m_sVerticeColorHeader = "color Color { color [";
		const string m_sTextureCoordinatesHeader = "TextureCoordinate { point [";
		const string m_sChannelOneTextureCoordinatesHeader = "texCoord  TextureCoordinate { point [";
		const string m_sMeshCoordinatesHeader = "coord Coordinate { point [";
		const string m_sCoordinateIndexHeader = "coordIndex [";

		ETextureType m_TextureType;

		public Shape() {}

		public void Delete()
		{
			foreach(Texture t in m_lTextures) {
				t.Delete();
			}
			foreach(Face f in m_lFaces) {
				f.Delete();
			}
		}

		public void InitializeLists()
		{
			foreach (Texture t in m_lTextures)
			{
				t.InitializeLists();
			}
			foreach (Face f in m_lFaces)
			{
				f.InitializeLists();
			}
		}

		public void ReadMain(List<Texture> lTextures, StreamReader sr, List<Face> lFaceReferences, ref int nCounter)
		{
			m_lTextures = new List<Texture>(lTextures);
			m_TextureType = lTextures.Count == 2 ? Shape.ETextureType.MULTI : Shape.ETextureType.SINGLE;

			if (Read(sr, ref nCounter))
			{
				CreateFaces(lFaceReferences);	
			}
			else 
				throw new Exception("Error in reading shape data from file");
		}

		private void MultiTextureDraw()
		{
			Gl.glActiveTexture(Gl.GL_TEXTURE0);
			Gl.glEnable(Gl.GL_TEXTURE_2D);
			m_lTextures[0].bindMe();

			Gl.glActiveTexture(Gl.GL_TEXTURE1);
			Gl.glEnable(Gl.GL_TEXTURE_2D);
			m_lTextures[1].bindMe();
		}

		private void DrawSingleTexture()
		{
			if (m_TextureType == ETextureType.MULTI)
			{
				Gl.glActiveTexture(Gl.GL_TEXTURE0);
				Gl.glEnable(Gl.GL_TEXTURE_2D);
				m_lTextures[1].bindMe();
			}
			else if (m_TextureType == ETextureType.SINGLE) 
			{
				Gl.glActiveTexture(Gl.GL_TEXTURE0);
				Gl.glEnable(Gl.GL_TEXTURE_2D);
				m_lTextures[0].bindMe();
			}
		}

		private void CreateFaces(List<Face> lFaceReferences)
		{
			List<D3Vect> faceVerts = new List<D3Vect>(); ;
			List<List<DPoint>> faceTexCoords = new List<List<DPoint>>();
			faceTexCoords.Add(new List<DPoint>());
			if (m_lTextures.Count > 1) faceTexCoords.Add(new List<DPoint>());
			List<D3Vect> faceVertColors = new List<D3Vect>();
			Face pFace = null;

			for (int i = 0; i < m_lCoordinateIndexes.Count; i++)
			{
				for (int j = 0; j < m_lCoordinateIndexes[i].Count; j++)
				{
					faceVerts.Add(m_lMeshCoordinates[m_lCoordinateIndexes[i][j]]);
					faceTexCoords[0].Add(m_lTexCoordinates[0][m_lCoordinateIndexes[i][j]]);
					if(m_TextureType == ETextureType.MULTI) 
						faceTexCoords[1].Add(m_lTexCoordinates[1][m_lCoordinateIndexes[i][j]]);
					faceVertColors.Add(m_lVerticeColors[m_lCoordinateIndexes[i][j]]);
				}
				LOGGER.Debug("Allocating map face");
				pFace = new Face(faceVerts, faceTexCoords, faceVertColors, new Color(240, 0, 0), new Color(100, 0, 0), lFaceReferences.Count);
                pFace.SetParentShape(this);
				m_lFaces.Add(pFace);
				lFaceReferences.Add(pFace);
				LOGGER.Debug("Added a face to the figure's map face references. Count = " + lFaceReferences.Count.ToString());
				Notify((int)ESignals.FACE_CREATED);
				pFace = null;
				faceVerts.Clear();
				faceTexCoords[0].Clear();
				if(m_TextureType == ETextureType.MULTI) 
					faceTexCoords[1].Clear();
				faceVertColors.Clear();
			}
		}

        public List<Texture> GetTextures() { return m_lTextures; }

		/// <summary>
		/// Shows this shape. Loop over texture objects and set same number
		/// of texture units.
		/// </summary>
		public void Show(Engine.EGraphicsMode mode, ref int nNumFacesRendered)
        {
			if (STATE.DebuggingMode)
			{
				if(mode == Engine.EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR || mode == Engine.EGraphicsMode.SINGLE_WHITE) 
				{
					DrawSingleTexture();
				}
				else if(mode == Engine.EGraphicsMode.MULTI_TEXTURE_WHITE)
				{
					if (m_TextureType == ETextureType.MULTI)
						MultiTextureDraw();
					else if (m_TextureType == ETextureType.SINGLE)
						DrawSingleTexture();
				}

				foreach(Face f in m_lFaces) {
					if(!f.RenderedThisPass) f.Draw(mode, ref nNumFacesRendered);
				}
			}
			else if (mode == Engine.EGraphicsMode.MULTI_TEXTURE_WHITE)
			{
				if (m_TextureType == ETextureType.MULTI)
				{
					MultiTextureDraw();
					foreach(Face f in m_lFaces)
					{
						if(!f.RenderedThisPass) f.Draw(Engine.EGraphicsMode.MULTI_TEXTURE_WHITE, ref nNumFacesRendered);
					}
				}
				else if (m_TextureType == ETextureType.SINGLE)
				{
					Gl.glActiveTexture(Gl.GL_TEXTURE0);
					Gl.glEnable(Gl.GL_TEXTURE_2D);
					m_lTextures[0].bindMe();

					foreach (Face f in m_lFaces)
					{
						if(!f.RenderedThisPass) f.Draw(Engine.EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR, ref nNumFacesRendered);
					}
				}
			}
			else if (mode == Engine.EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR)
			{
				if (m_TextureType == ETextureType.MULTI)
				{				
					Gl.glActiveTexture(Gl.GL_TEXTURE0);
					Gl.glEnable(Gl.GL_TEXTURE_2D);
					m_lTextures[1].bindMe();

					foreach (Face f in m_lFaces)
					{
						if(!f.RenderedThisPass) f.Draw(Engine.EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR, ref nNumFacesRendered);
					}
				}
				else if (m_TextureType == ETextureType.SINGLE)
				{
					Gl.glActiveTexture(Gl.GL_TEXTURE0);
					Gl.glEnable(Gl.GL_TEXTURE_2D);
					m_lTextures[0].bindMe();

					foreach (Face f in m_lFaces)
					{
						if(!f.RenderedThisPass) f.Draw(Engine.EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR, ref nNumFacesRendered);
					}
				}
			}
			else if (mode == Engine.EGraphicsMode.SINGLE_WHITE)
			{
				Gl.glActiveTexture(Gl.GL_TEXTURE0);
				Gl.glEnable(Gl.GL_TEXTURE_2D);

				if (m_TextureType == ETextureType.MULTI)
				{					
					m_lTextures[1].bindMe();
				}
				else if(m_TextureType == ETextureType.SINGLE)
				{
					m_lTextures[0].bindMe();
				}

				foreach (Face f in m_lFaces)
				{
					if(!f.RenderedThisPass) f.Draw(Engine.EGraphicsMode.SINGLE_WHITE, ref nNumFacesRendered);
				}
			}
			else if (mode == Engine.EGraphicsMode.WIREFRAME)
			{
				foreach (Face f in m_lFaces)
				{
					if(!f.RenderedThisPass) f.Draw(Engine.EGraphicsMode.WIREFRAME, ref nNumFacesRendered);
				}
			}
        }

		/// <summary>
		/// Get all faces
		/// </summary>
		/// <param name="lFaces">list to put faces in</param>
		public void GetFaces(List<Face> lFaces)
		{
			for (int i = 0; i < m_lFaces.Count; i++)
				lFaces.Add(m_lFaces[i]);
		}

		/// <summary>
		/// Reads in a single shape from the passed StreamReader
		/// </summary>
		/// <param m_DisplayName="sr">StreamReader of VRML 2.0 compliant file containing
		/// a shape to be processed.</param>
		/// <returns>true if shape is successfully created, false otherwise.
		/// </returns>
		private bool Read(StreamReader sr, ref int nCounter)
		{
			if(!ReadCoordinateIndexes(sr, ref nCounter)) return false;
			if(!ReadMeshCoordinates(sr, ref nCounter)) return false;
			m_lTexCoordinates.Add(new List<DPoint>());
			if (!ReadTextureCoordinates(sr, m_lTexCoordinates[0], ref nCounter)) return false;
			if (m_TextureType == ETextureType.MULTI)
			{
				m_lTexCoordinates.Add(new List<DPoint>());
				if (!ReadTextureCoordinates(sr, m_lTexCoordinates[1], ref nCounter)) return false;
			}
			if (!ReadVerticeColors(sr, ref nCounter)) return false;
			return true;
		}

		private bool ReadVerticeColors(StreamReader sr, ref int nCounter)
		{
			if (!stringhelper.LookFor(sr, m_sVerticeColorHeader, ref nCounter))
			{
				LOGGER.Error("Unable to find " + m_sVerticeColorHeader);
				return false;
			}
			else
			{
				string inLine = sr.ReadLine();
				nCounter++;
				string[] sTokens;
				while (inLine.IndexOf(']') == -1)
				{
					sTokens = stringhelper.Tokenize(inLine, ',');
					for (int i = 0; i < sTokens.Length - 1; i++)
						m_lVerticeColors.Add(new D3Vect(sTokens[i]));
					inLine = sr.ReadLine();
					nCounter++;
				}
				inLine = inLine.Substring(0, inLine.Length - 2);
				sTokens = stringhelper.Tokenize(inLine, ',');
				for (int i = 0; i < sTokens.Length; i++)
					m_lVerticeColors.Add(new D3Vect(sTokens[i]));

				return true;
			}
		}

		private bool ReadTextureCoordinates(StreamReader sr, List<DPoint> lTextureCoordinates, ref int nCounter)
		{
			if (!stringhelper.LookFor(sr, m_sTextureCoordinatesHeader, ref nCounter) &&
				!stringhelper.LookFor(sr, m_sChannelOneTextureCoordinatesHeader, ref nCounter))
			{
				LOGGER.Error("Unable to find " + m_sTextureCoordinatesHeader + " and " + m_sChannelOneTextureCoordinatesHeader);
				return false;
			}
			else
			{
				string inLine = sr.ReadLine();
				nCounter++;
				string[] sTokens;
				while (inLine.IndexOf(']') == -1)
				{
					sTokens = stringhelper.Tokenize(inLine, ',');
					for (int i = 0; i < sTokens.Length - 1; i++)
						lTextureCoordinates.Add(new DPoint(sTokens[i]));
					inLine = sr.ReadLine();
					nCounter++;
				}
				inLine = inLine.Substring(0, inLine.Length - 2);
				sTokens = stringhelper.Tokenize(inLine, ',');
				for (int i = 0; i < sTokens.Length; i++)
					lTextureCoordinates.Add(new DPoint(sTokens[i]));

				return true;
			}
		}

		private bool ReadCoordinateIndexes(StreamReader sr, ref int nCounter)
		{
			if (!stringhelper.LookFor(sr, m_sCoordinateIndexHeader, ref nCounter))
			{
				LOGGER.Error("Unable to find " + m_sCoordinateIndexHeader);
				return false;
			}
			else // found the coordinate indexes
			{
				string[] sTokens;
				List<int> lIndexes;
				string inLine = sr.ReadLine();
				nCounter++;
				while (inLine.IndexOf(']') == -1)
				{
					sTokens = stringhelper.Tokenize(inLine, ',');
					lIndexes = new List<int>();
					for (int i = 0; i < sTokens.Length - 2; i++)
						lIndexes.Add(Convert.ToInt32(sTokens[i]));
					m_lCoordinateIndexes.Add(lIndexes);
					inLine = sr.ReadLine();
					nCounter++;
				}
				sTokens = stringhelper.Tokenize(inLine, ',');
				lIndexes = new List<int>();
				for (int i = 0; i < sTokens.Length - 1; i++)
					lIndexes.Add(Convert.ToInt32(sTokens[i]));
				m_lCoordinateIndexes.Add(lIndexes);

				return true;
			}
		}

		/// <summary>
		/// Read Vertices
		/// </summary>
		/// <param name="sr">StreamReader to use</param>
		/// <returns></returns>
		private bool ReadMeshCoordinates(StreamReader sr, ref int nCounter)
		{
			if (!stringhelper.LookFor(sr, m_sMeshCoordinatesHeader, ref nCounter))
			{
				LOGGER.Error("Unable to find " + m_sMeshCoordinatesHeader);
				return false;
			}
			else
			{
				string inLine = sr.ReadLine();
				nCounter++;
				string[] sTokens;
				while (inLine.IndexOf(']') == -1)
				{
					sTokens = stringhelper.Tokenize(inLine, ',');
					for (int i = 0; i < sTokens.Length - 1; i++)
					{
						D3Vect vert = new D3Vect(sTokens[i]);
						// Reflect X values over Y-Z plane because Q3BSP reflects them for some reason when it
						// converts .bsp files into .vrmls. So here we reflect it back so the map matches what it looks like
						// in GTKQ3Radiant.
						vert[0] *= -1;
						double tempY = vert[1];
						vert[1] = vert[2];
						vert[2] = tempY;
						m_lMeshCoordinates.Add(vert);
					}
					inLine = sr.ReadLine();
					nCounter++;
				}
				inLine = inLine.Substring(0, inLine.Length - 2);
				sTokens = stringhelper.Tokenize(inLine, ',');
				for (int i = 0; i < sTokens.Length; i++)
				{
					D3Vect vert = new D3Vect(sTokens[i]);
					// Reflect X values over Y-Z plane because Q3BSP reflects them for some reason when it
					// converts .bsp files into .vrmls. So here we reflect it back so the map matches what it looks like
					// in GTKQ3Radiant.
					vert[0] *= -1;
					double tempY = vert[1];
					vert[1] = vert[2];
					vert[2] = tempY;
					m_lMeshCoordinates.Add(vert);
				}

				return true;
			}
		}
    }
}
