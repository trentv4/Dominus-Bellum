using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

//https://opentk.net/learn/chapter1/2-hello-triangle.html#linking-vertex-attributes

public class Game : GameWindow
{
	public static readonly string TITLE = "Dominus Bellum";
	public static readonly double RENDER_UPDATE_PER_SECOND = 60.0;
	public static readonly double LOGIC_UPDATE_PER_SECOND = 60.0;
	public static readonly Vector2i WINDOW_SIZE = new Vector2i(1280, 720);
	private int VAO = -1;

	public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

	private int CreateShader(string source, ShaderType type)
	{
		Console.WriteLine($"Create shader: \"{source}\"");
		int shader = GL.CreateShader(type);
		GL.ShaderSource(shader, new StreamReader(source).ReadToEnd());
		GL.CompileShader(shader);
		if (GL.GetShaderInfoLog(shader) != System.String.Empty)
			System.Console.WriteLine($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shader)}");
		return shader;
	}

	protected override void OnRenderThreadStarted()
	{
		Console.WriteLine("OnRenderThreadStarted(): start");

		GL.ClearColor(0.2f, 0.2f, 0.3f, 1.0f);

		int ShaderProgram = GL.CreateProgram();
		GL.AttachShader(ShaderProgram, CreateShader("vertex.glsl", ShaderType.VertexShader));
		GL.AttachShader(ShaderProgram, CreateShader("fragment.glsl", ShaderType.FragmentShader));
		GL.LinkProgram(ShaderProgram);
		GL.UseProgram(ShaderProgram);

		float[] vertices = {
			0.5f, 0.5f, 0.0f,
			0.5f, -0.5f, 0.0f,
			-0.5f, 0.5f, 0.0f,
			-0.5f, -0.5f, 0.0f,
		};

		uint[] indices = {
			0, 1, 2,
			1, 2, 3,
		};

		VAO = GL.GenVertexArray();
		GL.BindVertexArray(VAO);

		int EBO = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
		GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

		int VertexBufferObject = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
		GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

		GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
		GL.EnableVertexAttribArray(0);

		Console.WriteLine("OnRenderThreadStarted(): end");
	}

	protected override void OnRenderFrame(FrameEventArgs args)
	{
		GL.Clear(ClearBufferMask.ColorBufferBit);

		GL.BindVertexArray(VAO);
		GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

		Context.SwapBuffers();
		Console.WriteLine("OnRenderFrame(): end");
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