using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;

namespace DominusCore
{
	/// <summary> Storage format for non-animated models. This does not preserve vertex data in CPU memory after upload. 
	/// <br/> Provides methods to bind, draw, and dispose.</summary>
	struct Drawable : IDisposable
	{
		private readonly int IndexLength;
		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		private readonly Texture[] textures;

		/// <summary> Creates a drawable object with a given set of vertex data ([xyz][uv][rgba][qrs]).
		/// <br/>This represents a single model with specified data. It may be rendered many times with different uniforms,
		/// but the vertex data will remain static. Usage is hinted as StaticDraw. Both index and vertex data is
		/// discarded immediately after being sent to the GL context.
		/// <br/> !! Warning !! This is not a logical unit and exists on the render thread only! </summary>
		public Drawable(float[] VertexData, uint[] Indices, Texture[] textures)
		{
			IndexLength = Indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, VertexData.Length * sizeof(float), VertexData, BufferUsageHint.StaticDraw);

			this.textures = textures;
		}

		/// <summary> Binds both the vertex and index data for subsequent drawing.
		/// <br/> !! Warning !! This may be performance heavy with large amounts of different models! </summary>
		public void Bind()
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			for (int i = 0; i < textures.Length; i++)
				textures[i].Bind(i);
		}

		public void Draw()
		{
			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
		}

		public void BindAndDraw()
		{
			Bind();
			Draw();
		}

		/// <summary> Deletes buffers in OpenGL. This is automatically done by object garbage collection OR program close.
		/// <br/> !! Warning !! Avoid using this unless you know what you're doing! This can crash! </summary>
		public void Dispose()
		{
			GL.DeleteBuffer(VertexBufferObject_ID);
			GL.DeleteBuffer(ElementBufferArray_ID);
		}
	}

	struct ShaderProgram
	{
		public readonly int ShaderProgram_ID;
		public readonly int UniformModelView_ID;
		public readonly int UniformPerspective_ID;

		/// <summary> Creates and uses a new shader program using provided shader IDs to attach. <br/>
		/// Use ShaderProgram.CreateShader(...) to get these IDs.</summary>
		public ShaderProgram(int[] shaders)
		{
			ShaderProgram_ID = GL.CreateProgram();
			foreach (int i in shaders)
				GL.AttachShader(ShaderProgram_ID, i);
			GL.LinkProgram(ShaderProgram_ID);
			GL.UseProgram(ShaderProgram_ID);
			foreach (int i in shaders)
				GL.DeleteShader(i);

			UniformModelView_ID = GL.GetUniformLocation(ShaderProgram_ID, "modelView");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
		}

		/// <summary> Creates, loads, and compiles a shader given a file. Handles errors in the shader. Returns the shader ID. </summary>
		public static int CreateShader(string source, ShaderType type)
		{
			Console.WriteLine($"Create shader: \"{source}\"");
			int shader = GL.CreateShader(type);
			GL.ShaderSource(shader, new StreamReader(source).ReadToEnd());
			GL.CompileShader(shader);
			if (GL.GetShaderInfoLog(shader) != System.String.Empty)
				throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shader)}");
			return shader;
		}

		public ShaderProgram use()
		{
			GL.UseProgram(ShaderProgram_ID);
			return this;
		}
	}

	public class Game : GameWindow
	{
		/* Constants */
		public static readonly string WindowTitle = "Display";
		public static readonly double RenderUpdatePerSecond = 60.0;
		public static readonly double LogicUpdatePerSecond = 60.0;
		public static readonly Vector2i WindowSize = new Vector2i(1280, 720);
		/// <summary> Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180</summary>
		const float RCF = 0.017453293f;
		/// <summary> Determines if the program will exit on frame 11 (used for RenderDoc) </summary>
		private static bool autoExit = false;

		/* Rendering */
		private ShaderProgram GeometryShader;
		private ShaderProgram LightingShader;
		private static Vector3 CameraPosition = new Vector3(0.0f, 0.0f, -1.0f);
		/// <summary> Position relative to the camera that the camera is facing. Managed in OnUpdateFrame(). </summary>
		private static Vector3 CameraTarget = -Vector3.UnitZ;
		private float CameraAngle = 90;
		private static Matrix4 MatrixPerspective = Matrix4.CreatePerspectiveFieldOfView(90f * RCF, WindowSize.X / WindowSize.Y, 0.001f, 100.0f);

		/* Counters */
		private int RenderFrameCount = 0;
		private int LogicFrameCount = 0;

		/* Debugging */
		private Drawable DrawableTest;
		private static DebugProc debugCallback = DebugCallback;
		private static GCHandle debugCallbackHandle;

		public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		/// <summary> Handles all graphics setup processing: creates shader program, drawables, sets flags, sets attribs.
		/// <br/> THREAD: OpenGL </summary>
		protected override void OnRenderThreadStarted()
		{
			Console.WriteLine("OnRenderThreadStarted(): start");

			// Misc GL flags and callbacks
			debugCallbackHandle = GCHandle.Alloc(debugCallback);
			GL.DebugMessageCallback(debugCallback, IntPtr.Zero);
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);
			GL.Enable(EnableCap.DepthTest);

			// Geometry shader starts here
			GeometryShader = new ShaderProgram(new int[] {
				ShaderProgram.CreateShader("src/vertex.glsl", ShaderType.VertexShader),
				ShaderProgram.CreateShader("src/fragment.glsl", ShaderType.FragmentShader)
			}).use();

			DrawableTest = new Drawable(new float[]{
				0.5f,  0.5f, 0.0f,   1.0f, 1.0f,   1.0f, 0.0f, 0.0f, 1.0f,
				0.5f, -0.5f, 0.0f,   1.0f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,
				-0.5f, -0.5f, 0.0f,  0.0f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,
				-0.5f,  0.5f, 0.0f,  0.0f, 1.0f,   1.0f, 0.0f, 0.0f, 1.0f,
			}, new uint[]{
				0, 1, 3,
				1, 2, 3
			}, new Texture[] {
				new Texture("assets/tiles_diffuse.jpg", GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_diffuse")),
				new Texture("assets/tiles_gloss.jpg",   GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_gloss")),
				new Texture("assets/tiles_ao.jpg",      GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_ao")),
				new Texture("assets/tiles_normal.jpg",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_normal")),
				new Texture("assets/tiles_height.jpg",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_height"))
			});

			// Lighting shader starts here
			LightingShader = new ShaderProgram(new int[] {
				ShaderProgram.CreateShader("src/vertex_light.glsl", ShaderType.VertexShader),
				ShaderProgram.CreateShader("src/fragment_light.glsl", ShaderType.FragmentShader)
			}).use();

			FramebufferGeometry = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferGeometry);
			FramebufferTextures = new FramebufferTexture[] {
				new FramebufferTexture(0, GL.GetUniformLocation(LightingShader.ShaderProgram_ID, "gPosition")),
				new FramebufferTexture(1, GL.GetUniformLocation(LightingShader.ShaderProgram_ID, "gNormal")),
				new FramebufferTexture(2, GL.GetUniformLocation(LightingShader.ShaderProgram_ID, "gAlbedoSpec")),
			};
			DrawBuffersEnum[] attachments = new DrawBuffersEnum[FramebufferTextures.Length];
			for (int i = 0; i < attachments.Length; i++)
				attachments[i] = DrawBuffersEnum.ColorAttachment0 + i;
			GL.DrawBuffers(attachments.Length, attachments);

			int VertexArrayObject_ID = GL.GenVertexArray();
			GL.BindVertexArray(VertexArrayObject_ID);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);
			// Format: [xyz][uv][rgba]. Make sure to keep synced with how data is interleaved in vertex data!
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0 * sizeof(float)); /* xyz          */
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float)); /* uv           */
			GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), 5 * sizeof(float)); /* rgba         */
			Uniform_CameraPosition = GL.GetUniformLocation(LightingShader.ShaderProgram_ID, "cameraPosition");
			Uniform_Lights = GL.GetUniformLocation(LightingShader.ShaderProgram_ID, "lights");
			Console.WriteLine("OnRenderThreadStarted(): end");
		}

		int FramebufferGeometry;
		FramebufferTexture[] FramebufferTextures;
		int Uniform_CameraPosition;
		int Uniform_Lights;

		/// <summary> Core render loop. <br/> THREAD: OpenGL </summary>
		protected override void OnRenderFrame(FrameEventArgs args)
		{
			if (autoExit & RenderFrameCount == 10)
				Environment.Exit(0);
			RenderFrameCount++;

			// Matrices required for DrawableTest
			Matrix4 matrixModel = Matrix4.Identity;
			matrixModel *= Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f);
			matrixModel *= Matrix4.CreateScale(0.5f, 0.5f, 0.5f);
			matrixModel *= Matrix4.CreateRotationY(RenderFrameCount * RCF);
			Matrix4 modelViewTransform = matrixModel * Matrix4.LookAt(CameraPosition, CameraPosition + CameraTarget, Vector3.UnitY);

			// Geometry pass
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferGeometry);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GeometryShader.use();
			GL.UniformMatrix4(GeometryShader.UniformModelView_ID, true, ref modelViewTransform);
			GL.UniformMatrix4(GeometryShader.UniformPerspective_ID, true, ref MatrixPerspective);
			DrawableTest.BindAndDraw();

			// Lighting pass
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			LightingShader.use();
			GL.Uniform3(Uniform_CameraPosition, CameraPosition.X, CameraPosition.Y, CameraPosition.Z);
			for (int i = 0; i < FramebufferTextures.Length; i++)
				FramebufferTextures[i].Bind(i);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

			Context.SwapBuffers();
		}

		/// <summary> Handles all logical game events and input. <br/> THREAD: Logic </summary>
		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			LogicFrameCount++;
			// All of the following are in the set [-1, 0, 1] which is used to calculate movement.
			int ws = Convert.ToInt32(KeyboardState.IsKeyDown(Keys.W)) - Convert.ToInt32(KeyboardState.IsKeyDown(Keys.S));
			int ad = Convert.ToInt32(KeyboardState.IsKeyDown(Keys.A)) - Convert.ToInt32(KeyboardState.IsKeyDown(Keys.D));
			int qe = Convert.ToInt32(KeyboardState.IsKeyDown(Keys.Q)) - Convert.ToInt32(KeyboardState.IsKeyDown(Keys.E));
			int sl = Convert.ToInt32(KeyboardState.IsKeyDown(Keys.Space)) - Convert.ToInt32(KeyboardState.IsKeyDown(Keys.LeftShift));
			CameraPosition += 0.05f // speed
							* ((CameraTarget * ws) // Forward-back
							+ (Vector3.UnitY * sl) // Up-down
							+ (ad * Vector3.Cross(Vector3.UnitY, CameraTarget))); // Strafing
			CameraAngle -= qe * 1f; // qe * speed
			CameraTarget = new Vector3((float)Math.Cos(CameraAngle * RCF), CameraTarget.Y, (float)Math.Sin(CameraAngle * RCF));
		}


		/// <summary> Handles resizing and keeping GLViewport correct</summary>
		protected override void OnResize(ResizeEventArgs e)
		{
			GL.Viewport(0, 0, WindowSize.X, WindowSize.Y);
		}

		/// <summary> Handles all debug callbacks from OpenGL and throws exceptions if unhandled. </summary>
		private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			string messageString = Marshal.PtrToStringAnsi(message, length);
			Console.WriteLine($"{severity} {type} | {messageString}");
			if (type == DebugType.DebugTypeError)
				throw new Exception(messageString);
		}

		public static void Main(string[] args)
		{
			if (args.Length != 0)
				autoExit = true;
			Console.WriteLine("Initializing");
			GameWindowSettings gws = new GameWindowSettings();
			gws.IsMultiThreaded = true;
			gws.RenderFrequency = RenderUpdatePerSecond;
			gws.UpdateFrequency = LogicUpdatePerSecond;

			NativeWindowSettings nws = new NativeWindowSettings();
			nws.Size = WindowSize;
			nws.Title = WindowTitle;

			Console.WriteLine("Creating game object");
			using (Game g = new Game(gws, nws))
			{
				g.Run();
			}
		}
	}
}
