using System;
using System.IO;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using DominusCore;

namespace DominusCore
{
	struct Drawable : IDisposable
	{
		private readonly int IndexLength;
		// OpenGL ID for the index buffer
		public readonly int ElementBufferArray_ID;
		// OpenGL ID for the vertex data 
		public readonly int VertexBufferObject_ID;

		// Creates a drawable object with a given set of vertex data ([xyz][uv][rgba][qrs])
		// This represents a single model with specified data. It may be rendered many times with different uniforms,
		// but the vertex data will remain static. Usage is hinted as StaticDraw. Both index and vertex data is
		// discarded immediately after being sent to the GL context.
		// !! Warning !! This is not a logical unit and exists on the render thread only!
		public Drawable(float[] VertexData, uint[] Indices)
		{
			IndexLength = Indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, VertexData.Length * sizeof(float), VertexData, BufferUsageHint.StaticDraw);
		}

		// Binds both the vertex and index data for subsequent drawing
		// !! Warning !! This may be performance heavy with large amounts of different models!
		public void Bind()
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
		}

		public void Draw()
		{
			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
		}

		// Deletes buffers in OpenGL. This is automatically done by object garbage collection OR program close
		// !! Warning !! Avoid using this unless you know what you're doing! This can crash!
		public void Dispose()
		{
			GL.DeleteBuffer(VertexBufferObject_ID);
			GL.DeleteBuffer(ElementBufferArray_ID);
		}
	}

	struct ShaderProgram
	{
		// OpenGL ID for the shader program
		public readonly int ShaderProgram_ID;
		public readonly int UniformMVP_ID;
		public readonly int UniformMapDiffuse_ID;
		public readonly int UniformMapGloss_ID;
		public readonly int UniformMapAO_ID;
		public readonly int UniformMapNormal_ID;
		public readonly int UniformMapHeight_ID;

		// Creates and uses a new shader program using provided shader IDs to attach
		// Use CreateShader(...) to get these IDs
		public ShaderProgram(int[] shaders, int textureCount)
		{
			ShaderProgram_ID = GL.CreateProgram();
			foreach (int i in shaders)
				GL.AttachShader(ShaderProgram_ID, i);
			GL.LinkProgram(ShaderProgram_ID);
			GL.UseProgram(ShaderProgram_ID);
			foreach (int i in shaders)
				GL.DeleteShader(i);

			UniformMVP_ID = GL.GetUniformLocation(ShaderProgram_ID, "mvp");
			UniformMapDiffuse_ID = GL.GetUniformLocation(ShaderProgram_ID, "map_diffuse");
			UniformMapGloss_ID = GL.GetUniformLocation(ShaderProgram_ID, "map_gloss");
			UniformMapAO_ID = GL.GetUniformLocation(ShaderProgram_ID, "map_ao");
			UniformMapNormal_ID = GL.GetUniformLocation(ShaderProgram_ID, "map_normal");
			UniformMapHeight_ID = GL.GetUniformLocation(ShaderProgram_ID, "map_height");
		}

		// Creates, loads, and compiles a shader given a file. Handles errors in the shader by writing to console
		// Returns the shader ID
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
		/* Constants */
		public static readonly string Title = "Display";
		public static readonly double RenderUpdatePerSecond = 60.0;
		public static readonly double LogicUpdatePerSecond = 60.0;
		public static readonly Vector2i WindowSize = new Vector2i(1280, 720);
		const float RCF = 0.017453293f; // Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180
		/* Rendering */
		private int VertexArrayObject_ID = -1;
		private ShaderProgram Program;
		private static Vector3 CameraPosition = new Vector3(0.0f, 0.0f, -1.0f);
		private static Vector3 CameraTarget = new Vector3(0.0f, 0.0f, -1.0f); // Relative to CameraPosition, managed in OnRenderFrame()
		private float CameraAngle = 90;
		private static Matrix4 MatrixPerspective = Matrix4.CreatePerspectiveFieldOfView(45f * RCF, WindowSize.X / WindowSize.Y, 0.001f, 100.0f);
		/* Counters */
		private int RenderFrameCount = 0;
		private int LogicFrameCount = 0;
		/* Debugging */
		private Drawable DrawableTest;
		private Texture textureTest;
		private Texture textureTest2;

		public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		// Only run once. Creates the shader program, Drawable objects, sets OpenGL flags, sets shader attribs
		// Thread: render
		protected override void OnRenderThreadStarted()
		{
			Console.WriteLine("OnRenderThreadStarted(): start");

			GL.ClearColor(0.2f, 0.2f, 0.3f, 1.0f);
			GL.Viewport(0, 0, WindowSize.X, WindowSize.Y);
			GL.Enable(EnableCap.DepthTest);

			Program = new ShaderProgram(new int[] {
				ShaderProgram.CreateShader("src/vertex.glsl", ShaderType.VertexShader),
				ShaderProgram.CreateShader("src/fragment.glsl", ShaderType.FragmentShader)
			}, 3);

			DrawableTest = new Drawable(new float[]{
				0.5f,  0.5f, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f,
				0.5f, -0.5f, 0.0f, 1.0f, 0.0f, 1.0f, 0.5f, 0.0f,
				-0.5f, -0.5f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f,
				-0.5f,  0.5f, 0.0f, 0.0f, 1.0f,  0.0f, 0.5f, 1.0f,
			}, new uint[]{
				0, 1, 3,
				1, 2, 3
			});

			textureTest = new Texture("assets/tiles_diffuse.jpg", TextureUnit.Texture0, Program.UniformMapDiffuse_ID);
			textureTest2 = new Texture("assets/tiles_ao.jpg", TextureUnit.Texture1, Program.UniformMapAO_ID);

			VertexArrayObject_ID = GL.GenVertexArray();
			GL.BindVertexArray(VertexArrayObject_ID);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);

			// Format: [xyz][uv][rgba]
			// Make sure to keep synced with how data is interleaved in vertex data!
			int stride = 8 * sizeof(float);
			// AttribPointer: (location, vector size, type, normalize data, stride, offset in bytes)
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0 * sizeof(float)); /* xyz          */
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float)); /* uv           */
			GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float)); /* rgba         */

			Console.WriteLine("OnRenderThreadStarted(): end");
		}

		// Handles per-frame rendering cycle. Runs every single frame.
		// Thread: render
		protected override void OnRenderFrame(FrameEventArgs args)
		{
			RenderFrameCount++;
			long start = DateTime.Now.Ticks;
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			Matrix4 matrixModel = Matrix4.Identity;
			matrixModel *= Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f);
			matrixModel *= Matrix4.CreateScale(0.5f, 0.5f, 0.5f);
			matrixModel *= Matrix4.CreateRotationY(RenderFrameCount * RCF);

			Matrix4 matrixView = Matrix4.LookAt(CameraPosition, CameraPosition + CameraTarget, Vector3.UnitY);
			Matrix4 mvpTransform = matrixModel * matrixView * MatrixPerspective;
			GL.UniformMatrix4(Program.UniformMVP_ID, true, ref mvpTransform);

			textureTest.Bind();
			textureTest2.Bind();

			DrawableTest.Bind();
			DrawableTest.Draw();

			Context.SwapBuffers();
			base.OnRenderFrame(args);

			long frameTime = (DateTime.Now.Ticks - start) / 10000;
		}

		// Handles game logic, input, anything besides rendering
		// Thread: logic
		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			LogicFrameCount++;
			float speed = 0.05f;
			float angleSpeed = 1f;

			if (KeyboardState.IsKeyDown(Keys.W))
				CameraPosition += CameraTarget * speed;
			if (KeyboardState.IsKeyDown(Keys.S))
				CameraPosition -= CameraTarget * speed;
			if (KeyboardState.IsKeyDown(Keys.A))
				CameraAngle -= angleSpeed;
			if (KeyboardState.IsKeyDown(Keys.D))
				CameraAngle += angleSpeed;
			if (KeyboardState.IsKeyDown(Keys.Space))
				CameraPosition += Vector3.UnitY * speed;
			if (KeyboardState.IsKeyDown(Keys.LeftShift))
				CameraPosition -= Vector3.UnitY * speed;
			CameraTarget = new Vector3((float)Math.Cos(CameraAngle * RCF), CameraTarget.Y, (float)Math.Sin(CameraAngle * RCF));
			base.OnUpdateFrame(args);
		}

		protected override void OnResize(ResizeEventArgs e)
		{
			GL.Viewport(0, 0, WindowSize.X, WindowSize.Y);
			base.OnResize(e);
		}

		public static void Main(string[] args)
		{
			Console.WriteLine("Initializing");
			GameWindowSettings gws = new GameWindowSettings();
			gws.IsMultiThreaded = true;
			gws.RenderFrequency = RenderUpdatePerSecond;
			gws.UpdateFrequency = LogicUpdatePerSecond;

			NativeWindowSettings nws = new NativeWindowSettings();
			nws.Size = WindowSize;
			nws.Title = Title;

			Console.WriteLine("Creating game object");
			using (Game g = new Game(gws, nws))
			{
				g.Run();
			}
		}
	}
}
