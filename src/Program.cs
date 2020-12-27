using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

//https://opentk.net/learn/chapter1/2-hello-triangle.html#linking-vertex-attributes
//http://www.opengl-tutorial.org/beginners-tutorials/tutorial-8-basic-shading/

namespace DominusCore
{
	struct Drawable
	{
		public readonly int IndexLength;
		public int ElementBufferArray;
		public int VertexBufferObject;

		public Drawable(float[] Vertices, uint[] Indices)
		{
			IndexLength = Indices.Length;

			ElementBufferArray = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.StaticDraw);

			VertexBufferObject = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
			GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.StaticDraw);
		}

		public void Bind()
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
		}

		public void Draw()
		{
			GL.DrawElements(PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
		}
	}

	struct ShaderProgram
	{
		public readonly int ProgramID;
		public readonly int UniformMVPID;

		public ShaderProgram(int[] shaders)
		{
			ProgramID = GL.CreateProgram();
			foreach (int i in shaders)
				GL.AttachShader(ProgramID, i);
			GL.LinkProgram(ProgramID);
			GL.UseProgram(ProgramID);
			UniformMVPID = GL.GetUniformLocation(ProgramID, "mvp");
		}

		public static int CreateShader(string source, ShaderType type)
		{
			Console.WriteLine($"Create shader: \"{source}\"");
			int shader = GL.CreateShader(type);
			GL.ShaderSource(shader, new StreamReader(source).ReadToEnd());
			GL.CompileShader(shader);
			if (GL.GetShaderInfoLog(shader) != System.String.Empty)
				System.Console.WriteLine($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shader)}");
			return shader;
		}
	}

	public class Game : GameWindow
	{
		const float RCF = 0.017453293f;
		float rotation = 0f;

		public static readonly string TITLE = "Display";
		public static readonly double RENDER_UPDATE_PER_SECOND = 60.0;
		public static readonly double LOGIC_UPDATE_PER_SECOND = 60.0;
		public static readonly Vector2i WINDOW_SIZE = new Vector2i(1280, 720);
		private int VAO = -1;
		ShaderProgram Program;
		Drawable Square;
		Drawable Square2;

		public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		protected override void OnRenderThreadStarted()
		{
			Console.WriteLine("OnRenderThreadStarted(): start");

			GL.ClearColor(0.2f, 0.2f, 0.3f, 1.0f);
			GL.Viewport(0, 0, WINDOW_SIZE.X, WINDOW_SIZE.Y);
			//GL.Enable(EnableCap.DepthTest);

			Program = new ShaderProgram(new int[] {
				ShaderProgram.CreateShader("src/vertex.glsl", ShaderType.VertexShader),
				ShaderProgram.CreateShader("src/fragment.glsl", ShaderType.FragmentShader)
			});
			VAO = GL.GenVertexArray();
			GL.BindVertexArray(VAO);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);
			GL.EnableVertexAttribArray(3);

			//ordering: xyz uv normal-xyz rgba
			Square = new Drawable(new float[]{
				0.5f, 0.5f, 1.0f,    1.0f, 1.0f,   0.0f, 0.0f, 0.0f,   0.8f, 0.2f, 0.4f, 1.0f,
				0.5f, -0.5f, 0.0f,   1.0f, 0.0f,   0.0f, 0.0f, 0.0f,   0.6f, 0.4f, 0.2f, 1.0f,
				-0.5f, 0.5f, 0.0f,   0.0f, 1.0f,   0.0f, 0.0f, 0.0f,   0.4f, 0.6f, 0.8f, 1.0f,
				-0.5f, -0.5f, 0.0f,  0.0f, 0.0f,   0.0f, 0.0f, 0.0f,   0.2f, 0.8f, 0.6f, 1.0f,
			}, new uint[]{
				1, 2, 3,
			});

			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 48, 0);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 48, 3 * sizeof(float));
			GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 48, 5 * sizeof(float));
			GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 48, 8 * sizeof(float));

			Square2 = new Drawable(new float[]{
				0.5f, 0.5f, 0.0f,
				0.5f, -0.5f, 0.0f,
				-0.5f, 0.5f, 0.0f,
				-0.5f, -0.5f, 0.0f,
			}, new uint[]{
				0, 1, 2,
			});

			Console.WriteLine("OnRenderThreadStarted(): end");
		}
		protected override void OnResize(ResizeEventArgs e)
		{
			GL.Viewport(0, 0, WINDOW_SIZE.X, WINDOW_SIZE.Y);
			base.OnResize(e);
		}

		protected override void OnRenderFrame(FrameEventArgs args)
		{
			rotation += 1f;
			Console.Write("OnRenderFrame(): start - ");
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.BindVertexArray(VAO);

			Matrix4 modelTransform = Matrix4.Identity;
			modelTransform *= Matrix4.CreateRotationZ(rotation * RCF);
			modelTransform *= Matrix4.CreateScale(1.0f, 1.0f, 1.0f);
			modelTransform *= Matrix4.CreateTranslation(0, 0, 0);

			Vector3 cameraPos = new Vector3(0, 1, -5);
			Vector3 cameraTarget = Vector3.Zero;
			Matrix4 viewTransform = Matrix4.LookAt(cameraPos, cameraTarget, Vector3.Normalize(cameraPos - cameraTarget));

			Matrix4 projectionTransform = Matrix4.CreatePerspectiveFieldOfView(45 * RCF, (WINDOW_SIZE.X / WINDOW_SIZE.Y), 0.1f, 100f);

			//modelTransform = Matrix4.Identity;
			viewTransform = Matrix4.Identity;
			projectionTransform = Matrix4.Identity;

			Matrix4 mvpTransform = projectionTransform * viewTransform * modelTransform;
			GL.UniformMatrix4(Program.UniformMVPID, true, ref mvpTransform);
			Square.Bind();
			Square.Draw();

			Square2.Bind();
			Square2.Draw();

			Context.SwapBuffers();
			Console.WriteLine("end");
			base.OnRenderFrame(args);
		}

		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			base.OnUpdateFrame(args);
		}

		public static void Main(string[] args)
		{
			Console.WriteLine("Initializing");
			GameWindowSettings gws = new GameWindowSettings();
			gws.IsMultiThreaded = true;
			gws.RenderFrequency = RENDER_UPDATE_PER_SECOND;
			gws.UpdateFrequency = LOGIC_UPDATE_PER_SECOND;

			NativeWindowSettings nws = new NativeWindowSettings();
			nws.Size = WINDOW_SIZE;
			nws.Title = TITLE;

			Console.WriteLine("Creating game object");
			using (Game g = new Game(gws, nws))
			{
				g.Run();
			}
		}
	}
}
