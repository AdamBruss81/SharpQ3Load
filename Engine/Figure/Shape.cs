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
using System.Diagnostics;
using utilities;
using obsvr;
using OpenTK.Graphics.OpenGL;

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
        List<D3Vect> m_lVertices = new List<D3Vect>();
		List<List<DPoint>> m_lTexCoordinates = new List<List<DPoint>>();
        List<D3Vect> m_lVerticeColors = new List<D3Vect>();
		private Zipper m_zipper = new Zipper();
		Q3Shader m_q3Shader = new Q3Shader();

		// modern open gl constructs
		double[] m_arVertices = null;
		uint[] m_arIndices = null;
        int ShaderProgram;
        int VertexBufferObject;
        int VertexArrayObject;
        int ElementBufferObject;
		// ===

		const string m_sVerticeColorHeader = "color Color { color [";
		const string m_sTextureCoordinatesHeader = "TextureCoordinate { point [";
		const string m_sChannelOneTextureCoordinatesHeader = "texCoord  TextureCoordinate { point [";
		const string m_sMeshCoordinatesHeader = "coord Coordinate { point [";
		const string m_sCoordinateIndexHeader = "coordIndex [";
		private const string g_sDefaultTexture = "textures/base_floor/clang_floor.jpg";

		ETextureType m_TextureType;

        public Shape() { }

		public void Delete()
		{
			ShaderHelper.CloseProgram(ShaderProgram);

			foreach(Texture t in m_lTextures) {
				t.Delete();
			}
			foreach(Face f in m_lFaces) {
				f.Delete();
			}
		}

		public Q3Shader GetQ3Shader()
		{
			return m_q3Shader;
		}

		private string GetPathToTextureNoShaderLookup(bool bLightmap, string sURL)
		{
            string sFullPath;

			if(bLightmap)
			{
				sFullPath = m_zipper.ExtractLightmap(sURL);
			}
			else
			{
				sFullPath = m_zipper.ExtractSoundTextureOther(sURL);
				
				if(!File.Exists(sFullPath))
				{
                    // try to find texture as tga or jpg
					// when quake 3 was near shipping, id had to convert some tgas to jpg to reduce pak0 size					
					if(Path.GetExtension(sFullPath) == ".jpg")
						sFullPath = m_zipper.ExtractSoundTextureOther(Path.ChangeExtension(sURL, "tga"));
					else
						sFullPath = m_zipper.ExtractSoundTextureOther(Path.ChangeExtension(sURL, "jpg"));

					if (!File.Exists(sFullPath))
                    {
                        if (sFullPath.Contains("nightsky_xian_dm1"))
                            sFullPath = m_zipper.ExtractSoundTextureOther("env/xnight2_up.jpg");
                    }
                }				
            }   
			
			return sFullPath;
        }

		public void InitializeLists()
		{
			m_q3Shader.ReadShader(GetMainTexture().GetPath());			

			foreach (Texture t in m_lTextures)
			{
				if (GetMainTexture() == t) {
					string sNonShaderTexture = GetPathToTextureNoShaderLookup(false, t.GetPath());
					if(File.Exists(sNonShaderTexture))
						t.SetTexture(sNonShaderTexture);
					else
						t.SetTexture(m_q3Shader.GetShaderBasedMainTextureFullPath());
				} 
				else t.SetTexture(GetPathToTextureNoShaderLookup(true, t.GetPath()));
			}

            // hardcode something for killblock_i4b shader for now to test
            if (m_q3Shader.GetShaderName().Contains("killblock_i4b"))
            {
                if (m_q3Shader.GetStages().Count == 3)
                {
                    m_lTextures.Add(new Texture(m_q3Shader.GetStages()[2].GetTexturePath()));
					m_lTextures[m_lTextures.Count - 1].SetTexture(GetPathToTextureNoShaderLookup(false, m_lTextures[m_lTextures.Count - 1].GetPath()));
                }
            }

            foreach (Face f in m_lFaces)
			{
				f.InitializeLists();
			}

			// use modern open gl via vertex buffers, vertex array, element buffer and shaders
			// setup vertices
			int nNumValues = 11;
            m_arVertices = new double[m_lVertices.Count * nNumValues]; // vertices, texcoord1, texcoord2(could be dummy if no lightmap), color
			for(int i = 0; i < m_lVertices.Count; i++)
			{
				int nBase = i * nNumValues;

				// vertices
				m_arVertices[nBase] = m_lVertices[i].x;
				m_arVertices[nBase + 1] = m_lVertices[i].y;
				m_arVertices[nBase + 2] = m_lVertices[i].z;

				// main texture coordinates
				m_arVertices[nBase + 3] = m_lTexCoordinates[GetMainTextureIndex()][i].Vect[0];
				m_arVertices[nBase + 4] = m_lTexCoordinates[GetMainTextureIndex()][i].Vect[1];

				if(m_lTextures.Count > 1)
				{
					// lightmap texture coordinates
                    m_arVertices[nBase + 5] = m_lTexCoordinates[GetLightmapTextureIndex()][i].Vect[0];
                    m_arVertices[nBase + 6] = m_lTexCoordinates[GetLightmapTextureIndex()][i].Vect[1];
                }
				else
				{
					// dummy texture coordinates for lightmap
					m_arVertices[nBase + 5] = -1.0;
					m_arVertices[nBase + 6] = -1.0;
                }

				// vertice colors
				m_arVertices[nBase + 7] = m_lVerticeColors[i].x;
				m_arVertices[nBase + 8] = m_lVerticeColors[i].y;
				m_arVertices[nBase + 9] = m_lVerticeColors[i].z;
				m_arVertices[nBase + 10] = 1.0;
			}
            
            m_arIndices = new uint[m_lCoordinateIndexes.Count * 3];
			for(int i = 0; i < m_lCoordinateIndexes.Count; i++)
			{
				m_arIndices[i * 3] = (uint)m_lCoordinateIndexes[i][0];
				m_arIndices[i * 3 + 1] = (uint)m_lCoordinateIndexes[i][1];
				m_arIndices[i * 3 + 2] = (uint)m_lCoordinateIndexes[i][2];
			}

            // create buffers and shader program
            ShaderProgram = ShaderHelper.CreateProgram("shader.vert", "shader.frag"); 

            VertexBufferObject = GL.GenBuffer();
            VertexArrayObject = GL.GenVertexArray();
            ElementBufferObject = GL.GenBuffer();

			ShaderHelper.printOpenGLError();

			// setup vertex array object
			GL.BindVertexArray(VertexArrayObject);

            // 2. copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, m_arVertices.Length * sizeof(double), m_arVertices, BufferUsageHint.StaticDraw);

			// 3. then set our vertex attributes pointers
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, nNumValues * sizeof(double), 0);
            GL.EnableVertexAttribArray(0);

			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Double, false, nNumValues * sizeof(double), 3 * sizeof(double));
			GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Double, false, nNumValues * sizeof(double), 5 * sizeof(double));
            GL.EnableVertexAttribArray(2);

            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Double, false, nNumValues * sizeof(double), 7 * sizeof(double));
            GL.EnableVertexAttribArray(3);

            ShaderHelper.printOpenGLError();

			// setup element buffer for vertex indices
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, m_arIndices.Length * sizeof(uint), m_arIndices, BufferUsageHint.StaticDraw);
			// ===

			ShaderHelper.printOpenGLError();
		}

        public int GetIndex(Face f)
		{
			return m_lFaces.IndexOf(f);
		}

		public void ReadMain(List<Texture> lTextures, StreamReader sr, List<Face> lFaceReferences, ref int nCounter)
		{
			m_lTextures = new List<Texture>(lTextures);
			m_TextureType = lTextures.Count == 2 ? Shape.ETextureType.MULTI : Shape.ETextureType.SINGLE;

			if(m_lTextures.Count == 2)
			{
				// put main texture first and lightmap second to match what it is in q3 shaders
				Texture tLM = m_lTextures[0];
				m_lTextures.RemoveAt(0);
				m_lTextures.Add(tLM);
			}

			if (Read(sr, ref nCounter))
			{
				CreateFaces(lFaceReferences);	
			}
			else 
				throw new Exception("Error in reading shape data from file");
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
					faceVerts.Add(m_lVertices[m_lCoordinateIndexes[i][j]]);
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

		public bool IsSky()
		{
            bool bSky = false;

            Texture tex = GetMainTexture();
            if (tex != null)
            {
				bSky = tex.GetPath().Contains("skies");
            }

            return bSky;
        }

		public bool DontRender()
		{
			bool bRender = true;
			if (GetMainTexture() != null) 
			{
				string sName = Path.GetFileName(GetMainTexture().GetPath());
				if(sName.Contains("beam") || sName.Contains("fog") || sName.Contains("clip"))
					bRender = false;
            }
			return !bRender;
        }

		public bool NoClipping()
		{
			bool bNoClipping = false;

			Texture tex = GetMainTexture();
			if (tex != null)
			{
				string sName = Path.GetFileName(tex.GetPath());
				bNoClipping = sName.Contains("fog") ||
					sName.Contains("beam") || sName.Contains("lava") || tex.GetPath().Contains("skies");
			}

			return bNoClipping;
		}

		public Texture GetMainTexture()
		{
			if (m_lTextures.Count > 0) return m_lTextures[0];
			else return null;
        }

        public int GetMainTextureIndex()
        {
			if (m_lTextures.Count > 0) return 0;
			else return -1;
        }

        public Texture GetLightmapTexture()
        {
            if (m_lTextures.Count > 1) return m_lTextures[1];
            else return null;
        }

        public int GetLightmapTextureIndex()
        {
            if (m_lTextures.Count > 1) return 1;
            else return -1;
        }		

        public List<Texture> GetTextures() { return m_lTextures; }

		/// <summary>
		/// Shows this shape. Loop over texture objects and set same number
		/// of texture units.
		/// </summary>
		public void Show()
        {
			if (DontRender()) return;	
			
			if(GetMainTexture().GetPath().Contains("models"))
			{
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }

			if(m_q3Shader.GetCull() == "disable" || m_q3Shader.GetCull() == "none")
			{
				GL.PushAttrib(AttribMask.EnableBit);
				GL.Disable(EnableCap.CullFace);
			}
			else if(m_q3Shader.GetCull() == "back")
			{
                GL.PushAttrib(AttribMask.EnableBit);
                GL.CullFace(CullFaceMode.Back);
            }

			ShaderHelper.UseProgram(ShaderProgram);
        
			GL.BindVertexArray(VertexArrayObject);

			GL.ActiveTexture(TextureUnit.Texture0);
			GetMainTexture().bindMeRaw();
			GL.ActiveTexture(TextureUnit.Texture1);
			if (m_lTextures.Count >= 2)
			{
				GetLightmapTexture().bindMeRaw();				
			}
			else
			{
				GetMainTexture().bindMeRaw(); // placeholder
			}
			GL.ActiveTexture(TextureUnit.Texture2);
			if (m_q3Shader.GetShaderName().Contains("killblock_i4b"))
			{
				m_lTextures[2].bindMeRaw();
			}
			else
			{
				GetMainTexture().bindMeRaw(); // placeholder, not sure this is needed
			}

			int nLoc = GL.GetUniformLocation(ShaderProgram, "texture1");
			GL.Uniform1(nLoc, 0);
            nLoc = GL.GetUniformLocation(ShaderProgram, "texture2");
            GL.Uniform1(nLoc, 1);
            nLoc = GL.GetUniformLocation(ShaderProgram, "texture3");
            GL.Uniform1(nLoc, 2);

            nLoc = GL.GetUniformLocation(ShaderProgram, "thirdtex");
			bool bKillBlock = m_q3Shader.GetShaderName().Contains("killblock_i4b");
			GL.Uniform1(nLoc, bKillBlock ? 2 : 1);
			ShaderHelper.printOpenGLError();

			nLoc = GL.GetUniformLocation(ShaderProgram, "rgbgen");
			ShaderHelper.printOpenGLError();
			D3Vect dRGBGen = new D3Vect(1.0, 1.0, 1.0);
			if(bKillBlock)
			{
				dRGBGen = m_q3Shader.GetStages()[2].GetRGBGenValue();
			}
			GL.Uniform3(nLoc, Convert.ToSingle(dRGBGen.x), Convert.ToSingle(dRGBGen.y), Convert.ToSingle(dRGBGen.z));
			ShaderHelper.printOpenGLError();

            float[] proj = new float[16];
			float[] modelview = new float[16]; 
			GL.GetFloat(GetPName.ProjectionMatrix, proj);
			GL.GetFloat(GetPName.ModelviewMatrix, modelview);

			nLoc = GL.GetUniformLocation(ShaderProgram, "modelview");
			GL.UniformMatrix4(nLoc, 1, false, modelview);
            nLoc = GL.GetUniformLocation(ShaderProgram, "proj");
            GL.UniformMatrix4(nLoc, 1, false, proj);

			GL.DrawElements(PrimitiveType.Triangles, m_arIndices.Length, DrawElementsType.UnsignedInt, 0);

			GL.UseProgram(0);

            if (GetMainTexture().GetPath().Contains("models"))
            {
                GL.Disable(EnableCap.Blend);
            }
            if (m_q3Shader.GetCull() == "disable" || m_q3Shader.GetCull() == "none" || m_q3Shader.GetCull() == "back")
            {
				GL.PopAttrib();
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
			if(!ReadVertices(sr, ref nCounter)) return false;

			m_lTexCoordinates.Add(new List<DPoint>());
			if (!ReadTextureCoordinates(sr, m_lTexCoordinates[0], ref nCounter)) return false;
			if (m_TextureType == ETextureType.MULTI)
			{
				m_lTexCoordinates.Add(new List<DPoint>());
				if (!ReadTextureCoordinates(sr, m_lTexCoordinates[1], ref nCounter)) return false;

				List<DPoint> lLM = m_lTexCoordinates[0];
				m_lTexCoordinates.RemoveAt(0);
				m_lTexCoordinates.Add(lLM);
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
		private bool ReadVertices(StreamReader sr, ref int nCounter)
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
						vert[0] *= -1; // flip x
						double tempY = vert[1]; 
						vert[1] = vert[2]; // set y to z
						vert[2] = tempY; // set z to y
						m_lVertices.Add(vert);
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
					m_lVertices.Add(vert);
				}

				return true;
			}
		}
    }
}
