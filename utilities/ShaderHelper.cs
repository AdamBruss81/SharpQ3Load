using System;
using OpenTK.Graphics.OpenGL;
using System.IO;

namespace utilities
{
	public class ShaderHelper
	{
		public static void AttachShader(int nProgram, string sPathOrContent, ShaderType eType, out int nShader, bool bContent, string sLabel)
		{
			if (!GL.IsProgram(nProgram)) {
				nShader = -1;
			}
			else
			{
				string sSource;
				if (bContent) sSource = sPathOrContent;
				else ReadSource(out sSource, sPathOrContent);

				if (eType == ShaderType.VertexShader) 
					nShader = GL.CreateShader(ShaderType.VertexShader);
				else if (eType == ShaderType.FragmentShader)
					nShader = GL.CreateShader(ShaderType.FragmentShader);
				else
				{
					nShader = -1;
					return;
				}

				GL.ShaderSource(nShader, sSource);

				int nStatus;
				int nLinked;

				GL.CompileShader(nShader);
				GL.GetShader(nShader, ShaderParameter.CompileStatus, out nStatus);

				if (nStatus == 0) printShaderInfoLog(nShader, sLabel);

				GL.AttachShader(nProgram, nShader);
				GL.LinkProgram(nProgram);
				GL.GetProgram(nProgram, GetProgramParameterName.LinkStatus, out nLinked);
				if(nLinked == 0) printProgramInfoLog(nProgram);
			}
		}

        public static void AttachShadersFromContent(int nProgram, string sVert, string sFrag,
            out int nVertShader, out int nFragShader, string sLabel)
        {
            AttachShader(nProgram, sVert, ShaderType.VertexShader, out nVertShader, true, sLabel);
            AttachShader(nProgram, sFrag, ShaderType.FragmentShader, out nFragShader, true, sLabel);
        }

        public static void AttachShaders(int nProgram, string sVertPath, string sFragPath, 
			out int nVertShader, out int nFragShader)
		{
			AttachShader(nProgram, sVertPath, ShaderType.VertexShader, out nVertShader, false, "");
			AttachShader(nProgram, sFragPath, ShaderType.FragmentShader, out nFragShader, false, "");
		}

		public static void DetachShader(int nRunningProgram, int nShader)
		{
			if (!GL.IsProgram(nRunningProgram)) return;
			else {
				GL.DetachShader(nRunningProgram, nShader);
			}
		}

		public static void DetachAndDeleteShader(int nProgram, int nShader)
		{
			if (!GL.IsProgram(nProgram)) return;
			else
			{
				GL.DetachShader(nProgram, nShader);
				GL.DeleteShader(nShader);
			}
		}

		public static int CreateProgram()
		{
			return GL.CreateProgram();
		}

		public static int CreateProgram(string sPath, ShaderType eType)
		{
			int nProgram = CreateProgram();

			int nShader;
			AttachShader(nProgram, sPath, eType, out nShader, false, "");
			return nProgram;
		}

		public static int CreateProgramFromContent(string sVertShader, string sFragShader, string sLabel)
		{
            int nProgram = CreateProgram();

            int nVertShader, nFragShader;
            AttachShadersFromContent(nProgram, sVertShader, sFragShader, out nVertShader, out nFragShader, sLabel);
            return nProgram;
        }

		public static int CreateProgram(string sPathVert, string sPathFrag)
		{
			int nProgram = CreateProgram();

			int nVertShader, nFragShader;
			AttachShaders(nProgram, sPathVert, sPathFrag, out nVertShader, out nFragShader);
			return nProgram;
		}

		public static int CreateProgram(string sPath, ShaderType eType, out int nShader)
		{
			int nProgram = CreateProgram();

			AttachShader(nProgram, sPath, eType, out nShader, false, "");
			return nProgram;
		}

		public static int CreateProgram(string sPathVert, string sPathFrag, out int nVertShader, out int nFragShader)
		{
			int nProgram = CreateProgram();

			AttachShaders(nProgram, sPathVert, sPathFrag, out nVertShader, out nFragShader);
			return nProgram;
		}

		public static void UseProgram(int nProgram)
		{
			GL.UseProgram(nProgram);
			printProgramInfoLog(nProgram);
		}

		public static int GetOpenGLErrors(ref string sErrors)
		{
            ErrorCode eCode;
            int retCode = 0;

            eCode = GL.GetError();
            while (eCode != ErrorCode.NoError)
            {
				sErrors += "glError : " + eCode;
                retCode = 1;
                eCode = GL.GetError();
            }
			return retCode;
        }

		public static int printOpenGLError(string sHelperStr)
		{
			string s = "";
			int retCode = GetOpenGLErrors(ref s);
			s = s.Trim();
			if (!string.IsNullOrEmpty(s))
			{
				if (sHelperStr == "")
				{
					System.Diagnostics.Trace.WriteLine(s);
					//System.IO.File.WriteAllText("c:\\temp\\glout_release.txt", s);
				}
				else
				{
					System.Diagnostics.Trace.WriteLine(s + " from " + sHelperStr);
					//System.IO.File.WriteAllText("c:\\temp\\glout_release.txt", s + " from " + sHelperStr);
				}
			}
			return retCode;
		}

		public static void CloseProgram(int nProgram)
		{
			int nMaxAttached = 100;
			int nCount;
			int[] nShaders = new int[nMaxAttached];

			if (GL.IsProgram(nProgram))
			{
				GL.GetAttachedShaders(nProgram, nMaxAttached, out nCount, nShaders);
				for (int i = 0; i < nCount; i++)
				{
					GL.DetachShader(nProgram, nShaders[i]);
					GL.DeleteShader(nShaders[i]);
				}
				GL.DeleteProgram(nProgram);
				GL.UseProgram(0);
			}
		}

		public static void printShaderInfoLog(int nShader, string sLabel)
		{
			int infologLength = 0;
			int charsWritten = 0;			

			GL.GetShader(nShader, ShaderParameter.InfoLogLength, out infologLength);

			if (infologLength > 0)
			{
				string infoLog;
				GL.GetShaderInfoLog(nShader, infologLength, out charsWritten, out infoLog);
				System.Diagnostics.Trace.WriteLine(sLabel + " : Shader InfoLog:\n" + infoLog);
			}
		}

		public static void printProgramInfoLog(int nProgram)
		{
			int infologLength;
			int charsWritten;

			GL.GetProgram(nProgram, GetProgramParameterName.InfoLogLength, out infologLength);

			if (infologLength > 0)
			{
				string infoLog;
				GL.GetProgramInfoLog(nProgram, infologLength, out charsWritten, out infoLog);
				if(infoLog.Trim().Length > 0)
					System.Diagnostics.Trace.WriteLine("Program InfoLog:\n" + infoLog);
			}
		}

#region internal
		private static void ReadSource(out string sSource, string sPath)
		{
			StreamReader sr = new StreamReader(sPath);
			sSource = sr.ReadToEnd();
			sr.Close();
		}
#endregion
	}
}
