using OpenTK.Graphics.OpenGL;

namespace utilities
{
	public class Constructs
	{
		public static void DrawAxis(bool bGlut)
		{
			if (bGlut)
			{
				Edge x = new Edge(new D3Vect(0, 0, 0), new D3Vect(200, 0, 0));
				Edge y = new Edge(new D3Vect(0, 0, 0), new D3Vect(0, 200, 0));
				Edge z = new Edge(new D3Vect(0, 0, 0), new D3Vect(0, 0, 200));
				x.Draw(new Color(255, 0, 0), true);
				y.Draw(new Color(0, 255, 0), true);
				z.Draw(new Color(0, 0, 255), true);
			}
			else
			{
				//x-axis
				GL.Begin(PrimitiveType.LineLoop);
				GL.Color3(1.0, 0.0, 0.0);
				GL.Vertex3(0.0, 0.0, 0.0);
				GL.Vertex3(200.0, 0.0, 0.0);
				GL.End();

				//y-axis
				GL.Begin(PrimitiveType.LineLoop);
				GL.Color3(0.0, 1.0, 0.0);
				GL.Vertex3(0.0, 0.0, 0.0);
				GL.Vertex3(0.0, 200.0, 0.0);
				GL.End();

				//z-axis
				GL.Begin(PrimitiveType.LineLoop);
				GL.Color3(0.0, 0.0, 1.0);
				GL.Vertex3(0.0, 0.0, 0.0);
				GL.Vertex3(0.0, 0.0, 200.0);
				GL.End();
			}
		}
	}
}
