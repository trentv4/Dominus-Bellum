using System;
using System.IO;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace DominusCore {
	/// <summary> Superclass for all shaderprograms. Handles attachment and cleanup of individual shaders. </summary>
	public class ShaderProgram {
		/// <summary> OpenGL ID for this shaderprogram. </summary>
		public int ShaderProgram_ID { get; private set; } = -1;
		private readonly int _vertexArrayObject_ID;
		private Shader[] _shaders;

		public ShaderProgram(string unified) {
			_vertexArrayObject_ID = GL.GenVertexArray();
			_shaders = new Shader[] {
				new Shader(unified, ShaderType.VertexShader, true),
				new Shader(unified, ShaderType.FragmentShader, true)
			};
			ShaderProgram_ID = GL.CreateProgram();
			GL.AttachShader(ShaderProgram_ID, _shaders[0].shaderID);
			GL.AttachShader(ShaderProgram_ID, _shaders[1].shaderID);
			TryLoadShaders();
		}

		private void TryLoadShaders() {
			bool isAnyShaderReloaded = false;
			foreach (Shader s in _shaders) {
				if (s.TryLoad()) isAnyShaderReloaded = true;
			}
			if (isAnyShaderReloaded) {
				GL.LinkProgram(ShaderProgram_ID);
				GL.UseProgram(ShaderProgram_ID);
				if (GL.GetProgramInfoLog(ShaderProgram_ID) != System.String.Empty) {
					Console.WriteLine($"Error linking shader program: {GL.GetProgramInfoLog(ShaderProgram_ID)}");
				}
			}

			SetUniforms();
		}

		public virtual ShaderProgram Use(Game.RenderPass pass) {
			TryLoadShaders();
			GL.UseProgram(ShaderProgram_ID);
			GL.BindVertexArray(_vertexArrayObject_ID);
			Game.CurrentPass = pass;
			return this;
		}

		/// <summary> Assigns the pre-determined vertex attrib information to attrib pointers. This is called once after
		/// creating at least one VBO in this format. Provide the attribs as a series of ints specifying attrib size.
		/// For example, [vec3, vec4, vec3, vec2] would be int[] { 3, 4, 3, 2 }. </summary>
		public virtual ShaderProgram SetVertexAttribPointers(int[] attribs) {
			Use(Game.CurrentPass);
			int stride = attribs.Sum() * sizeof(float);
			int runningTotal = 0;
			for (int i = 0; i < attribs.Length; i++) {
				GL.EnableVertexAttribArray(i);
				GL.VertexAttribPointer(i, attribs[i], VertexAttribPointerType.Float, false, stride, runningTotal);
				runningTotal += attribs[i] * sizeof(float);
			}
			return this;
		}

		protected virtual void SetUniforms() { }

		private class Shader {
			internal readonly string filePath;
			internal DateTime lastWriteTime;
			internal int shaderID;
			internal bool isUnified;
			internal ShaderType type;

			internal Shader(string filePath, ShaderType type, bool isUnified) {
				this.filePath = filePath;
				this.lastWriteTime = DateTime.UnixEpoch;
				this.shaderID = GL.CreateShader(type);
				this.isUnified = isUnified;
				this.type = type;
			}

			internal bool TryLoad() {
				DateTime updatedLastTime = File.GetLastWriteTime(filePath);
				if (lastWriteTime == updatedLastTime)
					return false;
				lastWriteTime = updatedLastTime;

				string shaderSource = "";
				if (isUnified) {
					string[] tempSources = new StreamReader(filePath).ReadToEnd().Split("<split>");
					if (type == ShaderType.VertexShader) shaderSource = tempSources[0];
					if (type == ShaderType.FragmentShader) shaderSource = tempSources[1];
				} else {
					shaderSource = new StreamReader(filePath).ReadToEnd();
				}

				GL.ShaderSource(shaderID, shaderSource);
				GL.CompileShader(shaderID);

				if (GL.GetShaderInfoLog(shaderID) != System.String.Empty) {
					Console.WriteLine($"Error compiling shader {filePath}: {GL.GetShaderInfoLog(shaderID)}");
				}
				return true;
			}
		}
	}



	/// <summary> Interface shader program, with extra uniform IDs only needed for interface shaders. </summary>
	public class ShaderProgramInterface : ShaderProgram {
		public ShaderProgramInterface(string unifiedPath) : base(unifiedPath) { }

		public int UniformElementTexture_ID { get; private set; } = -1;
		public int UniformModel_ID { get; private set; } = -1;
		public int UniformDepth_ID { get; private set; } = -1;
		public int UniformPerspective_ID { get; private set; } = -1;
		public int UniformIsFont_ID { get; private set; } = -1;

		protected override void SetUniforms() {
			UniformElementTexture_ID = GL.GetUniformLocation(ShaderProgram_ID, "elementTexture");
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformDepth_ID = GL.GetUniformLocation(ShaderProgram_ID, "depth");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformIsFont_ID = GL.GetUniformLocation(ShaderProgram_ID, "isFont");
		}

		/// <summary> Sets OpenGL to use this shader program, and keeps track of the current shader in Game. </summary>
		public override ShaderProgramInterface Use(Game.RenderPass pass) {
			base.Use(pass);

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
		public ShaderProgramGeometry(string unifiedPath) : base(unifiedPath) { }

		public int[] TextureUniforms { get; private set; }
		public int UniformModel_ID { get; private set; } = -1;
		public int UniformView_ID { get; private set; } = -1;
		public int UniformPerspective_ID { get; private set; } = -1;

		protected override void SetUniforms() {
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
		}
	}

	/// <summary> Lighting shader program, with extra uniform IDs only needed for lighting shaders. </summary>
	public class ShaderProgramLighting : ShaderProgram {
		public ShaderProgramLighting(string unifiedPath) : base(unifiedPath) { }

		private readonly static int MAX_LIGHT_COUNT = 16;
		private readonly LightUniforms[] UniformLights_ID = new LightUniforms[MAX_LIGHT_COUNT];
		public int UniformCameraPosition_ID { get; private set; } = -1;
		/// <summary> Stores the next available light index during renderering, and reset every frame. </summary>
		public int NextLightID = 0;

		protected override void SetUniforms() {
			for (int i = 0; i < MAX_LIGHT_COUNT; i++) {
				UniformLights_ID[i] = new LightUniforms(
					GL.GetUniformLocation(ShaderProgram_ID, $"lights[{i}].position"),
					GL.GetUniformLocation(ShaderProgram_ID, $"lights[{i}].color"),
					GL.GetUniformLocation(ShaderProgram_ID, $"lights[{i}].direction"),
					GL.GetUniformLocation(ShaderProgram_ID, $"lights[{i}].strength"));
			}

			UniformCameraPosition_ID = GL.GetUniformLocation(ShaderProgram_ID, "cameraPosition");

			GL.Uniform1(GL.GetUniformLocation(ShaderProgram_ID, "gPosition"), 0);
			GL.Uniform1(GL.GetUniformLocation(ShaderProgram_ID, "gNormal"), 1);
			GL.Uniform1(GL.GetUniformLocation(ShaderProgram_ID, "gAlbedoSpec"), 2);
		}

		/// <summary> Sets the uniforms for a single light. </summary>
		public void SetLightUniform(int i, float strength, Vector3 position, Vector3 color, Vector3 direction) {
			GL.Uniform3(UniformLights_ID[i].position, position.X, position.Y, position.Z);
			GL.Uniform3(UniformLights_ID[i].color, color.X, color.Y, color.Z);
			GL.Uniform3(UniformLights_ID[i].direction, direction.X, direction.Y, direction.Z);
			GL.Uniform1(UniformLights_ID[i].strength, strength);
		}

		/// <summary> Resets the NextLightID to zero, and sets the strength of all lights in the scene to 0 preventing drawing.
		/// This prevents lights being "left on" as the scene changes. </summary>
		public void ResetLights() {
			NextLightID = 0;
			for (int i = 0; i < MAX_LIGHT_COUNT; i++)
				GL.Uniform1(UniformLights_ID[i].strength, 0.0f);
		}

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
	}
}