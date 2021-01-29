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
	public class ShaderProgram
	{
		public readonly int ShaderProgram_ID;

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

		public static int[] CreateShaderFromUnified(string source)
		{
			Console.WriteLine($"Creating unified shader: \"{source}\"");
			ShaderType[] types = new ShaderType[] { ShaderType.VertexShader, ShaderType.FragmentShader };
			string[] sources = new StreamReader(source).ReadToEnd().Split("<split>");
			int[] shaderIDs = new int[sources.Length];
			for (int i = 0; i < sources.Length; i++)
			{
				int shader = GL.CreateShader(types[i]);
				GL.ShaderSource(shader, sources[i]);
				GL.CompileShader(shader);
				if (GL.GetShaderInfoLog(shader) != System.String.Empty)
					throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shader)}");
				shaderIDs[i] = shader;
			}
			return shaderIDs;
		}

		public ShaderProgram use()
		{
			GL.UseProgram(ShaderProgram_ID);
			return this;
		}
	}

	public class ShaderProgramGeometry : ShaderProgram
	{
		public readonly int UniformModel_ID;
		public readonly int UniformView_ID;
		public readonly int UniformPerspective_ID;

		/// <summary> Creates and uses a new shader program using provided shader IDs to attach. <br/>
		/// Use ShaderProgram.CreateShader(...) to get these IDs.</summary>
		public ShaderProgramGeometry(int[] shaders) : base(shaders)
		{
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformView_ID = GL.GetUniformLocation(ShaderProgram_ID, "view");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
		}

		public new ShaderProgramGeometry use()
		{
			GL.UseProgram(ShaderProgram_ID);
			return this;
		}
	}

	public class ShaderProgramLighting : ShaderProgram
	{
		public readonly int UniformLights_ID;

		/// <summary> Creates and uses a new shader program using provided shader IDs to attach. <br/>
		/// Use ShaderProgram.CreateShader(...) to get these IDs.</summary>
		public ShaderProgramLighting(int[] shaders) : base(shaders)
		{
			UniformLights_ID = GL.GetUniformLocation(ShaderProgram_ID, "light");
		}

		public new ShaderProgramLighting use()
		{
			GL.UseProgram(ShaderProgram_ID);
			return this;
		}
	}
}