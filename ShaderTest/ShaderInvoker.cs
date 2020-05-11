using System;
using Tao.OpenGl;
using System.IO;

namespace ShaderTest
{
	public class ShaderInvoker
	{
		public static void ReadShaderSource(ref string sVert, ref string sFrag)
		{
			string sVertPath = "..\\..\\..\\basic.vert";
			string sFragPath = "..\\..\\..\\basic.frag";

			StreamReader sr = new StreamReader(sVertPath);
			sVert = sr.ReadToEnd();
			sr.Close();

			sr = new StreamReader(sFragPath);
			sFrag = sr.ReadToEnd();
			sr.Close();

			Console.WriteLine("Done reading shaders");
		}

		public static int InstallShaders(string sVert, string sFrag, ref int nVertShader, ref int nFragShader)
		{
			int basicProg;   // handles to objects
			int vertCompiled, fragCompiled;    // status values
			int linked;

			// allocate
			nVertShader = Gl.glCreateShader(Gl.GL_VERTEX_SHADER);
			nFragShader = Gl.glCreateShader(Gl.GL_FRAGMENT_SHADER);

			// source
			string[] aBV = { sVert };
			string[] aBF = { sFrag };
			Gl.glShaderSource(nVertShader, 1, aBV, null);
			Gl.glShaderSource(nFragShader, 1, aBF, null);

			// compile
			Gl.glCompileShader(nVertShader);
			printOpenGLError();
			Gl.glGetShaderiv(nVertShader, Gl.GL_COMPILE_STATUS, out vertCompiled);
			printShaderInfoLog(nVertShader);

			Gl.glCompileShader(nFragShader);
			printOpenGLError();
			Gl.glGetShaderiv(nFragShader, Gl.GL_COMPILE_STATUS, out fragCompiled);
			printShaderInfoLog(nFragShader);

			// program
			basicProg = Gl.glCreateProgram();
			Gl.glAttachShader(basicProg, nVertShader);
			Gl.glAttachShader(basicProg, nFragShader);
			Gl.glLinkProgram(basicProg);
			printOpenGLError();
			Gl.glGetProgramiv(basicProg, Gl.GL_LINK_STATUS, out linked);
			printProgramInfoLog(basicProg);
			Gl.glUseProgram(basicProg);

			Console.WriteLine("Done installing shaders");

			return basicProg;
		}

		public static int printOpenGLError()
		{
			//
			// Returns 1 if an OpenGL error occurred, 0 otherwise.
			//
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

		//
		// Print out the information log for a shader object
		//
		static void printShaderInfoLog(int shader)
		{
			int infologLength = 0;
			int charsWritten = 0;
			System.Text.StringBuilder infoLog = new System.Text.StringBuilder();

			printOpenGLError();  // Check for OpenGL errors

			Gl.glGetShaderiv(shader, Gl.GL_INFO_LOG_LENGTH, out infologLength);

			printOpenGLError();  // Check for OpenGL errors

			if (infologLength > 0)
			{
				Gl.glGetShaderInfoLog(shader, infologLength, out charsWritten, infoLog);
				Console.WriteLine("Shader InfoLog:\n" + infoLog + "\n");
			}
			printOpenGLError();  // Check for OpenGL errors
		}

		//
		// Print out the information log for a program object
		//
		static void printProgramInfoLog(int program)
		{
			int infologLength = 0;
			int charsWritten = 0;
			System.Text.StringBuilder infoLog = new System.Text.StringBuilder();

			printOpenGLError();  // Check for OpenGL errors

			Gl.glGetProgramiv(program, Gl.GL_INFO_LOG_LENGTH, out infologLength);

			printOpenGLError();  // Check for OpenGL errors

			if (infologLength > 0)
			{
				Gl.glGetProgramInfoLog(program, infologLength, out charsWritten, infoLog);
				Console.WriteLine("OpenGLBrickShader InfoLog:\n" + infoLog + "\n");
			}
			printOpenGLError();  // Check for OpenGL errors
		}

		public static void CloseProgram(int nProgram)
		{
			int nMaxAttached = 100;
			int nCount;
			int[] nShaders = new int[nMaxAttached];

			if(Gl.glIsProgram(nProgram) == 1) {
				Gl.glGetAttachedShaders(nProgram, nMaxAttached, out nCount, nShaders);
				for(int i = 0; i < nCount; i++) {
					Gl.glDetachShader(nProgram, nShaders[i]);
					Gl.glDeleteShader(nShaders[i]);
				}
				Gl.glDeleteProgram(nProgram);
				Gl.glUseProgram(0);
			}
		}
	}
}
