using System;
using Tao.OpenGl;
using System.IO;

namespace utilities
{
	public class ShaderHelper
	{
		public static void AttachShader(int nProgram, string sPath, int nType, out int nShader)
		{
			if (Gl.glIsProgram(nProgram) == 0) {
				nShader = -1;
			}
			else
			{
				string sSource;
				ReadSource(out sSource, sPath);

				if (nType == Gl.GL_VERTEX_SHADER) 
					nShader = Gl.glCreateShader(Gl.GL_VERTEX_SHADER);
				else if (nType == Gl.GL_FRAGMENT_SHADER)
					nShader = Gl.glCreateShader(Gl.GL_FRAGMENT_SHADER);
				else
				{
					nShader = -1;
					return;
				}

				string[] aShader = { sSource };

				Gl.glShaderSource(nShader, 1, aShader, null);

				int nStatus;
				int nLinked;

				Gl.glCompileShader(nShader);
				Gl.glGetShaderiv(nShader, Gl.GL_COMPILE_STATUS, out nStatus);
				if(nStatus == 0) printShaderInfoLog(nShader);

				Gl.glAttachShader(nProgram, nShader);
				Gl.glLinkProgram(nProgram);
				Gl.glGetProgramiv(nProgram, Gl.GL_LINK_STATUS, out nLinked);
				if(nLinked == 0) printProgramInfoLog(nProgram);
			}
		}

		public static void AttachShaders(int nProgram, string sVertPath, string sFragPath, 
			out int nVertShader, out int nFragShader)
		{
			AttachShader(nProgram, sVertPath, Gl.GL_VERTEX_SHADER, out nVertShader);
			AttachShader(nProgram, sFragPath, Gl.GL_FRAGMENT_SHADER, out nFragShader);
		}

		public static void DetachShader(int nRunningProgram, int nShader)
		{
			if (Gl.glIsProgram(nRunningProgram) == 0) return;
			else {
				Gl.glDetachShader(nRunningProgram, nShader);
			}
		}

		public static void DetachAndDeleteShader(int nProgram, int nShader)
		{
			if (Gl.glIsProgram(nProgram) == 0) return;
			else
			{
				Gl.glDetachShader(nProgram, nShader);
				Gl.glDeleteShader(nShader);
			}
		}

		public static int CreateProgram()
		{
			return Gl.glCreateProgram();
		}

		public static int CreateProgram(string sPath, int nShaderType)
		{
			int nProgram = CreateProgram();

			int nShader;
			AttachShader(nProgram, sPath, nShaderType, out nShader);
			return nProgram;
		}

		public static int CreateProgram(string sPathVert, string sPathFrag)
		{
			int nProgram = CreateProgram();

			int nVertShader, nFragShader;
			AttachShaders(nProgram, sPathVert, sPathFrag, out nVertShader, out nFragShader);
			return nProgram;
		}

		public static int CreateProgram(string sPath, int nShaderType, out int nShader)
		{
			int nProgram = CreateProgram();

			AttachShader(nProgram, sPath, nShaderType, out nShader);
			return nProgram;
		}

		public static int CreateProgram(string sPathVert, string sPathFrag, out int nVertShader, out int nFragShader)
		{
			int nProgram = CreateProgram();

			AttachShaders(nProgram, sPathVert, sPathFrag, out nVertShader, out nFragShader);
			return nProgram;
		}

		public static int printOpenGLError()
		{
			int glErr;
			int retCode = 0;

			glErr = Gl.glGetError();
			while (glErr != Gl.GL_NO_ERROR)
			{
				Console.WriteLine("glError : " + Glu.gluErrorString(glErr));
				retCode = 1;
				glErr = Gl.glGetError();
			}
			return retCode;
		}

		public static void CloseProgram(int nProgram)
		{
			int nMaxAttached = 100;
			int nCount;
			int[] nShaders = new int[nMaxAttached];

			if (Gl.glIsProgram(nProgram) == 1)
			{
				Gl.glGetAttachedShaders(nProgram, nMaxAttached, out nCount, nShaders);
				for (int i = 0; i < nCount; i++)
				{
					Gl.glDetachShader(nProgram, nShaders[i]);
					Gl.glDeleteShader(nShaders[i]);
				}
				Gl.glDeleteProgram(nProgram);
				Gl.glUseProgram(0);
			}
		}

		public static void printShaderInfoLog(int nShader)
		{
			int infologLength;
			int charsWritten;

			Gl.glGetShaderiv(nShader, Gl.GL_INFO_LOG_LENGTH, out infologLength);

			if (infologLength > 0)
			{
				System.Text.StringBuilder infoLog = new System.Text.StringBuilder();
				Gl.glGetShaderInfoLog(nShader, infologLength, out charsWritten, infoLog);
				Console.WriteLine("Shader InfoLog:\n" + infoLog);
			}
		}

		public static void printProgramInfoLog(int nProgram)
		{
			int infologLength;
			int charsWritten;

			Gl.glGetProgramiv(nProgram, Gl.GL_INFO_LOG_LENGTH, out infologLength);

			if (infologLength > 0)
			{
				System.Text.StringBuilder infoLog = new System.Text.StringBuilder();
				Gl.glGetProgramInfoLog(nProgram, infologLength, out charsWritten, infoLog);
				Console.WriteLine("Program InfoLog:\n" + infoLog);
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
