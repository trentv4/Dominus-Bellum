using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

// Resources
// https://opentk.net/learn/chapter1/2-hello-triangle.html#linking-vertex-attributes
// http://www.opengl-tutorial.org/beginners-tutorials/tutorial-8-basic-shading/
// https://github.com/opentk/LearnOpenTK/blob/master/Chapter1/8-Camera/Window.cs

namespace DominusCore
{
	struct Drawable : IDisposable
	{
		private readonly int IndexLength;
		// OpenGL ID for the index buffer
		public readonly int ElementBufferArray;
		// OpenGL ID for the vertex data 
		public readonly int VertexBufferObject;

		// Creates a drawable object with a given set of vertex data ([xyz][uv][rgba][qrs])
		// This represents a single model with specified data. It may be rendered many times with different uniforms,
		// but the vertex data will remain static. Usage is hinted as StaticDraw. Both index and vertex data is
		// discarded immediately after being sent to the GL context.
		// !! Warning !! This is not a logical unit and exists on the render thread only!
		public Drawable(float[] VertexData, uint[] Indices)
		{
			IndexLength = Indices.Length;

			ElementBufferArray = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.StaticDraw);

			VertexBufferObject = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
			GL.BufferData(BufferTarget.ArrayBuffer, VertexData.Length * sizeof(float), VertexData, BufferUsageHint.StaticDraw);
		}

		// Binds both the vertex and index data for subsequent drawing
		// !! Warning !! This may be performance heavy with large amounts of different models!
		public void Bind()
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
		}

		public void Draw()
		{
			GL.DrawElements(PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
		}

		// Deletes buffers in OpenGL. This is automatically done by object garbage collection OR program close
		// !! Warning !! Avoid using this unless you know what you're doing! This can crash!
		public void Dispose()
		{
			GL.DeleteBuffer(VertexBufferObject);
			GL.DeleteBuffer(ElementBufferArray);
		}
	}

	struct ShaderProgram
	{
		// OpenGL ID for the shader program
		public readonly int ProgramID;
		// OpenGL ID for the uniform mat4 mvp
		public readonly int UniformMVPID;

		// Creates and uses a new shader program using provided shader IDs to attach
		// Use CreateShader(...) to get these IDs
		public ShaderProgram(int[] shaders)
		{
			ProgramID = GL.CreateProgram();
			foreach (int i in shaders)
				GL.AttachShader(ProgramID, i);
			GL.LinkProgram(ProgramID);
			GL.UseProgram(ProgramID);
			UniformMVPID = GL.GetUniformLocation(ProgramID, "mvp");
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
		public static readonly string TITLE = "Display";
		public static readonly double RENDER_UPDATE_PER_SECOND = 60.0;
		public static readonly double LOGIC_UPDATE_PER_SECOND = 60.0;
		public static readonly Vector2i WINDOW_SIZE = new Vector2i(1280, 720);
		const float RCF = 0.017453293f; // Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180
		/* Rendering */
		private int VertexArrayObject = -1;
		private ShaderProgram Program;
		private Drawable TestSquare;
		float rotation = 0f;
		static Vector3 CameraPosition = new Vector3(0.0f, 0.0f, 0.0f);
		static Vector3 CameraTarget = Vector3.UnitZ; // Relative to CameraPosition, managed in OnRenderFrame()

		public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		// Only run once. Creates the shader program, Drawable objects, sets OpenGL flags, sets shader attribs
		// Thread: render
		protected override void OnRenderThreadStarted()
		{
			Console.WriteLine("OnRenderThreadStarted(): start");

			GL.ClearColor(0.2f, 0.2f, 0.3f, 1.0f);
			GL.Viewport(0, 0, WINDOW_SIZE.X, WINDOW_SIZE.Y);
			GL.Enable(EnableCap.DepthTest);

			Program = new ShaderProgram(new int[] {
				ShaderProgram.CreateShader("src/vertex.glsl", ShaderType.VertexShader),
				ShaderProgram.CreateShader("src/fragment.glsl", ShaderType.FragmentShader)
			});

			TestSquare = new Drawable(new float[]{
				0.5f,  0.5f, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f,// top right
				0.5f, -0.5f, 0.0f, 1.0f, 0.0f, 1.0f, 0.5f, 0.0f, 1.0f,// bottom right
				-0.5f, -0.5f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, 1.0f,// bottom left
				-0.5f,  0.5f, 0.0f, 0.0f, 1.0f,  0.0f, 0.5f, 1.0f, 1.0f,// top left
			}, new uint[]{
				0, 1, 3,
				1, 2, 3
			});

			VertexArrayObject = GL.GenVertexArray();
			GL.BindVertexArray(VertexArrayObject);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);
			//GL.EnableVertexAttribArray(3);

			// Format: [xyz][uv][rgba][qrs]
			// Make sure to keep synced with how data is interleaved in vertex data!
			int stride = 9 * sizeof(float);
			// AttribPointer: (location, vector size, type, normalize data, stride, offset in bytes)
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0 * sizeof(float)); /* xyz          */
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float)); /* uv           */
			GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float)); /* rgba         */
			//GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, stride, 0 * sizeof(float)); /* qrs (normal) */

			Console.WriteLine("OnRenderThreadStarted(): end");
		}

		// Handles per-frame rendering cycle. Runs every single frame.
		// Thread: render
		protected override void OnRenderFrame(FrameEventArgs args)
		{
			Console.Write("OnRenderFrame(): start - ");
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			rotation += 1f;
			Matrix4 model = Matrix4.CreateRotationZ(rotation * RCF)
						  * Matrix4.CreateScale(0.25f, 0.25f, 0.25f)
						  * Matrix4.CreateTranslation(0, 0f, rotation * 0.01f);
			Matrix4 view = Matrix4.LookAt(CameraPosition, CameraPosition + CameraTarget, Vector3.UnitY);
			Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(45f * RCF, WINDOW_SIZE.X / WINDOW_SIZE.Y, 0.001f, 100.0f);

			Matrix4 mvpTransform = view * model;
			GL.UniformMatrix4(Program.UniformMVPID, true, ref mvpTransform);
			TestSquare.Bind();
			TestSquare.Draw();

			Context.SwapBuffers();
			base.OnRenderFrame(args);
			Console.WriteLine("end");
		}

		// Handles game logic, input, anything besides rendering
		// Thread: logic
		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			float speed = 0.1f;

			if (KeyboardState.IsKeyDown(Keys.W))
				CameraPosition += Vector3.UnitY * speed;
			if (KeyboardState.IsKeyDown(Keys.S))
				CameraPosition -= Vector3.UnitY * speed;
			if (KeyboardState.IsKeyDown(Keys.A))
				CameraPosition += Vector3.UnitX * speed;
			if (KeyboardState.IsKeyDown(Keys.D))
				CameraPosition -= Vector3.UnitX * speed;
			if (KeyboardState.IsKeyDown(Keys.Space))
				CameraPosition += Vector3.UnitZ * speed;
			if (KeyboardState.IsKeyDown(Keys.LeftShift))
				CameraPosition -= Vector3.UnitZ * speed;

			base.OnUpdateFrame(args);
		}

		protected override void OnResize(ResizeEventArgs e)
		{
			GL.Viewport(0, 0, WINDOW_SIZE.X, WINDOW_SIZE.Y);
			base.OnResize(e);
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
