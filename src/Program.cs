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
	public class Game : GameWindow
	{
		/* Constants */
		public static readonly string WindowTitle = "Display";
		public static readonly double RenderUpdatePerSecond = 60.0;
		public static readonly double LogicUpdatePerSecond = 60.0;
		public static readonly Vector2i WindowSize = new Vector2i(1280, 720);
		/// <summary> Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180</summary>
		internal const float RCF = 0.017453293f;
		/// <summary> Determines if the program will exit on frame 11 (used for RenderDoc) </summary>
		private static bool autoExit = false;

		/* Rendering */
		private ShaderProgramGeometry GeometryShader;
		private ShaderProgramLighting LightingShader;
		private float CameraAngle = 90;
		private static Matrix4 MatrixPerspective;
		private int FramebufferGeometry;
		private Texture[] FramebufferTextures;

		/* Camera */
		private static Vector3 CameraPosition = new Vector3(20.0f, 5.0f, 3.0f);
		/// <summary> Position relative to the camera that the camera is facing. Managed in OnUpdateFrame(). </summary>
		private static Vector3 CameraTarget = -Vector3.UnitZ;

		/* Counters */
		private int RenderFrameCount = 0;
		private int LogicFrameCount = 0;

		/* Debugging */
		private Drawable[] Scene;
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
			GeometryShader = new ShaderProgramGeometry(ShaderProgram.CreateShaderFromUnified("src/GeometryShader.glsl")).use();

			Drawable circle = Drawable.CreateCircle(90, new Texture[] {
					new Texture("assets/tiles_diffuse.jpg", GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_diffuse")),
					new Texture("assets/tiles_gloss.jpg",   GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_gloss")),
					new Texture("assets/tiles_ao.jpg",      GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_ao")),
					new Texture("assets/tiles_normal.jpg",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_normal")),
					new Texture("assets/tiles_height.jpg",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_height"))
				}).SetPosition(new Vector3(20, 4, 5)).SetScale(1.0f);
			Drawable plane = Drawable.CreateDrawablePlane(new Texture[] {
					new Texture("assets/tiles_diffuse.jpg", GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_diffuse")),
					new Texture("assets/tiles_gloss.jpg",   GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_gloss")),
					new Texture("assets/tiles_ao.jpg",      GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_ao")),
					new Texture("assets/tiles_normal.jpg",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_normal")),
					new Texture("assets/tiles_height.jpg",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_height"))
				}).SetPosition(new Vector3(20, 2, 10)).SetScale(10.0f);
			Drawable cube = Drawable.CreateDrawableCube(new Texture[] {
					new Texture("assets/tiles_diffuse.jpg", GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_diffuse")),
					new Texture("assets/tiles_gloss.jpg",   GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_gloss")),
					new Texture("assets/tiles_ao.jpg",      GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_ao")),
					new Texture("assets/tiles_normal.jpg",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_normal")),
					new Texture("assets/tiles_height.jpg",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_height"))
				}).SetPosition(new Vector3(20, 4, 5)).SetScale(new Vector3(0.25f, 0.25f, 0.25f));
			Drawable lightCube = Drawable.CreateDrawableCube(new Texture[] {
					new Texture("assets/tiles_blank.png",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_diffuse")),
					new Texture("assets/tiles_blank.png",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_gloss")),
					new Texture("assets/tiles_blank.png",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_ao")),
					new Texture("assets/tiles_blank.png",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_normal")),
					new Texture("assets/tiles_blank.png",  GL.GetUniformLocation(GeometryShader.ShaderProgram_ID, "map_height"))
				}).SetPosition(new Vector3(20, 5, 6)).SetScale(0.25f);
			Scene = new Drawable[] {
				plane, lightCube, circle
			};

			// Lighting shader starts here
			LightingShader = new ShaderProgramLighting(ShaderProgram.CreateShaderFromUnified("src/LightingShader.glsl")).use();

			FramebufferGeometry = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferGeometry);
			FramebufferTextures = new Texture[] {
				new Texture(0, GL.GetUniformLocation(LightingShader.ShaderProgram_ID, "gPosition")),
				new Texture(1, GL.GetUniformLocation(LightingShader.ShaderProgram_ID, "gNormal")),
				new Texture(2, GL.GetUniformLocation(LightingShader.ShaderProgram_ID, "gAlbedoSpec")),
			};
			int depth = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, depth);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32f, Game.WindowSize.X, Game.WindowSize.Y,
						  0, PixelFormat.DepthComponent, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
									TextureTarget.Texture2D, depth, 0);

			DrawBuffersEnum[] attachments = new DrawBuffersEnum[FramebufferTextures.Length];
			for (int i = 0; i < attachments.Length; i++)
				attachments[i] = DrawBuffersEnum.ColorAttachment0 + i;
			GL.DrawBuffers(attachments.Length, attachments);

			int VertexArrayObject_ID = GL.GenVertexArray();
			GL.BindVertexArray(VertexArrayObject_ID);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);
			GL.EnableVertexAttribArray(3);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12 * sizeof(float), 0 * sizeof(float)); /* xyz  */
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 12 * sizeof(float), 3 * sizeof(float)); /* uv   */
			GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 12 * sizeof(float), 5 * sizeof(float)); /* rgba */
			GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 12 * sizeof(float), 9 * sizeof(float)); /* normal */

			Console.WriteLine("OnRenderThreadStarted(): end");
		}

		/// <summary> Core render loop. <br/> THREAD: OpenGL </summary>
		protected override void OnRenderFrame(FrameEventArgs args)
		{
			if (autoExit & RenderFrameCount == 10)
				Environment.Exit(0);
			RenderFrameCount++;

			Matrix4 MatrixView = Matrix4.LookAt(CameraPosition, CameraPosition + CameraTarget, Vector3.UnitY);

			// Geometry pass
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferGeometry);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GeometryShader.use();
			GL.UniformMatrix4(GeometryShader.UniformView_ID, true, ref MatrixView);
			GL.UniformMatrix4(GeometryShader.UniformPerspective_ID, true, ref MatrixPerspective);
			foreach (Drawable d in Scene)
			{
				Matrix4 MatrixModel = d.GetModelMatrix();
				GL.UniformMatrix4(GeometryShader.UniformModel_ID, true, ref MatrixModel);
				d.BindAndDraw();
			}

			// Lighting pass
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			LightingShader.use();
			GL.Uniform3(LightingShader.UniformCameraPosition_ID, CameraPosition.X, CameraPosition.Y, CameraPosition.Z);
			for (int i = 0; i < FramebufferTextures.Length; i++)
				FramebufferTextures[i].Bind(i);

			LightingShader.setLightUniform(0, 5f,
										new Vector3(20f, 5f + (float)(8 * Math.Sin(RenderFrameCount * RCF)), 6f),
										Vector3.One, Vector3.Zero);
			LightingShader.setLightUniform(1, 2.0f,
										new Vector3(20f + (float)(5 * Math.Sin(RenderFrameCount * RCF)), 0f, 6f),
										new Vector3(0.0f, 0.5f, 1.0f), Vector3.Zero);
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
			MatrixPerspective = Matrix4.CreatePerspectiveFieldOfView(90f * RCF, (float)WindowSize.X / (float)WindowSize.Y, 0.001f, 100.0f);
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
			nws.WindowBorder = WindowBorder.Fixed;

			Console.WriteLine("Creating game object");
			using (Game g = new Game(gws, nws))
			{
				g.Run();
			}
		}
	}
}
