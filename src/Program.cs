using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

namespace DominusCore {
	public class Game : GameWindow {
		// Constants
		public static readonly Vector2i WindowSize = new Vector2i(1280, 720);
		/// <summary> Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180</summary>
		internal const float RCF = 0.017453293f;

		// Rendering
		public static ShaderProgramGeometry GeometryShader;
		public static ShaderProgramLighting LightingShader;
		public static ShaderProgramInterface InterfaceShader;
		public static ShaderProgram CurrentShader;
		private float CameraAngle = 90;
		private static Matrix4 MatrixPerspective;
		private int FramebufferGeometry;
		private Texture[] FramebufferTextures;
		private Drawable SceneRoot;
		private Drawable InterfaceRoot;

		// Camera
		private static Vector3 CameraPosition = new Vector3(20.0f, 2.0f, -3.0f);
		/// <summary> Position relative to the camera that the camera is facing. Managed in OnUpdateFrame(). </summary>
		private static Vector3 CameraTarget = -Vector3.UnitZ;

		// Debugging
		private static DebugProc debugCallback = DebugCallback;
		private static GCHandle debugCallbackHandle;
		private static Stopwatch frameTimer = new Stopwatch();

		public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		/// <summary> Handles all graphics setup processing: creates shader program, drawables, sets flags, sets attribs.
		/// <br/> THREAD: OpenGL </summary>
		protected override void OnRenderThreadStarted() {
			Console.WriteLine("OnRenderThreadStarted(): start");

			// Misc GL flags and callbacks
			debugCallbackHandle = GCHandle.Alloc(debugCallback);
			GL.DebugMessageCallback(debugCallback, IntPtr.Zero);
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);
			GL.Enable(EnableCap.DepthTest);
			VSync = VSyncMode.Off; // On seems to break?

			InterfaceShader = new ShaderProgramInterface(ShaderProgram.CreateShaderFromUnified("src/InterfaceShader.glsl")).use();
			GeometryShader = new ShaderProgramGeometry(ShaderProgram.CreateShaderFromUnified("src/GeometryShader.glsl")).use();
			LightingShader = new ShaderProgramLighting(ShaderProgram.CreateShaderFromUnified("src/LightingShader.glsl")).use();

			// Framebuffer setup for the geometry buffer
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

			SceneRoot = DemoBuilder.BuildDemoScene_TextureTest();
			InterfaceRoot = DemoBuilder.BuildDemoInterface_IngameTest();

			// Interface VAO setup
			GL.BindVertexArray(InterfaceShader.VertexArrayObject_ID);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0 * sizeof(float)); /* xy */
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float)); /* uv */

			// Geometry VAO setup
			GL.BindVertexArray(GeometryShader.VertexArrayObject_ID);
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
		protected override void OnRenderFrame(FrameEventArgs args) {
			frameTimer.Start();

			// Background pass
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// Geometry pass
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferGeometry);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GeometryShader.use();
			Matrix4 MatrixView = Matrix4.LookAt(CameraPosition, CameraPosition + CameraTarget, Vector3.UnitY);
			GL.UniformMatrix4(GeometryShader.UniformView_ID, true, ref MatrixView);
			GL.UniformMatrix4(GeometryShader.UniformPerspective_ID, true, ref MatrixPerspective);
			int drawcalls = SceneRoot.Draw();

			// Lighting pass
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			LightingShader.use();
			GL.Uniform3(LightingShader.UniformCameraPosition_ID, CameraPosition.X, CameraPosition.Y, CameraPosition.Z);
			for (int i = 0; i < FramebufferTextures.Length; i++)
				FramebufferTextures[i].Bind(i);
			SceneRoot.Draw();
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			LightingShader.ResetLights();

			// Interface pass
			InterfaceShader.use();
			InterfaceRoot.Draw();

			// Frame done
			Context.SwapBuffers();

			frameTimer.Stop();
			double time = 1000 * (double)frameTimer.ElapsedTicks / (double)Stopwatch.Frequency; // in milliseconds
			int targetFramerate = 60;
			this.Title = $"Display - {1000 / time,-1:F1} FPS ({time,-4:F4}ms, {100 * time / (1000 / targetFramerate),-2:F2}% budget, {drawcalls} draw calls)"; // 16.6ms frame budget
			frameTimer.Reset();
		}

		/// <summary> Handles all logical game events and input. <br/> THREAD: Logic </summary>
		protected override void OnUpdateFrame(FrameEventArgs args) {
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
		protected override void OnResize(ResizeEventArgs e) {
			GL.Viewport(0, 0, WindowSize.X, WindowSize.Y);
			MatrixPerspective = Matrix4.CreatePerspectiveFieldOfView(90f * RCF, (float)WindowSize.X / (float)WindowSize.Y, 0.001f, 100.0f);
		}

		/// <summary> Handles all debug callbacks from OpenGL and throws exceptions if unhandled. </summary>
		private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
			string messageString = Marshal.PtrToStringAnsi(message, length);
			Console.WriteLine($"{severity} {type} | {messageString}");
			if (type == DebugType.DebugTypeError)
				throw new Exception(messageString);
		}

		public static void Main(string[] args) {
			Console.WriteLine("Initializing");
			GameWindowSettings gws = new GameWindowSettings();
			gws.IsMultiThreaded = true;
			gws.RenderFrequency = 0.0;
			gws.UpdateFrequency = 60;

			NativeWindowSettings nws = new NativeWindowSettings();
			nws.Size = WindowSize;
			nws.Title = "Display";
			nws.WindowBorder = WindowBorder.Fixed;

			Console.WriteLine("Creating game object");
			using (Game g = new Game(gws, nws)) {
				g.Run();
			}
		}
	}
}
