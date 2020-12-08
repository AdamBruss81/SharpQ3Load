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
using obsvr;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace engine
{
	/// <summary>
	/// Summary description for Shape.
	/// </summary>
	public class Shape : Subject
	{
		public enum ESignals { FACE_CREATED = SignalStarts.g_nShapeStart };
		public enum ETextureType { NONE, SINGLE, MULTI };

		MapInfo m_map = null;
		List<Texture> m_lTextures;
		List<Face> m_lFaces = new List<Face>();
		List<List<int>> m_lCoordinateIndexes = new List<List<int>>(); // the inner list always has 3 elements
		List<D3Vect> m_lVertices = new List<D3Vect>();
		List<List<DPoint>> m_lTexCoordinates = new List<List<DPoint>>();
		List<D3Vect> m_lVerticeColors = new List<D3Vect>();
		Q3Shader m_q3Shader = null;
		List<D3Vect> m_lVertexNormals = new List<D3Vect>();
		List<List<List<int>>> m_lSubShapes = new List<List<List<int>>>(); // for dividing up shapes into sub shapes to fix transparency issues on rendering
																		  // could also be used later to control properties of jumppads
		D3Vect m_d3AutoSprite2UpVector = new D3Vect();

        float[] m_util4x4 = new float[16];

        D3Vect m_d3MidPoint = null;
		bool m_bSubShape = false;
		bool m_bMergeSource = false;
		bool m_bRender = true;

		string m_autoGenereatedFragShader = "";
		string m_autoGenereatedVertexShader = "";

		// shader utility members for performance
		float[] m_uniformFloat6 = { 0f, 0f, 0f, 0f, 0f, 0f };
		float[] m_uniformFloat3 = { 0f, 0f, 0f };
		float[] m_uniformFloat2 = { 0f, 0f };

		// modern open gl constructs
		double[] m_arVertices = null;
		uint[] m_arIndices = null;
		int ShaderProgram;
		int VertexBufferObject;
		int VertexArrayObject;
		int ElementBufferObject;
		int ShaderStorageBufferObject;
		// ===

		// consts
		const string m_sVerticeColorHeader = "color Color { color [";
		const string m_sTextureCoordinatesHeader = "TextureCoordinate { point [";
		const string m_sChannelOneTextureCoordinatesHeader = "texCoord  TextureCoordinate { point [";
		const string m_sMeshCoordinatesHeader = "coord Coordinate { point [";
		const string m_sCoordinateIndexHeader = "coordIndex [";
		const int m_nNumValuesInVA = 14;

		ETextureType m_TextureType;

		public Shape(MapInfo map) { m_map = map; m_q3Shader = new Q3Shader(this); }

		public List<Face> GetMapFaces() { return m_lFaces; }

		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="s">source to copy from to this</param>
		public Shape(Shape s)
		{
			m_lTextures = new List<Texture>();
			m_lTextures.AddRange(s.m_lTextures);
			m_lVertices.AddRange(s.m_lVertices);
			m_lTexCoordinates.AddRange(s.m_lTexCoordinates);
			m_lVerticeColors.AddRange(s.m_lVerticeColors);
			m_TextureType = s.m_TextureType;
			m_q3Shader = new Q3Shader(this);
			m_map = s.m_map;
		}

		public List<D3Vect> GetVertices() { return m_lVertices; }

		public MapInfo GetMap() { return m_map; }

		public void Merge(Shape s)
		{
			s.m_bMergeSource = true;

			int nOriginalVertCount = m_lVertices.Count;
			m_lVertices.AddRange(s.m_lVertices);

			System.Diagnostics.Debug.Assert(s.m_lTexCoordinates.Count == m_lTexCoordinates.Count);
			for (int i = 0; i < s.m_lTexCoordinates.Count; i++)
			{
				m_lTexCoordinates[i].AddRange(s.m_lTexCoordinates[i]);
			}

			m_lVerticeColors.AddRange(s.m_lVerticeColors);
			m_lFaces.AddRange(s.m_lFaces);

			int nFaceCount = m_lCoordinateIndexes.Count;
			for (int i = 0; i < s.m_lCoordinateIndexes.Count; i++)
			{
				List<int> lCoordIndicesNew = s.m_lCoordinateIndexes[i];
				lCoordIndicesNew[0] += nOriginalVertCount;
				lCoordIndicesNew[1] += nOriginalVertCount;
				lCoordIndicesNew[2] += nOriginalVertCount;
				m_lCoordinateIndexes.Add(lCoordIndicesNew);

				m_lFaces[nFaceCount + i].SetIndices(lCoordIndicesNew);
			}

			s.m_lCoordinateIndexes.Clear(); // s stays the same except that it now has no faces to render. it still has
											// its faces for collision detection
		}

		public bool IsMergeSource() { return m_bMergeSource; }

		public void Delete()
		{
			ShaderHelper.CloseProgram(ShaderProgram);

			m_q3Shader.Delete();

			foreach (Face f in m_lFaces) {
				f.Delete();
			}
		}

		public void SetSubShape(bool b) { m_bSubShape = b; }

		public List<List<List<int>>> GetSubShapes() { return m_lSubShapes; }

		public Q3Shader GetQ3Shader()
		{
			return m_q3Shader;
		}

		public void SetCoordIndices(List<List<int>> lSubShapeIndices)
		{
			m_lCoordinateIndexes = lSubShapeIndices;
		}

		private string CreateGLSLVertShader()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			sb.AppendLine("#version 430");
			sb.AppendLine("");
			sb.AppendLine("layout(location = 0) in vec3 aPosition;");
			sb.AppendLine("layout(location = 1) in vec2 aTexCoord;");
			sb.AppendLine("layout(location = 2) in vec2 aTexCoord2;");
			sb.AppendLine("layout(location = 3) in vec4 aColor;");
			sb.AppendLine("layout(location = 4) in vec3 vertexNormal;");
			sb.AppendLine("layout(std430, binding = 3) buffer layoutName");
			sb.AppendLine("{");
			sb.AppendLine("float sinValues[];");
			sb.AppendLine("};");
			sb.AppendLine("");
			sb.AppendLine("out vec2 mainTexCoord;");
			sb.AppendLine("out vec2 lightmapTexCoord;");
			sb.AppendLine("out vec4 color;");
			sb.AppendLine("out vec3 vertice;");
			sb.AppendLine("");

			for (int i = 0; i < m_q3Shader.GetStages().Count; i++)
			{
				if (m_q3Shader.GetStages()[i].GetTCMODS().Count > 0)
				{
					sb.AppendLine("out vec2 texmod" + i + ";");
				}
			}

			sb.AppendLine("");

			bool bUsesTCGen = m_q3Shader.UsesTcgen();
			bool bUsesAlphaGenspec = m_q3Shader.UsesAlphaGenspec();

			if (bUsesTCGen)
			{
				sb.AppendLine("out vec2 tcgenEnvTexCoord;");
			}
			if (bUsesAlphaGenspec)
			{
				sb.AppendLine("out float alphaGenSpecular;");
			}

			// uniforms
			sb.AppendLine("uniform mat4 modelview;");
			sb.AppendLine("uniform mat4 proj;");
			sb.AppendLine("uniform vec3 camPosition;");
			sb.AppendLine("uniform float timeS;");
			sb.AppendLine("uniform mat4 autospriteMat;");

			bool bSendSinTable = false;

			for (int i = 0; i < m_q3Shader.GetStages().Count; i++)
			{
				Q3ShaderStage stage = m_q3Shader.GetStages()[i];
				string sIndex = i.ToString();

				if (stage.GetTCMODS().Count > 0) // there are tcmods
				{
					for (int j = 0; j < stage.GetTCMODS().Count; j++) // this order doesn't matter for the uniform declarations
					{
						switch (stage.GetTCMODS()[j].GetModType())
						{
							case TCMOD.ETYPE.SCALE: sb.AppendLine("uniform vec2 scale" + sIndex + ";"); break;
							case TCMOD.ETYPE.SCROLL: sb.AppendLine("uniform vec2 scroll" + sIndex + ";"); break;
							case TCMOD.ETYPE.TURB:
								{
									sb.AppendLine("uniform vec3 turb" + sIndex + ";");
									bSendSinTable = true;
									break;
								}
							case TCMOD.ETYPE.STRETCH: sb.AppendLine("uniform float stretch" + sIndex + "[6];"); break;
							case TCMOD.ETYPE.ROTATE: sb.AppendLine("uniform float rotate" + sIndex + "[6];"); break;
							case TCMOD.ETYPE.TRANSFORM: sb.AppendLine("uniform float transform" + sIndex + "[6];"); break;
						}
					}
				}
			}

			bool bDeformVWavePresent = false;
			bool bDeformBulgePresent = false;
			bool bDeformMovePresent = false;
			bool bDeformAutoSprite = false;

			for (int i = 0; i < m_q3Shader.GetDeformVertexes().Count; i++)
			{
				DeformVertexes dv = m_q3Shader.GetDeformVertexes()[i];
				if (dv.m_eType == DeformVertexes.EDeformVType.WAVE)
				{
					bDeformVWavePresent = true;
				}
				else if (dv.m_eType == DeformVertexes.EDeformVType.BULGE)
				{
					bDeformBulgePresent = true;
				}
				else if (dv.m_eType == DeformVertexes.EDeformVType.MOVE)
				{
					bDeformMovePresent = true;
				}
				else if (dv.m_eType == DeformVertexes.EDeformVType.AUTOSPRITE || dv.m_eType == DeformVertexes.EDeformVType.AUTOSPRITE2)
				{
					bDeformAutoSprite = true;

					if(dv.m_eType == DeformVertexes.EDeformVType.AUTOSPRITE2)
                    {
						// setup fixed up vector
						DefineFixedAS2UpVector();
                    }
				}
			}

			if (bSendSinTable)
			{
				//sb.AppendLine("uniform float sinValues[1024];");
			}

			if (bUsesTCGen)
			{
				sb.AppendLine("");
				sb.AppendLine("void CalculateTcGen(in vec3 campos, in vec3 position, out vec2 tcgen)");
				sb.AppendLine("{");
				sb.AppendLine("vec3 viewer = campos - position;");
				sb.AppendLine("viewer = normalize(viewer);");
				sb.AppendLine("float d = dot(vertexNormal, viewer);");
				sb.AppendLine("vec3 reflected = vertexNormal * 2.0 * d - viewer;");
				sb.AppendLine("tcgen[0] = 0.5 + reflected[0] * 0.5;");
				sb.AppendLine("tcgen[1] = 0.5 - reflected[1] * 0.5;");
				sb.AppendLine("}");
				sb.AppendLine("");
			}

			if (bUsesAlphaGenspec)
			{
				sb.AppendLine("");
				sb.AppendLine("void CalculateAlphaGenSpec(in vec3 campos, in vec3 position, out float alpha)");
				sb.AppendLine("{");
				sb.AppendLine("vec3 lightorigin = vec3(-960, 1980, 96);");
				sb.AppendLine("vec3 lightdir = lightorigin - position;");
				sb.AppendLine("lightdir = normalize(lightdir);");
				sb.AppendLine("float d = dot(vertexNormal, lightdir);");
				sb.AppendLine("vec3 reflected = vertexNormal * 2 * d - lightdir;");
				sb.AppendLine("vec3 viewer = campos - position;");
				sb.AppendLine("float ilen = sqrt(dot(viewer, viewer));");
				sb.AppendLine("float l = dot(reflected, viewer);");
				sb.AppendLine("l *= ilen;");
				sb.AppendLine("if (l < 0) {");
				sb.AppendLine("alpha = 0;");
				sb.AppendLine("}");
				sb.AppendLine("else {");
				sb.AppendLine("l = l*l;");
				sb.AppendLine("l = l*l;");
				sb.AppendLine("alpha = l * 255;");
				sb.AppendLine("if (alpha > 255) {");
				sb.AppendLine("alpha = 255;");
				sb.AppendLine("}");
				sb.AppendLine("}");
				sb.AppendLine("alpha = alpha / 255.0;");
				sb.AppendLine("}");
				sb.AppendLine("");
			}

			if (bDeformVWavePresent)
			{
				// insert function to deform a vertex     

				// wavefunc: sin, triangle, square, sawtooth or inversesawtooth : 0,1,2,3,4
				sb.AppendLine("");
				sb.AppendLine("void DeformVertexWave(in float div, in float wavefunc, in float base, in float amp, in float phase, in float freq, inout vec3 vertex)");
				sb.AppendLine("{");
				sb.AppendLine("if(wavefunc == 0) {"); // just sin for now
				sb.AppendLine("float divMult = -.261 * freq + .1861;");
				// my vertices are a lot smaller than the bsp ones so i need to scale the div, amp and base				
				sb.AppendLine("float off = ( vertex[0] + vertex[1] + vertex[2] ) * (div * divMult);");
				sb.AppendLine("float fCycleTimeMS = 1.0 / (freq * 2.0);");
				sb.AppendLine("float fIntoSin = (timeS + (phase + off) * freq) / fCycleTimeMS * 3.1415926;");
				sb.AppendLine("float fSinValue = sin(fIntoSin);");
				sb.AppendLine("float fScale = 0.015;");
				sb.AppendLine("float fVal = fSinValue * (amp*fScale) + (base*fScale);");
				sb.AppendLine("vec3 offset;");
				sb.AppendLine("offset[0] = vertexNormal[0]*fVal;");
				sb.AppendLine("offset[1] = vertexNormal[1]*fVal;");
				sb.AppendLine("offset[2] = vertexNormal[2]*fVal;");
				sb.AppendLine("vertex += offset;");
				sb.AppendLine("}");
				sb.AppendLine("}");
				sb.AppendLine("");
			}

			if (bDeformBulgePresent)
			{
				sb.AppendLine("void BulgeVertexes(in float width, in float height, in float speed, inout vec3 vertex)");
				sb.AppendLine("{");
				sb.AppendLine("float now = timeS * speed * 0.15;");
				sb.AppendLine("float off = 3.1415926 * 2 * (aTexCoord[0] * width + now);");
				sb.AppendLine("float scale = sin(off) * (height*.02);");
				sb.AppendLine("vertex += vertexNormal * scale;");
				sb.AppendLine("}");
			}

			if (bDeformAutoSprite)
			{
				// create auto sprite function
				// this function will modify the vertex. it will use the vector from eye to shape midpoint. and the normal of one of the faces of the shape.
				// it could use a vertexnormal too.
				// it's going to figure out how much to rotate the vertex so that the normals are pointing at eachother.

				sb.AppendLine("");
                sb.AppendLine("void DeformAutoSprite(inout vec3 vertex)");
                sb.AppendLine("{");
				sb.AppendLine("vec4 vtemp;");
				sb.AppendLine("vtemp.x = vertex.x;");
				sb.AppendLine("vtemp.y = vertex.y;");
				sb.AppendLine("vtemp.z = vertex.z;");
				sb.AppendLine("vtemp.w = 1.0;");
				sb.AppendLine("vtemp = vtemp * autospriteMat;");
				sb.AppendLine("vertex.x = vtemp.x;");
				sb.AppendLine("vertex.y = vtemp.y;");
				sb.AppendLine("vertex.z = vtemp.z;");
				sb.AppendLine("}");
            }

			if (bDeformMovePresent)
			{
				// insert function to move a vertex     

				// wavefunc: sin, triangle, square, sawtooth or inversesawtooth : 0,1,2,3,4

				sb.AppendLine("void MoveVertexes(in float x, in float y, in float z, in float wavefunc, in float base, in float amp, in float phase, in float freq, inout vec3 vertex)");
				sb.AppendLine("{");
				sb.AppendLine("if(wavefunc == 0) {"); // just sin for now
				sb.AppendLine("float fCycleTimeMS = 1.0f / (freq * 2.0);");
				sb.AppendLine("float fIntoSin = (timeS + phase * freq) / fCycleTimeMS * 3.1415926;");
				sb.AppendLine("float fSinValue = sin(fIntoSin);");
				sb.AppendLine("float scale = fSinValue * (amp*0.0166) + (base*0.0166);");
				sb.AppendLine("vec3 moveVec = vec3(x, y, z);");
				sb.AppendLine("moveVec *= scale;");
				sb.AppendLine("vertex += moveVec;");
				sb.AppendLine("}");
				sb.AppendLine("}");
			}

			sb.AppendLine("");
			sb.AppendLine("void main(void)");
			sb.AppendLine("{");

			sb.AppendLine("mainTexCoord = aTexCoord;");
			sb.AppendLine("lightmapTexCoord = aTexCoord2;");

			if (bUsesTCGen)
			{
				sb.AppendLine("CalculateTcGen(camPosition, aPosition, tcgenEnvTexCoord);");
			}
			if (bUsesAlphaGenspec)
			{
				sb.AppendLine("CalculateAlphaGenSpec(camPosition, aPosition, alphaGenSpecular);");
			}

			sb.AppendLine("");

			// define tcmods
			for (int i = 0; i < m_q3Shader.GetStages().Count; i++)
			{
				Q3ShaderStage stage = m_q3Shader.GetStages()[i];
				string sIndex = Convert.ToString(i);
				string sTexmod = "texmod" + sIndex;

				// init texmod
				if (stage.GetTCMODS().Count > 0)
				{
					if (stage.GetTCGEN_CS() == "environment")
						sb.AppendLine(sTexmod + " = tcgenEnvTexCoord;");
					else
						sb.AppendLine(sTexmod + " = mainTexCoord;");
				}

				for (int j = 0; j < stage.GetTCMODS().Count; j++)
				{
					switch (stage.GetTCMODS()[j].GetModType())
					{
						case TCMOD.ETYPE.SCROLL:
							{
								if (m_q3Shader.GetShaderName().Contains("skies"))
								{
									sb.AppendLine(sTexmod + ".x -= scroll" + sIndex + "[0] * timeS * 10;");
									sb.AppendLine(sTexmod + ".y -= scroll" + sIndex + "[1] * timeS * 10;");
								}
								else
								{
									sb.AppendLine(sTexmod + ".x -= scroll" + sIndex + "[0] * timeS;");
									sb.AppendLine(sTexmod + ".y -= scroll" + sIndex + "[1] * timeS;");
								}
								break;
							}
						case TCMOD.ETYPE.SCALE:
							{
								if (m_q3Shader.GetShaderName().Contains("skies"))
								{
									sb.AppendLine(sTexmod + ".x /= scale" + sIndex + "[0];");
									sb.AppendLine(sTexmod + ".y /= scale" + sIndex + "[1];");
								}
								else
								{
									sb.AppendLine(sTexmod + ".x *= scale" + sIndex + "[0];");
									sb.AppendLine(sTexmod + ".y *= scale" + sIndex + "[1];");
								}
								break;
							}
						case TCMOD.ETYPE.STRETCH:
							{
								// 0 - 3 are the 2x2 transform matrix
								// 4-5 are the translate vector
								DefineTransformGLSL(sb, sTexmod, "stretch", sIndex);
								break;
							}
						case TCMOD.ETYPE.ROTATE:
							{
								// 0 - 3 are the 2x2 transform matrix
								// 4-5 are the translate vector
								DefineTransformGLSL(sb, sTexmod, "rotate", sIndex);
								break;
							}
						case TCMOD.ETYPE.TURB:
							{
								// get rid of sintable usage for now until i get to fixing this again
								sb.AppendLine("float turbVal" + sIndex + " = turb" + sIndex + "[1] + timeS * turb" + sIndex + "[2];");

								// The multiply by fScaler below is needed to scale the vertice positions to account for the fact that my vrml values are scaled down from q3 values
								// if you don't scale like this then the turbulence won't look right because the phases of the s and t will be too close

								float fScaler = 60.0f;

                                sb.AppendLine(sTexmod + ".x = " + sTexmod + ".x + sinValues[ int ( ( ( aPosition.x + aPosition.z ) * " + fScaler + " * 0.125 * 1.0/128 + turbVal" + sIndex + " ) * 1024 ) & 1023 ] * turb" + sIndex + "[0];");
                                sb.AppendLine(sTexmod + ".y = " + sTexmod + ".y + sinValues[ int ( ( ( aPosition.y ) * " + fScaler + " * 0.125 * 1.0/128 + turbVal" + sIndex + " ) * 1024 ) & 1023 ] * turb" + sIndex + "[0];");

                                break;
							}
						case TCMOD.ETYPE.TRANSFORM:
                            {
								// 0 - 3 are the 2x2 transform matrix
								// 4-5 are the translate vector
								DefineTransformGLSL(sb, sTexmod, "transform", sIndex);
								break;
                            }
                    }
				}
			}

			sb.AppendLine("");

			sb.AppendLine("color = aColor;");

			if (bDeformVWavePresent || bDeformBulgePresent || bDeformMovePresent || bDeformAutoSprite)
			{
				sb.AppendLine("vec3 newPosition = aPosition;");
				sb.AppendLine("");
				for (int i = 0; i < m_q3Shader.GetDeformVertexes().Count; i++)
				{
					DeformVertexes dv = m_q3Shader.GetDeformVertexes()[i];

					if (dv.m_eType == DeformVertexes.EDeformVType.WAVE)
					{
						float fWF = dv.m_wf.func == "sin" ? 0f : -1f;
						if (fWF != 0f)
						{
							throw new Exception("Encountered non sin deformvertexes wave");
						}
						sb.AppendLine("DeformVertexWave(" + dv.m_div + ", " + fWF + ", " + dv.m_wf.fbase + ", " + dv.m_wf.amp + ", " + dv.m_wf.phase + ", " + dv.m_wf.freq + ", newPosition);");
					}
					else if (dv.m_eType == DeformVertexes.EDeformVType.BULGE)
					{
						sb.AppendLine("BulgeVertexes(" + dv.m_Bulge.m_bulgeWidth + ", " + dv.m_Bulge.m_bulgeHeight + ", " + dv.m_Bulge.m_bulgeSpeed + ", newPosition);");
					}
					else if (dv.m_eType == DeformVertexes.EDeformVType.MOVE)
					{
						float fWF = dv.m_wf.func == "sin" ? 0f : -1f;
						if (fWF != 0f)
						{
							throw new Exception("Encountered non sin deformmove wave");
						}
						sb.AppendLine("MoveVertexes(" + dv.m_Move.m_x + ", " + dv.m_Move.m_y + ", " + dv.m_Move.m_z + ", " + fWF + ", " + dv.m_wf.fbase + ", " + dv.m_wf.amp + ", " + dv.m_wf.phase + ", " + dv.m_wf.freq + ", newPosition);");
					}
					else if(dv.m_eType == DeformVertexes.EDeformVType.AUTOSPRITE || dv.m_eType == DeformVertexes.EDeformVType.AUTOSPRITE2)
					{
						sb.AppendLine("DeformAutoSprite(newPosition);");
					}
				}
				sb.AppendLine("");
			}

			string sFinalPosName = (bDeformVWavePresent || bDeformBulgePresent || bDeformMovePresent || bDeformAutoSprite) ? "newPosition" : "aPosition";
			sb.AppendLine("vertice = " + sFinalPosName + ";");
			sb.AppendLine("gl_Position = proj * modelview * vec4(" + sFinalPosName + ", 1.0);");

			sb.AppendLine("}");

			return sb.ToString();
		}

		private void DefineTransformGLSL(System.Text.StringBuilder sb, string sTexmod, string sTransformType, string sIndex)
        {
            // 0 - 3 are the 2x2 transform matrix
            // 4-5 are the translate vector
            sb.AppendLine("float " + sTexmod + "_" + sTransformType + "_x = " + sTexmod + ".x;");
            sb.AppendLine("float " + sTexmod + "_" + sTransformType + "_y = " + sTexmod + ".y;");
            sb.AppendLine(sTexmod + ".x = " + sTexmod + "_" + sTransformType + "_x * " + sTransformType + sIndex + "[0] + " + sTexmod + "_" + sTransformType + "_y * " + sTransformType + sIndex + "[1] + " + sTransformType + sIndex + "[4];");
			sb.AppendLine(sTexmod + ".y = " + sTexmod + "_" + sTransformType + "_x * " + sTransformType + sIndex + "[2] + " + sTexmod + "_" + sTransformType + "_y * " + sTransformType + sIndex + "[3] + " + sTransformType + sIndex + "[5];");
        }

		/// <summary>
		/// Find long axis of this shape and use it as up vector for autosprite2
		/// </summary>
        private void DefineFixedAS2UpVector()
        {
			List<Edge> lUniqueEdges = new List<Edge>();
            for(int i = 0; i < m_lFaces.Count; i++)
            {
				m_lFaces[i].GatherUniqueEdges(lUniqueEdges, false);
            }
			for(int i = 0; i < lUniqueEdges.Count; i++)
            {
				if(lUniqueEdges[i].GetLength() > m_d3AutoSprite2UpVector.Length)
                {
					m_d3AutoSprite2UpVector = (lUniqueEdges[i].Vertice1 - lUniqueEdges[i].Vertice2);
                }
            }
			m_d3AutoSprite2UpVector.normalize();
        }

		public void SetDontRender(bool b)
        {
			m_bRender = !b;
        }

        public string CreateGLSLFragShader()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			// things in all fragment shaders
			sb.AppendLine("#version 430");
			sb.AppendLine("");
			sb.AppendLine("out vec4 outputColor;");
			sb.AppendLine("");
			sb.AppendLine("in vec2 mainTexCoord;");
			sb.AppendLine("in vec2 lightmapTexCoord;");
			sb.AppendLine("in vec4 color;");
			sb.AppendLine("in vec3 vertice;");

			if (m_q3Shader.UsesTcgen())
				sb.AppendLine("in vec2 tcgenEnvTexCoord;");
			if (m_q3Shader.UsesAlphaGenspec())
				sb.AppendLine("in float alphaGenSpecular;");
			sb.AppendLine("");

			for (int i = 0; i < m_q3Shader.GetStages().Count; i++)
			{
				if (m_q3Shader.GetStages()[i].GetTCMODS().Count > 0)
				{
					sb.AppendLine("in vec2 texmod" + i + ";");
				}
			}

			if (!string.IsNullOrEmpty(m_q3Shader.GetShaderName()))
			{
				m_q3Shader.ConvertQ3ShaderToFragGLSL(sb);
			}
			else
			{
				// default shader for lightmapped or vert colored faces

				// texture uniform samplers
				sb.AppendLine("uniform " + Q3Shader.GetSampler2DName() + " texture0;"); // main texture
				if (m_lTextures.Count > 1) sb.AppendLine("uniform " + Q3Shader.GetSampler2DName() + " texture1;");
				sb.AppendLine("");

				sb.AppendLine("void main()");
				sb.AppendLine("{");

				if (GetLightmapTexture() != null)
                {
                    
                    sb.AppendLine("vec4 main_tex_texel = texture(texture0, mainTexCoord);");
                    sb.AppendLine("vec4 lightmap_texel = texture(texture1, lightmapTexCoord);");
                    sb.AppendLine("outputColor = clamp(main_tex_texel * lightmap_texel * " + GameGlobals.GetBaseLightmapScale() + ", 0.0, 1.0);");
                }
				else
                {
                    sb.AppendLine("vec4 main_tex_texel = texture(texture0, mainTexCoord);");
                    sb.AppendLine("outputColor = main_tex_texel * color * 3.0;");
                }

				sb.AppendLine("}");
			}

			return sb.ToString();
		}

		private void InitTexture(Texture t, bool bLightmap)
		{
			bool bShouldBeTGA = false;
			string sFullTexPath = m_q3Shader.GetPathToTextureNoShaderLookup(bLightmap, t.GetPath(), ref bShouldBeTGA);
			bool bFoundTexture = false;
			if (!File.Exists(sFullTexPath))
			{
				//LOGGER.Info("Could not find texture at location " + sFullTexPath + ". This is probably a problem with loading a custom map.");
				//SetDontRender(true);

				string sPK3 = Path.ChangeExtension(GetMap().GetMapPathOnDisk, "pk3");
				if (File.Exists(sPK3))
				{
					sFullTexPath = m_q3Shader.GetPathToTextureNoShaderLookup(false, t.GetPath(), ref bShouldBeTGA, sPK3);
					if (File.Exists(sFullTexPath))
					{
						bFoundTexture = true;
						t.SetShouldBeTGA(bShouldBeTGA); // lm so never should be tga
						t.SetFullPath(sFullTexPath);
					}
					else
					{
						LOGGER.Info("Could not find texture at location " + sFullTexPath + ". This is probably a problem with loading a custom map.");
						SetDontRender(true);
					}
				}
			}
			else
			{
				bFoundTexture = true;
                t.SetShouldBeTGA(bShouldBeTGA); // lm so never should be tga
                t.SetFullPath(sFullTexPath);
            }

			if(bFoundTexture)
            {
				t.SetTexture(m_q3Shader.GetShaderName());
			}
		}

		public void InitializeNonGL()
        {
            m_q3Shader.ReadQ3Shader(GetMainTexture().GetPath());

			GameGlobals.m_SharedTextureInit.WaitOne();

			if (string.IsNullOrEmpty(m_q3Shader.GetShaderName()))
            {
                foreach (Texture t in m_lTextures)
                {				
                    if (!t.Initialized())
                    {
                        if (GetMainTexture() == t)
                        {
							InitTexture(t, false);
                        }
                        else
                        {
							// lightmap for non shader shape
							InitTexture(t, true);
						}
					}					
                }
            }
			else
			{
				// shader present but still need to init lightmap if present
				if(GetLightmapTexture() != null && !GetLightmapTexture().Initialized())
				{
					InitTexture(GetLightmapTexture(), true);
				}

				// one key point here is that we don't try to initialize the main texture in this case. if a shader is present there's no need to
				// i should probably not create or delete the main texture in this case but it doesn't do any harm right now
			}

			GameGlobals.m_SharedTextureInit.ReleaseMutex();			

            // use modern open gl via vertex buffers, vertex array, element buffer and shaders
            // setup vertices
            m_arVertices = new double[m_lVertices.Count * m_nNumValuesInVA]; // vertices, texcoord1, texcoord2(could be dummy if no lightmap), color
            for (int i = 0; i < m_lVertices.Count; i++)
            {
                int nBase = i * m_nNumValuesInVA;

                // vertices
                m_arVertices[nBase] = m_lVertices[i].x;
                m_arVertices[nBase + 1] = m_lVertices[i].y;
                m_arVertices[nBase + 2] = m_lVertices[i].z;

                // main texture coordinates
                m_arVertices[nBase + 3] = m_lTexCoordinates[GetMainTextureIndex()][i].Vect[0];
                m_arVertices[nBase + 4] = m_lTexCoordinates[GetMainTextureIndex()][i].Vect[1];

                if (m_lTextures.Count > 1)
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

                // vertice normals
                D3Vect vNormal = new D3Vect();
                int nCounter = 0;
                for (int j = 0; j < m_lCoordinateIndexes.Count; j++)
                {
                    for (int k = 0; k < m_lCoordinateIndexes[j].Count; k++)
                    {
                        if (i == m_lCoordinateIndexes[j][k])
                        {
                            // j corresponds to a face
                            vNormal += m_lFaces[j].GetNormal;
                            nCounter++;
                            break;
                        }
                    }
                }
				if (nCounter > 0)
				{
					vNormal = vNormal / nCounter;
					vNormal.normalize();

					vNormal.Negate();
					m_lVertexNormals.Add(vNormal);
				}

                m_arVertices[nBase + 11] = vNormal.x;
                m_arVertices[nBase + 12] = vNormal.y;
                m_arVertices[nBase + 13] = vNormal.z;
            }

            m_arIndices = new uint[m_lCoordinateIndexes.Count * 3];
            for (int i = 0; i < m_lCoordinateIndexes.Count; i++)
            {
                m_arIndices[i * 3] = (uint)m_lCoordinateIndexes[i][0];
                m_arIndices[i * 3 + 1] = (uint)m_lCoordinateIndexes[i][1];
                m_arIndices[i * 3 + 2] = (uint)m_lCoordinateIndexes[i][2];
            }

            m_autoGenereatedFragShader = CreateGLSLFragShader();
            m_autoGenereatedVertexShader = CreateGLSLVertShader();

#if DEBUG
            if (!string.IsNullOrEmpty(m_autoGenereatedFragShader) && !string.IsNullOrEmpty(m_q3Shader.GetShaderName()))
			{
				GameGlobals.m_DebugShaderWriteMutex.WaitOne();

				// this will not output the default glsl shaders for when no q3 shader exists. but I can turn that on somehow if i need it.				
				File.WriteAllText("c:\\temp\\" + Path.GetFileName(m_q3Shader.GetShaderName()) + ".frag.txt", m_autoGenereatedFragShader);
				File.WriteAllText("c:\\temp\\" + Path.GetFileName(m_q3Shader.GetShaderName()) + ".vert.txt", m_autoGenereatedVertexShader);

				GameGlobals.m_DebugShaderWriteMutex.ReleaseMutex();
			}
#endif
        }

		public bool GetRenderMember() { return m_bRender; }

        public void InitializeGL()
		{
			// define gl aspects of all textures for this shape
			// three locations are : shape texture list, q3shader list and q3shader animmap list
			if (string.IsNullOrEmpty(m_q3Shader.GetShaderName()))
			{
				foreach (Texture t in m_lTextures)
				{
					t.GLDefineTexture();
				}
			}
			m_q3Shader.GLDefineTextures();

            foreach (Face f in m_lFaces)
            {
                f.InitializeLists();
            }

            ShaderProgram = ShaderHelper.CreateProgramFromContent(m_autoGenereatedVertexShader, m_autoGenereatedFragShader, m_q3Shader.GetShaderName());

			VertexBufferObject = GL.GenBuffer();
			VertexArrayObject = GL.GenVertexArray();
			ElementBufferObject = GL.GenBuffer();
			ShaderStorageBufferObject = GL.GenBuffer();

			ShaderHelper.printOpenGLError("");

			// setup vertex array object
			GL.BindVertexArray(VertexArrayObject);

			// 2. copy our vertices array in a buffer for OpenGL to use
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
			GL.BufferData(BufferTarget.ArrayBuffer, m_arVertices.Length * sizeof(double), m_arVertices, BufferUsageHint.StaticDraw);

			// 3. then set our vertex attributes pointers
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, m_nNumValuesInVA * sizeof(double), 0);
			GL.EnableVertexAttribArray(0);

			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Double, false, m_nNumValuesInVA * sizeof(double), 3 * sizeof(double));
			GL.EnableVertexAttribArray(1);

			GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Double, false, m_nNumValuesInVA * sizeof(double), 5 * sizeof(double));
			GL.EnableVertexAttribArray(2);

			GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Double, false, m_nNumValuesInVA * sizeof(double), 7 * sizeof(double));
			GL.EnableVertexAttribArray(3);

			GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Double, false, m_nNumValuesInVA * sizeof(double), 11 * sizeof(double));
			GL.EnableVertexAttribArray(4);

			ShaderHelper.printOpenGLError("");

			// setup element buffer for vertex indices
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
			GL.BufferData(BufferTarget.ElementArrayBuffer, m_arIndices.Length * sizeof(uint), m_arIndices, BufferUsageHint.StaticDraw);

			// setup ssbo for sin values table 
			GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ShaderStorageBufferObject);
			GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * 1024, GameGlobals.m_SinTable, BufferUsageHint.StaticDraw);
			GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, ShaderStorageBufferObject);
			// ===

			ShaderHelper.printOpenGLError("");
		}

		public int GetIndex(Face f)
		{
			return m_lFaces.IndexOf(f);
		}

		public bool ReadMain(List<Texture> lTextures, StreamReader sr, ref int nCounter)
		{
			m_lTextures = new List<Texture>(lTextures);
			m_TextureType = lTextures.Count == 2 ? Shape.ETextureType.MULTI : Shape.ETextureType.SINGLE;

			if (m_lTextures.Count == 2)
			{
				// put main texture first and lightmap second to match what it is in q3 shaders
				Texture tLM = m_lTextures[0];
				m_lTextures.RemoveAt(0);
				m_lTextures.Add(tLM);
			}

			return Read(sr, ref nCounter);			
		}

		public void CreateFaces(List<Face> lFaceReferences)
		{
			List<D3Vect> faceVerts = new List<D3Vect>();
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
					if (m_TextureType == ETextureType.MULTI)
						faceTexCoords[1].Add(m_lTexCoordinates[1][m_lCoordinateIndexes[i][j]]);
					faceVertColors.Add(m_lVerticeColors[m_lCoordinateIndexes[i][j]]);
				}
				pFace = new Face(faceVerts, faceTexCoords, faceVertColors, new Color(240, 0, 0), new Color(100, 0, 0), lFaceReferences.Count, m_lCoordinateIndexes[i]);
				pFace.SetParentShape(this);
				m_lFaces.Add(pFace);
				lFaceReferences.Add(pFace);
				Notify((int)ESignals.FACE_CREATED);
				pFace = null;
				faceVerts.Clear();
				faceTexCoords[0].Clear();
				if (m_TextureType == ETextureType.MULTI)
					faceTexCoords[1].Clear();
				faceVertColors.Clear();
			}

			// calculate mid point based on faces
			m_d3MidPoint = new D3Vect();
			for (int i = 0; i < m_lFaces.Count; i++)
			{
				m_d3MidPoint += m_lFaces[i].GetMidpoint();
			}
			m_d3MidPoint /= m_lFaces.Count;			
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
			bool bRender = m_bRender;

			if (bRender)
			{
				if (GetMainTexture() != null)
				{
					string sName = Path.GetFileName(GetMainTexture().GetPath());
					if (sName.Contains("fog") || sName.Contains("clip"))
						bRender = false;
				}
				if (bRender)
					bRender = m_lCoordinateIndexes.Count > 0;

				if (bRender)
				{
					//bRender = GetMainTexture().GetPath().Contains("slamp3");
				}
			}

			return !bRender;
		}

		public bool NonSolid()
		{
			bool bNonSolid;

			string s = m_q3Shader.GetShaderName();

			bNonSolid = s.Contains("fog") ||
				s.Contains("beam") ||
				s.Contains("lava") || s.Contains("wires") ||
				(s.Contains("skies") && !s.Contains("gothic_wall"));

			// this is too unreliable
			/*bNoClipping = m_q3Shader.GetNonSolid() || m_q3Shader.GetSky() || m_q3Shader.GetLava() || m_q3Shader.GetFog();

			if(m_q3Shader.GetShaderName().Contains("skin") || m_q3Shader.GetShaderName().Contains("bluemetal2_shiny_trans"))
			{
				bNoClipping = false; // special case. hopefully only one? no idea why this is non solid. annoying that i cant easily determine
				// whether a shader should restrict movement or not

				// why the heck is a normal wall like bluemetal2 in dm0 marked nonsolid in shader???

				// i may have to abort on this strategy of using nonsolid
			}*/

			return bNonSolid;
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

		/// <summary>
		/// There are three levels of render order control
		/// 1. this, GetRenderOrder, which only happens once per map.
		/// 2. then there is sorting of shapes by mid point distance. subshapes can be created to sort specific beams against specific consoles for example.
		/// see deva station control room.
		/// 3. then there is face sorting inside a shape for flame faces, beam faces, etc.
		/// </summary>
		/// <returns></returns>
		public int GetRenderOrder() // this is only for non-subshapes
		{
			int nVal = 0;

			// 0 means render first
			// higher means render later

			// i can probably improve on this a bit after reading the q3 shader manual's comments on sorting

			// 0 would be the most static geometry like walls and floors

			string sShaderName = m_q3Shader.GetShaderName();

			if (sShaderName.Contains("models")) nVal = 3; // models are always in front of walls

			if (!string.IsNullOrEmpty(sShaderName)) nVal = 4; // any shaders are next

			if (m_q3Shader.GetAddAlpha()) // alpha enabled shaders
			{
				if (m_q3Shader.GetSort() == "5")
					nVal = 5;
				else if (m_q3Shader.GetSort() == "6")
					nVal = 6;
				else
					nVal = 5;
			}

			if (sShaderName.Contains("slamp2") || sShaderName.Contains("kmlamp_white")) nVal = 7;

			if (sShaderName.Contains("spotlamp/beam") || sShaderName.Contains("proto_zzztblu3") ||
				sShaderName.Contains("teleporter/energy") || sShaderName.Contains("portal_sfx_ring")) nVal = 8;

			// proto_zzztblu3 is for the coil in dm0
			// slamp2 are the bulbs under the skull lights
			// beam is for spotlamp beams

			return nVal;
		}

		public List<Texture> GetTextures() { return m_lTextures; }

		public void ShowWireframe()
		{
			// for debugging can only show certain shapes here
			//if (!m_q3Shader.GetShaderName().Contains("flame")) return;

			for (int i = 0; i < m_lFaces.Count; i++)
				m_lFaces[i].Draw(Engine.EGraphicsMode.WIREFRAME);

			if (STATE.DrawFaceNormals)
			{
				DrawFaceNormals();

				/*for (int i = 0; i < m_lVerticeNormals.Count; i++)
				{
					Face.DrawNormalStatic(m_lVerticeNormals[i], m_lVertices[i], 0.1, new Color(100, 100, 0), new Color(50, 100, 150));
				}*/
			}
		}

		public D3Vect GetMidpoint() { return m_d3MidPoint; }

		/// <summary>
		/// Sort faces back to front from viewer. That's why we do disTwo length compared to disOne length.
		/// say disTwo length is 2 and disOne length is 1
		/// We want disTwo to be higher up in list so we do 2.compareto(1)
		/// this sorts descending
		/// </summary>
		/// <param name="f1"></param>
		/// <param name="f2"></param>
		/// <returns></returns>
        private static int CompareFaces(Face f1, Face f2)
        {
            D3Vect camTof1V1 = f1.GetVertices[0] - GameGlobals.m_CamPosition;
            double dLenf1v1 = camTof1V1.Length;

            D3Vect camTof1V2 = f1.GetVertices[1] - GameGlobals.m_CamPosition;
            double dLenf1v2 = camTof1V2.Length;

            D3Vect camTof1V3 = f1.GetVertices[2] - GameGlobals.m_CamPosition;
            double dLenf1v3 = camTof1V3.Length;

            D3Vect camTof2V1 = f2.GetVertices[0] - GameGlobals.m_CamPosition;
            double dLenf2v1 = camTof2V1.Length;

            D3Vect camTof2V2 = f2.GetVertices[1] - GameGlobals.m_CamPosition;
            double dLenf2v2 = camTof2V2.Length;

            D3Vect camTof2V3 = f2.GetVertices[2] - GameGlobals.m_CamPosition;
            double dLenf2v3 = camTof2V3.Length;

            double dMax1 = Math.Max(Math.Max(dLenf1v1, dLenf1v2), dLenf1v3);
            double dMax2 = Math.Max(Math.Max(dLenf2v1, dLenf2v2), dLenf2v3);

            return dMax2.CompareTo(dMax1);
        }

        private static int CompareFlames(Face f1, Face f2)
        {
			string s1 = f1.GetParentShape().GetQ3Shader().GetShaderName();
			string s2 = f2.GetParentShape().GetQ3Shader().GetShaderName();

			// take dot of cam to shape midpoint and face normal
			D3Vect camVec1 = f1.GetMidpoint() - GameGlobals.m_CamPosition;
            camVec1.normalize();
            double dot1 = D3Vect.DotProduct(camVec1, f1.GetNormal);

            D3Vect camVec2 = f2.GetMidpoint() - GameGlobals.m_CamPosition;
            camVec2.normalize();
            double dot2 = D3Vect.DotProduct(camVec2, f2.GetNormal);

            return Math.Abs(dot2).CompareTo(Math.Abs(dot1));
        }

        private void SortFaces()
		{
			// i need to sort the faces for this shape in the fastest way possible
			// the faces that get sent to renderer are in m_arIndices.
			// i think i can sort m_lFaces and then dump the indices into m_arIndices
			// should be decently fast
			// i don't think it matters what order the faces are in m_lFaces

			if (m_q3Shader.GetShaderName().Contains("flame"))
			{
				m_lFaces.Sort(CompareFlames);
			}
			else
			{
				m_lFaces.Sort(CompareFaces);
			}

			for( int i = 0; i < m_lFaces.Count; i++)
			{				
				m_arIndices[i * 3] = m_lFaces[i].GetIndice(0);
				m_arIndices[i * 3 + 1] = m_lFaces[i].GetIndice(1);
				m_arIndices[i * 3 + 2] = m_lFaces[i].GetIndice(2);
			}

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, m_arIndices.Length * sizeof(uint), m_arIndices, BufferUsageHint.StaticDraw);
        }

		/// <summary>
		/// Shows this shape. Loop over texture objects and set same number
		/// of texture units.
		/// </summary>
		public void Show()
		{
			// filter shape showing here for debugging
			//if (!m_q3Shader.GetShaderName().Contains("killblockgeomtrn")) return;

			if (DontRender()) return;

			// these apply to entire shader
			if (m_q3Shader.GetCull() == "disable" || m_q3Shader.GetCull() == "none" || m_q3Shader.GetCull() == "twosided")
			{
				GL.PushAttrib(AttribMask.EnableBit);
				GL.Disable(EnableCap.CullFace);
			}
			else if (m_q3Shader.GetCull() == "back")
			{
				GL.PushAttrib(AttribMask.EnableBit);
				GL.CullFace(CullFaceMode.Back);
			}

			if (m_q3Shader.GetAddAlpha())
			{
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			}
            // ***

            ShaderHelper.UseProgram(ShaderProgram);

			GL.BindVertexArray(VertexArrayObject);

			if(DoDistanceTest()) 
			{
				SortFaces();
			}

            // SET UNIFORMS ***
            int nLoc;

			if (string.IsNullOrEmpty(m_q3Shader.GetShaderName()))
			{
				nLoc = GL.GetUniformLocation(ShaderProgram, "texture0");
				GL.Uniform1(nLoc, 0);
				if (m_lTextures.Count > 1)
				{
					nLoc = GL.GetUniformLocation(ShaderProgram, "texture1");
					GL.Uniform1(nLoc, 1);
				}
			}
			else
			{
				// tcmods - these need to be before the rgbgen at the moment because some of these calculations can 
				// affect rgbgen
				// tcmod scroll can also dictate animmap for example for launch pads
				for (int i = 0; i < m_q3Shader.GetStages().Count; i++)
				{
					Q3ShaderStage stage = m_q3Shader.GetStages()[i];

					for (int j = 0; j < stage.GetTCMODS().Count; j++)
					{
						TCMOD mod = stage.GetTCMODS()[j];
						switch (mod.GetModType())
						{
							case TCMOD.ETYPE.SCALE:
								{
									nLoc = GL.GetUniformLocation(ShaderProgram, "scale" + Convert.ToString(i));
									stage.GetScaleValues(ref m_uniformFloat2);
									GL.Uniform2(nLoc, 1, m_uniformFloat2);
									break;
								}
							case TCMOD.ETYPE.SCROLL:
								{
									nLoc = GL.GetUniformLocation(ShaderProgram, "scroll" + Convert.ToString(i));
									stage.GetScrollValues(ref m_uniformFloat2);
									GL.Uniform2(nLoc, 1, m_uniformFloat2);
									break;
								}
							case TCMOD.ETYPE.TURB:
								{
									nLoc = GL.GetUniformLocation(ShaderProgram, "turb" + Convert.ToString(i));
									stage.GetTurbValues(ref m_uniformFloat3);
									GL.Uniform3(nLoc, 1, m_uniformFloat3);
									break;
								}
							case TCMOD.ETYPE.STRETCH:
								{
									nLoc = GL.GetUniformLocation(ShaderProgram, "stretch" + Convert.ToString(i));
									stage.GetStretchValues(ref m_uniformFloat6);
									GL.Uniform1(nLoc, 6, m_uniformFloat6);
									break;
								}
							case TCMOD.ETYPE.ROTATE:
								{
									nLoc = GL.GetUniformLocation(ShaderProgram, "rotate" + Convert.ToString(i));
									stage.GetRotateValues(ref m_uniformFloat6);
									GL.Uniform1(nLoc, 6, m_uniformFloat6);
									break;
								}
							case TCMOD.ETYPE.TRANSFORM:
                                {
                                    nLoc = GL.GetUniformLocation(ShaderProgram, "transform" + Convert.ToString(i));
                                    stage.GetTransformValues(ref m_uniformFloat6);
                                    GL.Uniform1(nLoc, 6, m_uniformFloat6);
                                    break;
                                }
						}
					}
				}

				// always send in time now because vertex shader can use it too
				nLoc = GL.GetUniformLocation(ShaderProgram, "timeS");
				GL.Uniform1(nLoc, GameGlobals.GetElapsedS());

				if (m_q3Shader.AutoSpriteEnabled())
				{
					nLoc = GL.GetUniformLocation(ShaderProgram, "autospriteMat");
					// setup autosprite vertex rotation matrix
					SetupAutospriteMat(nLoc, m_d3AutoSprite2UpVector);
				}

				// rgbgen and alphagen
				for (int i = 0; i < m_q3Shader.GetStages().Count; i++)
				{
					if (!m_q3Shader.GetStages()[i].IsRGBGENIdentity())
					{
						nLoc = GL.GetUniformLocation(ShaderProgram, "rgbgen" + i);
						m_q3Shader.GetStages()[i].GetRGBGenValue(ref m_uniformFloat3);
						GL.Uniform4(nLoc, m_uniformFloat3[0], m_uniformFloat3[1], m_uniformFloat3[2], 1.0f);
					}
					if (m_q3Shader.GetStages()[i].GetAlphaGenFunc() == GEN.ETYPE.WAVEFORM)
					{
						nLoc = GL.GetUniformLocation(ShaderProgram, "alphagen" + i);
						GL.Uniform1(nLoc, m_q3Shader.GetStages()[i].GetAlphaGenValue());
					}
				}

				nLoc = GL.GetUniformLocation(ShaderProgram, "camPosition");
				GL.Uniform3(nLoc, 1, GameGlobals.m_CamPosition.VectFloat());
			}

			// Activate textures - this needs to be after the uniforms above to make animmaps sync with waveforms
			if (!string.IsNullOrEmpty(m_q3Shader.GetShaderName()))
			{
				for (int i = 0; i < m_q3Shader.GetStages().Count; i++)
				{
					switch (i)
					{
						case 0: GL.ActiveTexture(TextureUnit.Texture0); break;
						case 1: GL.ActiveTexture(TextureUnit.Texture1); break;
						case 2: GL.ActiveTexture(TextureUnit.Texture2); break;
						case 3: GL.ActiveTexture(TextureUnit.Texture3); break;
						case 4: GL.ActiveTexture(TextureUnit.Texture4); break;
						case 5: GL.ActiveTexture(TextureUnit.Texture5); break;
					}
					Texture tex = m_q3Shader.GetStageTexture(i);
					if (tex != null)
					{
						m_q3Shader.GetStageTexture(i).bindMeRaw();
						ShaderHelper.printOpenGLError(m_q3Shader.GetShaderName());

						int nLocation = GL.GetUniformLocation(ShaderProgram, "texture" + Convert.ToString(i));
						GL.Uniform1(nLocation, i);
						ShaderHelper.printOpenGLError(m_q3Shader.GetShaderName());
					}
				}
			}
			else
			{
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
				GetMainTexture().bindMeRaw(); // placeholder, not sure this is needed
			}

			ShaderHelper.printOpenGLError(m_q3Shader.GetShaderName());
			
			GL.GetFloat(GetPName.ProjectionMatrix, m_util4x4);
            nLoc = GL.GetUniformLocation(ShaderProgram, "proj");
            GL.UniformMatrix4(nLoc, 1, false, m_util4x4);

            GL.GetFloat(GetPName.ModelviewMatrix, m_util4x4);
			nLoc = GL.GetUniformLocation(ShaderProgram, "modelview");
			GL.UniformMatrix4(nLoc, 1, false, m_util4x4);
			
			// END SET UNIFORMS ***

			GL.DrawElements(PrimitiveType.Triangles, m_arIndices.Length, DrawElementsType.UnsignedInt, 0);

			GL.UseProgram(0);

			// apply to whole shader
			if (m_q3Shader.GetCull() == "disable" || m_q3Shader.GetCull() == "none" || m_q3Shader.GetCull() == "back")
			{
				GL.PopAttrib();
			}
			if (m_q3Shader.GetAddAlpha())
			{
				GL.Disable(EnableCap.Blend);
			}
			// ***

			if (STATE.DrawFaceNormals)
			{
				DrawFaceNormals();
			}
		}

		private void SetupAutospriteMat(int nUniformLocation, D3Vect upvectorOptional)
		{
			D3Vect camForward = m_d3MidPoint - GameGlobals.m_CamPosition;
			camForward.normalize();

			double dDot = D3Vect.DotProduct(m_lVertexNormals[0], camForward);
			double dAngle = Math.Acos(dDot) * GLB.RadToDeg;

			D3Vect d3RotationVector;

			if (upvectorOptional.Empty)
			{
				d3RotationVector = new D3Vect(camForward, m_lVertexNormals[0]); // cross product	
				dAngle = 180 - dAngle;
			}
			else
			{				
				d3RotationVector = upvectorOptional;
				dAngle = 180 - dAngle;

				// this doesn't work right yet
				// todo : get this to rotate correctly to face the camera always

				//dAngle = 0;

				//dAngle = dAngle + 180;
				/*if(dDot <= 0)
                {
					d3RotationVector.Negate();
                }*/
			}			

			sgl.PUSHMAT();
			GL.LoadIdentity();

			GL.Translate((float)m_d3MidPoint.x, (float)m_d3MidPoint.y, (float)m_d3MidPoint.z);
			GL.Rotate((float)dAngle, (float)d3RotationVector.x, (float)d3RotationVector.y, (float)d3RotationVector.z);
			GL.Translate((float)-m_d3MidPoint.x, (float)-m_d3MidPoint.y, (float)-m_d3MidPoint.z);

			GL.GetFloat(GetPName.ModelviewMatrix, m_util4x4);
			sgl.POPMAT();

            GL.UniformMatrix4(nUniformLocation, 1, true, m_util4x4);
		}

		private void DrawFaceNormals()
		{
			if (!m_q3Shader.GetShaderName().Contains("flare03")) return;

			/*for (int i = 0; i < m_lFaces.Count; i++)
			{
				m_lFaces[i].DrawNormals();
			}*/

			for(int i = 0; i < m_lVertexNormals.Count; i++)
			{
				Face.DrawNormalStatic(m_lVertexNormals[i], m_lVertices[i], .5, new utilities.Color(255, 0, 0), new utilities.Color(0, 255, 0));
			}
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
			if (!ReadCoordinateIndexes(sr, ref nCounter)) return false;
			if (!ReadVertices(sr, ref nCounter)) return false;

			CreateSubShapes();

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

		private bool DoDistanceTest()
		{
			string sTex = GetMainTexture().GetPath();
			return sTex.Contains("ctf/blue_telep") || sTex.Contains("ctf/red_telep") ||
				sTex.Contains("sfx/console01") || sTex.Contains("sfx/beam") || sTex.Contains("spotlamp/beam") ||
				sTex.Contains("sfx/console03") || sTex.Contains("bot_flare") || sTex.Contains("lamps/beam") ||
				sTex.Contains("teleporter/energy") || sTex.Contains("flame");
		}

        private List<List<int>> GetMatchedFaces(List<List<int>> lCoordIndicesCopy, List<int> face)
        {
			List<List<int>> lMatchesFaces = new List<List<int>>();
            for(int i = 0; i < lCoordIndicesCopy.Count; i++)
			{
				if(ShareVert(lCoordIndicesCopy[i], face))
				{
					lMatchesFaces.Add(lCoordIndicesCopy[i]);
				}
			}
			return lMatchesFaces;
        }

		private void ProcessCurShape(List<List<int>> lCurShape, List<List<int>> lCoordIndices, List<int> seedFace)
		{			
            List<List<int>> lMatchedFaces = GetMatchedFaces(lCoordIndices, seedFace);

			// add matched faces to cur shape
			// remove matched faces from lCoordIndices
			// loop over lMatchedFaces and call ProcessCurShape

			if(lMatchedFaces.Count > 0)
			{
                foreach (List<int> face in lMatchedFaces)
                {
					lCurShape.Add(face);
                    RemoveFace(face, lCoordIndices);
                }

                for (int i = 0; i < lMatchedFaces.Count; i++)
                {
                    ProcessCurShape(lCurShape, lCoordIndices, lMatchedFaces[i]);
                }
            }
			else
			{
				return;
			}
        }

		private void RemoveFace(List<int> face, List<List<int>> lCoordIndices)
		{
			int nCounter = 0;
			foreach(List<int> f in lCoordIndices)
			{
				if(f[0] == face[0] && f[1] == face[1] && f[2] == face[2])
				{
					lCoordIndices.RemoveAt(nCounter);
					break;
				}
				nCounter++;
			}
		}

		/// <summary>
		/// peak into the q3 shader and see if a line contains token without being a comment line
		/// </summary>
		/// <param name="sToken"></param>
		/// <returns></returns>
		private bool PeakQ3Shader(List<string> lSearchTokens)
		{
			bool bFound = false;
            List<string> lShaderLines;

			bool bFoundShader = GameGlobals.m_dictQ3ShaderContent.TryGetValue(Path.ChangeExtension(GetMainTexture().GetPath(), null), out lShaderLines);

			if(bFoundShader)
			{
				for(int i = 0; i < lShaderLines.Count; i++)
				{
					// comments are already removed
					string[] tokens = Q3Shader.GetTokens(lShaderLines[i]);
					foreach(string token in tokens)
					{
						if(lSearchTokens.Contains(token))
						{
							bFound = true;
							break;
						}
					}
					if (bFound) break;
				}
			}
			return bFound;
        }

		private bool SubShapeEnabled()
		{
            string sTex = GetMainTexture().GetPath();

            bool bPortal = GameGlobals.IsPortalEntry(sTex);
            bool bTeleporter = GameGlobals.IsTeleporterEntry(sTex);

			bool bEnabled = sTex.Contains("sfx/console01") || sTex.Contains("sfx/beam") ||
				sTex.Contains("sfx/console03") || sTex.Contains("teleporter/energy") ||
				sTex.Contains("jets") || (sTex.Contains("liquids") && !sTex.Contains("lava")) || sTex.Contains("lamps/beam") ||
				sTex.Contains("colua0_lght") || bPortal || bTeleporter;

			if(!bEnabled)
			{
				bEnabled = PeakQ3Shader(new List<string> { "autosprite", "autosprite2" });
			}

			if(!bEnabled)
            {
				bEnabled = GameGlobals.IsJumpLaunchPad(sTex);
            }

			return bEnabled;
        }

		private void CreateSubShapes()
		{
			if (!m_bSubShape && SubShapeEnabled())
			{
				List<List<int>> lCoordIndicesCopy = new List<List<int>>(m_lCoordinateIndexes);

				while(lCoordIndicesCopy.Count > 0) 
				{
					m_lSubShapes.Add(new List<List<int>>());
					List<List<int>> curSubShape = m_lSubShapes[m_lSubShapes.Count - 1];

					curSubShape.Add(lCoordIndicesCopy[0]);
					List<int> seedFace = new List<int>(lCoordIndicesCopy[0]);
					lCoordIndicesCopy.RemoveAt(0);

					ProcessCurShape(curSubShape, lCoordIndicesCopy, seedFace);
				}

				LOGGER.Info("Converted " + GetMainTexture().GetPath() + " into " + m_lSubShapes.Count + " subshapes");
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

		private bool ShareVert(List<int> lFace1, List<int> lFace2)
		{
			for(int i = 0; i < lFace1.Count; i++)
			{
				for(int j = 0; j < lFace2.Count; j++)
				{
					if(D3Vect.Equals(m_lVertices[lFace1[i]], m_lVertices[lFace2[j]]))
					{
						return true;
					}
				}
			}
			return false; 
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
					for (int i = 0;  i < sTokens.Length - 1; i++)
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
