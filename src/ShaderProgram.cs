using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace DominusCore {
	/// <summary> Superclass for all shaderprograms. Handles attachment and cleanup of individual shaders. </summary>
	public class ShaderProgram {
		/// <summary> OpenGL ID for this shaderprogram. </summary>
		public readonly int ShaderProgram_ID;

		/// <summary> Creates and uses a new shader program using provided shader IDs to attach. <br/>
		/// Use ShaderProgram.CreateShader(...) to get these IDs.</summary>
		public ShaderProgram(params int[] shaders) {
			ShaderProgram_ID = GL.CreateProgram();
			foreach (int i in shaders)
				GL.AttachShader(ShaderProgram_ID, i);
			GL.LinkProgram(ShaderProgram_ID);
			GL.UseProgram(ShaderProgram_ID);
			foreach (int i in shaders)
				GL.DeleteShader(i);
		}

		/// <summary> Creates, loads, and compiles a shader given a file. Handles errors in the shader. Returns the shader ID. </summary>
		public static int CreateShader(string source, ShaderType type) {
			Console.WriteLine($"Create shader: \"{source}\"");
			int shader = GL.CreateShader(type);
			GL.ShaderSource(shader, new StreamReader(source).ReadToEnd());
			GL.CompileShader(shader);
			if (GL.GetShaderInfoLog(shader) != System.String.Empty)
				throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shader)}");
			return shader;
		}

		/// <summary> Creates, loads, splits, and compiles a shader given a file. Handles errors in the shaders. Returns the shader IDs as an array. 
		/// <br/> This is the method used for combined shader files. To create a combined shader file, place the vertex shader in a file,
		/// then &lt; split &gt; with no spaces, then your fragment shader. This is meant to facilitate easier debugging for new shaders. </summary>
		public static int[] CreateShaderFromUnified(string source) {
			Console.WriteLine($"Creating unified shader: \"{source}\"");
			ShaderType[] types = new ShaderType[] { ShaderType.VertexShader, ShaderType.FragmentShader };
			string[] sources = new StreamReader(source).ReadToEnd().Split("<split>");
			int[] shaderIDs = new int[sources.Length];
			for (int i = 0; i < sources.Length; i++) {
				int shader = GL.CreateShader(types[i]);
				GL.ShaderSource(shader, sources[i]);
				GL.CompileShader(shader);
				if (GL.GetShaderInfoLog(shader) != System.String.Empty)
					throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shader)}");
				shaderIDs[i] = shader;
			}
			return shaderIDs;
		}
	}

	/// <summary> Interface shader program, with extra uniform IDs only needed for interface shaders. </summary>
	public class ShaderProgramInterface : ShaderProgram {
		public readonly int UniformElementTexture_ID;
		public readonly int UniformModel_ID;
		public readonly int UniformDepth_ID;
		public readonly int UniformIsFont_ID;
		public readonly int VertexArrayObject_ID;

		/// <summary> Creates and uses a new shader program using provided shader IDs to attach. <br/>
		/// Use ShaderProgram.CreateShader(...) to get these IDs.</summary>
		public ShaderProgramInterface(params int[] shaders) : base(shaders) {
			UniformElementTexture_ID = GL.GetUniformLocation(ShaderProgram_ID, "elementTexture");
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformDepth_ID = GL.GetUniformLocation(ShaderProgram_ID, "depth");
			UniformIsFont_ID = GL.GetUniformLocation(ShaderProgram_ID, "isFont");
			VertexArrayObject_ID = GL.GenVertexArray();
		}

		/// <summary> Sets OpenGL to use this shader program, and keeps track of the current shader in Game. </summary>
		public ShaderProgramInterface use(Game.RenderPass pass) {
			GL.UseProgram(ShaderProgram_ID);
			GL.BindVertexArray(VertexArrayObject_ID);
			Game.CurrentPass = pass;

			if (pass == Game.RenderPass.InterfaceBackground) {
				GL.Uniform1(UniformDepth_ID, 0.999999f);
				GL.Uniform1(UniformIsFont_ID, 0);
			} else if (pass == Game.RenderPass.InterfaceForeground) {
				GL.Uniform1(UniformDepth_ID, 0.2f);
				GL.Uniform1(UniformIsFont_ID, 0);
			} else if (pass == Game.RenderPass.InterfaceText) {
				GL.Uniform1(UniformDepth_ID, 0.1f);
				GL.Uniform1(UniformIsFont_ID, 1);
			}

			return this;
		}
	}

	/// <summary> Geometry shader program, with extra uniform IDs only needed for geometry shaders. </summary>
	public class ShaderProgramGeometry : ShaderProgram {
		public readonly int[] TextureUniforms;
		public readonly int UniformModel_ID;
		public readonly int UniformView_ID;
		public readonly int UniformPerspective_ID;
		public readonly int VertexArrayObject_ID;

		private Texture[] FramebufferTextures;
		public readonly int FramebufferGeometry;

		/// <summary> Creates and uses a new shader program using provided shader IDs to attach. <br/>
		/// Use ShaderProgram.CreateShader(...) to get these IDs.</summary>
		public ShaderProgramGeometry(params int[] shaders) : base(shaders) {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformView_ID = GL.GetUniformLocation(ShaderProgram_ID, "view");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");

			TextureUniforms = new int[] {
				GL.GetUniformLocation(ShaderProgram_ID, "map_diffuse"),
				GL.GetUniformLocation(ShaderProgram_ID, "map_gloss"),
				GL.GetUniformLocation(ShaderProgram_ID, "map_ao"),
				GL.GetUniformLocation(ShaderProgram_ID, "map_normal"),
				GL.GetUniformLocation(ShaderProgram_ID, "map_height")
			};
			VertexArrayObject_ID = GL.GenVertexArray();

			FramebufferGeometry = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferGeometry);
			FramebufferTextures = new Texture[] { new Texture(0), new Texture(1), new Texture(2) };
			int depth = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, depth);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, Game.WindowSize.X, Game.WindowSize.Y,
						  0, PixelFormat.DepthComponent, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
									TextureTarget.Texture2D, depth, 0);
			DrawBuffersEnum[] attachments = new DrawBuffersEnum[FramebufferTextures.Length];
			for (int i = 0; i < attachments.Length; i++)
				attachments[i] = DrawBuffersEnum.ColorAttachment0 + i;
			GL.DrawBuffers(attachments.Length, attachments);
		}

		/// <summary> Sets OpenGL to use this shader program, and keeps track of the current shader in Game. </summary>
		public ShaderProgramGeometry use() {
			GL.UseProgram(ShaderProgram_ID);
			GL.BindVertexArray(VertexArrayObject_ID);
			Game.CurrentPass = Game.RenderPass.Geometry;
			return this;
		}

		public void BindFramebufferTextures() {
			FramebufferTextures[0].Bind(0, Game.LightingShader.UniformGPosition);
			FramebufferTextures[1].Bind(1, Game.LightingShader.UniformGNormal);
			FramebufferTextures[2].Bind(2, Game.LightingShader.UniformGAlbedoSpec);
		}
	}

	/// <summary> Lighting shader program, with extra uniform IDs only needed for lighting shaders. </summary>
	public class ShaderProgramLighting : ShaderProgram {
		/// <summary> Data format for a single Light in the shader, storing the uniform locations. </summary>
		public struct LightUniforms {
			public int position;
			public int color;
			public int direction;
			public int strength;

			public LightUniforms(int position, int color, int direction, int strength) {
				this.position = position;
				this.color = color;
				this.direction = direction;
				this.strength = strength;
			}
		}

		private readonly static int MAX_LIGHT_COUNT = 16;
		private readonly LightUniforms[] UniformLights_ID = new LightUniforms[MAX_LIGHT_COUNT];
		public readonly int UniformCameraPosition_ID;
		public readonly int UniformGPosition;
		public readonly int UniformGNormal;
		public readonly int UniformGAlbedoSpec;
		/// <summary> Stores the next available light index during renderering, and reset every frame. </summary>
		public int NextLightID = 0;

		/// <summary> Creates and uses a new shader program using provided shader IDs to attach. <br/>
		/// Use ShaderProgram.CreateShader(...) to get these IDs.</summary>
		public ShaderProgramLighting(params int[] shaders) : base(shaders) {
			for (int i = 0; i < MAX_LIGHT_COUNT; i++) {
				UniformLights_ID[i] = new LightUniforms(
					GL.GetUniformLocation(ShaderProgram_ID, $"lights[{i}].position"),
					GL.GetUniformLocation(ShaderProgram_ID, $"lights[{i}].color"),
					GL.GetUniformLocation(ShaderProgram_ID, $"lights[{i}].direction"),
					GL.GetUniformLocation(ShaderProgram_ID, $"lights[{i}].strength"));
			}

			UniformCameraPosition_ID = GL.GetUniformLocation(ShaderProgram_ID, "cameraPosition");

			UniformGPosition = GL.GetUniformLocation(ShaderProgram_ID, "gPosition");
			UniformGNormal = GL.GetUniformLocation(ShaderProgram_ID, "gNormal");
			UniformGAlbedoSpec = GL.GetUniformLocation(ShaderProgram_ID, "gAlbedoSpec");
		}

		/// <summary> Sets the uniforms for a single light. </summary>
		public void SetLightUniform(int i, float strength, Vector3 position, Vector3 color, Vector3 direction) {
			GL.Uniform3(UniformLights_ID[i].position, position.X, position.Y, position.Z);
			GL.Uniform3(UniformLights_ID[i].color, color.X, color.Y, color.Z);
			GL.Uniform3(UniformLights_ID[i].direction, direction.X, direction.Y, direction.Z);
			GL.Uniform1(UniformLights_ID[i].strength, strength);
		}

		/// <summary> Sets OpenGL to use this shader program, and keeps track of the current shader in Game. </summary>
		public ShaderProgramLighting use() {
			GL.UseProgram(ShaderProgram_ID);
			Game.CurrentPass = Game.RenderPass.Lighting;
			return this;
		}

		/// <summary> Resets the NextLightID to zero, and sets the strength of all lights in the scene to 0 preventing drawing.
		/// This prevents lights being "left on" as the scene changes. </summary>
		public void ResetLights() {
			NextLightID = 0;
			for (int i = 0; i < MAX_LIGHT_COUNT; i++)
				GL.Uniform1(UniformLights_ID[i].strength, 0.0f);
		}
	}
}