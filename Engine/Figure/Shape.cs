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
		List<List<int>> m_lCoordinateIndexes = new List<List<int>>(); // the inner list always has 3 elements
		List<D3Vect> m_lVertices = new List<D3Vect>();
		List<List<DPoint>> m_lTexCoordinates = new List<List<DPoint>>();
		List<D3Vect> m_lVerticeColors = new List<D3Vect>();
		Q3Shader m_q3Shader = null;
		List<D3Vect> m_lVerticeNormals = new List<D3Vect>();
		List<List<List<int>>> m_lSubShapes = new List<List<List<int>>>(); // for dividing up shapes into sub shapes to fix transparency issues on rendering
																		  // could also be used later to control properties of jumppads
		D3Vect m_d3MidPoint = null;
		bool m_bSubShape = false;

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
		// ===

		// consts
		const string m_sVerticeColorHeader = "color Color { color [";
		const string m_sTextureCoordinatesHeader = "TextureCoordinate { point [";
		const string m_sChannelOneTextureCoordinatesHeader = "texCoord  TextureCoordinate { point [";
		const string m_sMeshCoordinatesHeader = "coord Coordinate { point [";
		const string m_sCoordinateIndexHeader = "coordIndex [";
		private const string g_sDefaultTexture = "textures/base_floor/clang_floor.jpg";

		ETextureType m_TextureType;

		public Shape() { m_q3Shader = new Q3Shader(this); }

		public List<Face> GetMapFaces() { return m_lFaces; }

		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="s">source to copy from to this</param>
		public Shape(Shape s)
		{
			m_lTextures = new List<Texture>();
			m_lTextures.AddRange(s.m_lTextures);
			//m_lFaces.AddRange(s.m_lFaces); // going to create new faces
			//m_lCoordinateIndexes.AddRange(s.m_lCoordinateIndexes); going to set this later
			m_lVertices.AddRange(s.m_lVertices);
			m_lTexCoordinates.AddRange(s.m_lTexCoordinates);
			m_lVerticeColors.AddRange(s.m_lVerticeColors);
			m_TextureType = s.m_TextureType;
			m_q3Shader = new Q3Shader(this);
		}

		public void Delete()
		{
			ShaderHelper.CloseProgram(ShaderProgram);

			foreach (Texture t in m_lTextures) {
				t.Delete();
			}
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
						}
					}
				}
			}

			bool bDeformVWavePresent = false;
			bool bDeformBulgePresent = false;
			bool bDeformMovePresent = false;
			for (int i = 0; i < m_q3Shader.GetDeformVertexes().Count; i++)
			{
				if (m_q3Shader.GetDeformVertexes()[i].m_eType == DeformVertexes.EDeformVType.WAVE)
				{
					bDeformVWavePresent = true;
				}
				if (m_q3Shader.GetDeformVertexes()[i].m_eType == DeformVertexes.EDeformVType.BULGE)
				{
					bDeformBulgePresent = true;
				}
				if (m_q3Shader.GetDeformVertexes()[i].m_eType == DeformVertexes.EDeformVType.MOVE)
				{
					bDeformMovePresent = true;
				}
			}

			if (bSendSinTable)
			{
				sb.AppendLine("uniform float sinValues[1024];");
			}

			if (bUsesTCGen)
			{
				sb.AppendLine("");
				sb.AppendLine("void CalculateTcGen(in vec3 campos, in vec3 position, in vec3 vertexnormal, out vec2 tcgen)");
				sb.AppendLine("{");
				sb.AppendLine("vec3 viewer = campos - position;");
				sb.AppendLine("viewer = normalize(viewer);");
				sb.AppendLine("float d = dot(vertexNormal, viewer);");
				sb.AppendLine("vec3 reflected = vertexnormal * 2.0 * d - viewer;");
				sb.AppendLine("tcgen[0] = 0.5 + reflected[0] * 0.5;");
				sb.AppendLine("tcgen[1] = 0.5 - reflected[1] * 0.5;");
				sb.AppendLine("}");
				sb.AppendLine("");
			}

			if (bUsesAlphaGenspec)
			{
				sb.AppendLine("");
				sb.AppendLine("void CalculateAlphaGenSpec(in vec3 campos, in vec3 position, in vec3 vertexnormal, out float alpha)");
				sb.AppendLine("{");
				sb.AppendLine("vec3 lightorigin = vec3(-960, 1980, 96);");
				sb.AppendLine("vec3 lightdir = lightorigin - position;");
				sb.AppendLine("lightdir = normalize(lightdir);");
				sb.AppendLine("float d = dot(vertexnormal, lightdir);");
				sb.AppendLine("vec3 reflected = vertexnormal * 2 * d - lightdir;");
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
				sb.AppendLine("if(wavefunc == 0f) {"); // just sin for now
				sb.AppendLine("float divMult = -.261 * freq + .1861;");
				// my vertices are a lot smaller than the bsp ones so i need to scale the div, amp and base				
				sb.AppendLine("float off = ( vertex[0] + vertex[1] + vertex[2] ) * (div * divMult);");
				sb.AppendLine("float fCycleTimeMS = 1.0f / (freq * 2.0f);");
				sb.AppendLine("float fIntoSin = (timeS + (phase + off) * freq) / fCycleTimeMS * 3.1415926f;");
				sb.AppendLine("float fSinValue = sin(fIntoSin);");
				sb.AppendLine("float fScale = 0.008;");
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
				sb.AppendLine("float now = timeS * speed * 0.25f;");
				sb.AppendLine("float off = 3.1415926f * 2 * (aTexCoord[0] * width + now);");
				sb.AppendLine("float scale = sin(off) * (height*.025);");
				sb.AppendLine("vertex += vertexNormal * scale;");
				sb.AppendLine("}");
			}

			if (bDeformMovePresent)
			{
				// insert function to move a vertex     

				// wavefunc: sin, triangle, square, sawtooth or inversesawtooth : 0,1,2,3,4

				sb.AppendLine("void MoveVertexes(in float x, in float y, in float z, in float wavefunc, in float base, in float amp, in float phase, in float freq, inout vec3 vertex)");
				sb.AppendLine("{");
				sb.AppendLine("if(wavefunc == 0f) {"); // just sin for now
				sb.AppendLine("float fCycleTimeMS = 1.0f / (freq * 2.0f);");
				sb.AppendLine("float fIntoSin = (timeS + phase * freq) / fCycleTimeMS * 3.1415926f;");
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
				sb.AppendLine("CalculateTcGen(camPosition, aPosition, vertexNormal, tcgenEnvTexCoord);");
			}
			if (bUsesAlphaGenspec)
			{
				sb.AppendLine("CalculateAlphaGenSpec(camPosition, aPosition, vertexNormal, alphaGenSpecular);");
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
								sb.AppendLine("float " + sTexmod + "_stretch_x = " + sTexmod + ".x;");
								sb.AppendLine("float " + sTexmod + "_stretch_y = " + sTexmod + ".y;");
								sb.AppendLine(sTexmod + ".x = " + sTexmod + "_stretch_x * stretch" + sIndex + "[0] + " + sTexmod + "_stretch_y * stretch" + sIndex + "[1] + stretch" + sIndex + "[4];");
								sb.AppendLine(sTexmod + ".y = " + sTexmod + "_stretch_x * stretch" + sIndex + "[2] + " + sTexmod + "_stretch_y * stretch" + sIndex + "[3] + stretch" + sIndex + "[5];");
								break;
							}
						case TCMOD.ETYPE.ROTATE:
							{
								// 0 - 3 are the 2x2 transform matrix
								// 4-5 are the translate vector
								sb.AppendLine("float " + sTexmod + "_rotate_x = " + sTexmod + ".x;");
								sb.AppendLine("float " + sTexmod + "_rotate_y = " + sTexmod + ".y;");
								sb.AppendLine(sTexmod + ".x = " + sTexmod + "_rotate_x * rotate" + sIndex + "[0] + " + sTexmod + "_rotate_y * rotate" + sIndex + "[1] + rotate" + sIndex + "[4];");
								sb.AppendLine(sTexmod + ".y = " + sTexmod + "_rotate_x * rotate" + sIndex + "[2] + " + sTexmod + "_rotate_y * rotate" + sIndex + "[3] + rotate" + sIndex + "[5];");
								break;
							}
						case TCMOD.ETYPE.TURB:
							{
								sb.AppendLine("float turbVal" + sIndex + " = turb" + sIndex + "[1] + timeS * turb" + sIndex + "[2];");

								//sb.AppendLine(sTexmod + ".x += sin( ( (vertice.x + vertice.z) * 1.0/128.0 * 0.125 + turbVal" + sIndex + " ) * 6.238) * turb" + sIndex + "[0];");
								//sb.AppendLine(sTexmod + ".y += sin( (vertice.y * 1.0/128.0 * 0.125 + turbVal" + sIndex + " ) * 6.238) * turb" + sIndex + "[0];");

								//sb.AppendLine(sTexmod + ".x = " + sTexmod + ".x + sinValues[ int ( ( ( vertice.x + vertice.z ) + turbVal" + sIndex + " ) * 1024 ) & 1023 ] * turb" + sIndex + "[0];");
								//sb.AppendLine(sTexmod + ".y = " + sTexmod + ".y + sinValues[ int ( ( ( vertice.y ) + turbVal" + sIndex + " ) * 1024 ) & 1023 ] * turb" + sIndex + "[0];");

								sb.AppendLine(sTexmod + ".x = " + sTexmod + ".x + sinValues[ int ( ( ( vertice.x + vertice.z ) * 0.125 * 1.0/128 + turbVal" + sIndex + " ) * 1024 ) & 1023 ] * turb" + sIndex + "[0];");
								sb.AppendLine(sTexmod + ".y = " + sTexmod + ".y + sinValues[ int ( ( ( vertice.y ) * 0.125 * 1.0/128 + turbVal" + sIndex + " ) * 1024 ) & 1023 ] * turb" + sIndex + "[0];");

								break;
							}
					}
				}
			}

			sb.AppendLine("");

			sb.AppendLine("color = aColor;");

			if (bDeformVWavePresent || bDeformBulgePresent || bDeformMovePresent)
			{
				sb.AppendLine("vec3 newPosition = aPosition;");
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
				}
			}

			string sFinalPosName = (bDeformVWavePresent | bDeformBulgePresent | bDeformMovePresent) ? "newPosition" : "aPosition";
			sb.AppendLine("vertice = " + sFinalPosName + ";");
			sb.AppendLine("gl_Position = proj * modelview * vec4(" + sFinalPosName + ", 1.0);");

			sb.AppendLine("}");

			return sb.ToString();
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
				m_q3Shader.ConvertQ3ShaderToGLSL(sb);
			}
			else
			{
				// default shader for lightmapped or vert colored faces

				// texture uniform samplers
				sb.AppendLine("uniform " + Q3Shader.GetSampler2DName() + " texture0;"); // main texture
				if (m_lTextures.Count > 0) sb.AppendLine("uniform " + Q3Shader.GetSampler2DName() + " texture1;");

				sb.AppendLine("void main()");
				sb.AppendLine("{");
				sb.AppendLine("if (lightmapTexCoord.x != -1.0) {");
				sb.AppendLine("vec4 main_tex_texel = texture(texture0, mainTexCoord);");
				sb.AppendLine("vec4 lightmap_texel = texture(texture1, lightmapTexCoord);");
				sb.AppendLine("outputColor = clamp(main_tex_texel * lightmap_texel * 3.0, 0.0, 1.0);");
				sb.AppendLine("}");
				sb.AppendLine("else {");
				sb.AppendLine("vec4 texel0 = texture(texture0, mainTexCoord);");
				sb.AppendLine("outputColor = texel0 * color * 3.0;");
				sb.AppendLine("}");
				sb.AppendLine("}");
			}

			return sb.ToString();
		}

		public void InitializeLists()
		{
			m_q3Shader.ReadQ3Shader(GetMainTexture().GetPath());

			bool bShouldBeTGA = false;
			foreach (Texture t in m_lTextures)
			{
				if (GetMainTexture() == t)
				{
					string sNonShaderTexture = m_q3Shader.GetPathToTextureNoShaderLookup(false, t.GetPath(), ref bShouldBeTGA);
					if (File.Exists(sNonShaderTexture))
						t.SetTexture(sNonShaderTexture, bShouldBeTGA, m_q3Shader.GetShaderName());
					// else: should be using q3 shader then. no need to set t to anything. it represents the shader.
				}
				else
				{
					// lightmap for non shader shape
					t.SetTexture(m_q3Shader.GetPathToTextureNoShaderLookup(true, t.GetPath(), ref bShouldBeTGA), false, m_q3Shader.GetShaderName());
				}
			}

			foreach (Face f in m_lFaces)
			{
				f.InitializeLists();
			}

			// use modern open gl via vertex buffers, vertex array, element buffer and shaders
			// setup vertices
			int nNumValues = 14;
			m_arVertices = new double[m_lVertices.Count * nNumValues]; // vertices, texcoord1, texcoord2(could be dummy if no lightmap), color
			for (int i = 0; i < m_lVertices.Count; i++)
			{
				int nBase = i * nNumValues;

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
				vNormal = vNormal / nCounter;
				vNormal.normalize();

				vNormal.Negate();
				m_lVerticeNormals.Add(vNormal);

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

			string autoGenereatedGLSL = CreateGLSLFragShader();
			string autoGenereatedVertexShader = CreateGLSLVertShader();
			ShaderProgram = ShaderHelper.CreateProgramFromContent(autoGenereatedVertexShader, autoGenereatedGLSL, m_q3Shader.GetShaderName());

#if DEBUG
			if (!string.IsNullOrEmpty(autoGenereatedGLSL))
			{
				File.WriteAllText("c:\\temp\\" + Path.GetFileName(m_q3Shader.GetShaderName()) + ".frag.txt", autoGenereatedGLSL);
				File.WriteAllText("c:\\temp\\" + Path.GetFileName(m_q3Shader.GetShaderName()) + ".vert.txt", autoGenereatedVertexShader);
			}
#endif

			VertexBufferObject = GL.GenBuffer();
			VertexArrayObject = GL.GenVertexArray();
			ElementBufferObject = GL.GenBuffer();

			ShaderHelper.printOpenGLError("");

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

			GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Double, false, nNumValues * sizeof(double), 11 * sizeof(double));
			GL.EnableVertexAttribArray(4);

			ShaderHelper.printOpenGLError("");

			// setup element buffer for vertex indices
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
			GL.BufferData(BufferTarget.ElementArrayBuffer, m_arIndices.Length * sizeof(uint), m_arIndices, BufferUsageHint.StaticDraw);
			// ===

			ShaderHelper.printOpenGLError("");
		}

		public int GetIndex(Face f)
		{
			return m_lFaces.IndexOf(f);
		}

		public void ReadMain(List<Texture> lTextures, StreamReader sr, List<Face> lFaceReferences, ref int nCounter)
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

			if (Read(sr, ref nCounter))
			{
				CreateFaces(lFaceReferences);
			}
			else
				throw new Exception("Error in reading shape data from file");
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
				//LOGGER.Debug("Allocating map face");
				pFace = new Face(faceVerts, faceTexCoords, faceVertColors, new Color(240, 0, 0), new Color(100, 0, 0), lFaceReferences.Count);
				pFace.SetParentShape(this);
				m_lFaces.Add(pFace);
				lFaceReferences.Add(pFace);
				//LOGGER.Debug("Added a face to the figure's map face references. Count = " + lFaceReferences.Count.ToString());
				Notify((int)ESignals.FACE_CREATED);
				pFace = null;
				faceVerts.Clear();
				faceTexCoords[0].Clear();
				if (m_TextureType == ETextureType.MULTI)
					faceTexCoords[1].Clear();
				faceVertColors.Clear();
			}

			if (m_bSubShape)
			{
				// calculate mid point based on faces
				m_d3MidPoint = new D3Vect();
				for (int i = 0; i < m_lFaces.Count; i++)
				{
					m_d3MidPoint += m_lFaces[i].GetMidpoint();
				}
				m_d3MidPoint /= m_lFaces.Count;
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
				if (sName.Contains("fog") || sName.Contains("clip"))
					bRender = false;
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

		public int GetRenderOrder()
		{
			int nVal = 0;

			// 0 means render first
			// higher means render later

			// i can probably improve on this a bit after reading the q3 shader manual's comments on sorting

			string sShaderName = m_q3Shader.GetShaderName();

			if (sShaderName.Contains("models")) nVal = 3;

			if (!string.IsNullOrEmpty(sShaderName)) nVal = 4;

			if (m_q3Shader.GetAddAlpha())
			{
				if (m_q3Shader.GetSort() == "5")
					nVal = 5;
				else if (m_q3Shader.GetSort() == "6")
					nVal = 6;
				else
					nVal = 5;
			}

			if (sShaderName.Contains("slamp2") || sShaderName.Contains("kmlamp_white")) nVal = 7;

			if (sShaderName.Contains("flame") || sShaderName.Contains("beam") || sShaderName.Contains("proto_zzztblu3") ||
				sShaderName.Contains("teleporter/energy") || sShaderName.Contains("bot_flare") || sShaderName.Contains("portal_sfx_ring")) nVal = 8;

			// proto_zzztblu3 is for the coil in dm0
			// slamp2 are the bulbs under the skull lights

			return nVal;
		}

		public List<Texture> GetTextures() { return m_lTextures; }

		public void ShowWireframe()
		{
			// for debugging can only show certain shapes here
			if (!m_q3Shader.GetShaderName().Contains("beam")) return;

			for (int i = 0; i < m_lFaces.Count; i++)
				m_lFaces[i].Draw(Engine.EGraphicsMode.WIREFRAME);

			if (STATE.DrawFaceNormals)
			{
				//DrawFaceNormals();

				for (int i = 0; i < m_lVerticeNormals.Count; i++)
				{
					Face.DrawNormalStatic(m_lVerticeNormals[i], m_lVertices[i], 0.1, new Color(100, 100, 0), new Color(50, 100, 150));
				}
			}
		}

		public D3Vect GetMidpoint() { return m_d3MidPoint; }

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
				bool bSendSinTable = false;
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
									bSendSinTable = true;
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
						}
					}
				}

				// always send in time now because vertex shader can use it too
				nLoc = GL.GetUniformLocation(ShaderProgram, "timeS");
				GL.Uniform1(nLoc, GameGlobals.GetElapsedS());

				if (bSendSinTable)
				{
					nLoc = GL.GetUniformLocation(ShaderProgram, "sinValues");
					GL.Uniform1(nLoc, 1024, GameGlobals.m_SinTable);
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
					if (m_q3Shader.GetStages()[i].GetAlphaGenFunc() == "wave")
					{
						nLoc = GL.GetUniformLocation(ShaderProgram, "alphagen" + i);
						GL.Uniform1(nLoc, m_q3Shader.GetStages()[i].GetAlphaGenValue());
					}
				}

				nLoc = GL.GetUniformLocation(ShaderProgram, "camPosition");
				GL.Uniform3(nLoc, 1, GameGlobals.m_CamPosition.VectFloat());
			}
			// END SET UNIFORMS ***

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

		private void DrawFaceNormals()
		{
			for (int i = 0; i < m_lFaces.Count; i++)
			{
				m_lFaces[i].DrawNormals();
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
			//return false;

			string sTex = GetMainTexture().GetPath();
			return sTex.Contains("ctf/blue_telep") || sTex.Contains("ctf/red_telep") ||
				sTex.Contains("sfx/console01") || sTex.Contains("sfx/beam") ||
				sTex.Contains("sfx/console03");
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

		private void CreateSubShapes()
		{
			if (!m_bSubShape && DoDistanceTest())
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
