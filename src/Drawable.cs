using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using Assimp;
using System.IO;

namespace DominusCore {
	/// <summary> Rendering node object, where anything drawn in a scene is a Drawable. This can be lights, models, fx, etc. </summary>
	public class Drawable {
		public List<Drawable> children = new List<Drawable>();
		/// <summary> Determines if the element and its children will be drawn. </summary>
		public bool isEnabled = true;

		public Drawable() { }

		/// <summary> Draws all children of this Drawable recursively if applicable, then draws itself. </summary>
		public int Draw() {
			if (!isEnabled) return 0;
			int runningCount = 0;
			foreach (Drawable child in children) {
				runningCount += child.Draw();
			}

			return runningCount + Convert.ToByte(DrawSelf());
		}

		/// <summary> Overridden method to handle drawing each subclass. Return true if this executes (correct render pass)</summary>
		public virtual bool DrawSelf() { return false; }

		public Drawable AddChild(Drawable child) {
			children.Add(child);
			return this;
		}

		public Drawable AddChildren(params Drawable[] children) {
			this.children.AddRange(children);
			return this;
		}

		public Drawable SetEnabled(bool state) {
			isEnabled = state;
			return this;
		}
	}

	/// <summary> Storage format for textured quads with no depth. This is meant for interface use only. Vertex data is not preserved in CPU after upload.
	/// <br/> Provides methods to set position, scale, rotation, and to dispose. </summary>
	internal class InterfaceImage : Drawable, IDisposable {
		private readonly Texture texture;
		private readonly Game.RenderPass DrawPass;

		public readonly int VertexBufferObject_ID;
		public Vector3 Scale { get; private set; } = Vector3.One;
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;
		public Matrix4 ModelMatrix { get; private set; } = Matrix4.Identity;

		/// <summary> Creates a drawable object with a given set of vertex data ([xyz][uv][rgba][qrs]).
		/// <br/>This represents a single model with specified data. It may be rendered many times with different uniforms,
		/// but the vertex data will remain static. Usage is hinted as StaticDraw. Both index and vertex data is
		/// discarded immediately after being sent to the GL context.
		/// <br/> !! Warning !! This is not a logical unit and exists on the render thread only! </summary>
		public InterfaceImage(Texture texture, Game.RenderPass passToDrawIn) {
			float[] vertexData = new float[]{
				-1.0f, -1.0f, 0.0f, 0.0f,
				-1.0f,  1.0f, 0.0f, 1.0f,
				 1.0f, -1.0f, 1.0f, 0.0f,
				-1.0f,  1.0f, 0.0f, 1.0f,
				 1.0f, -1.0f, 1.0f, 0.0f,
				 1.0f,  1.0f, 1.0f, 1.0f,
			};

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);

			this.texture = texture;
			this.DrawPass = passToDrawIn;
			UpdateModelMatrix();
		}

		/// <summary> Binds the index and vertex buffers, binds textures, then draws. Does not recurse.
		/// <br/> !! Warning !! This may be performance heavy with large amounts of different models! </summary>
		public override bool DrawSelf() {
			if (Game.CurrentPass != DrawPass) return false;

			GL.BindVertexBuffer(0, VertexBufferObject_ID, (IntPtr)(0 * sizeof(float)), 4 * sizeof(float));
			GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(2 * sizeof(float)), 4 * sizeof(float));
			Matrix4 tempModelMatrix = ModelMatrix;
			GL.UniformMatrix4(Game.InterfaceShader.UniformModel_ID, true, ref tempModelMatrix);
			texture.Bind();
			GL.DrawArrays(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, 0, 6);
			return true;
		}

		/// <summary> Deletes buffers in OpenGL. This is automatically done by object garbage collection OR program close.
		/// <br/> !! Warning !! Avoid using this unless you know what you're doing! This can crash! </summary>
		public void Dispose() {
			GL.DeleteBuffer(VertexBufferObject_ID);
		}

		/// <summary> Regenerates the model matrix after an update to scale, translation, or rotation. </summary>
		private void UpdateModelMatrix() {
			Matrix4 m = Matrix4.CreateScale(Scale);
			m *= Matrix4.CreateRotationX(Rotation.X * Game.RCF);
			m *= Matrix4.CreateRotationY(Rotation.Y * Game.RCF);
			m *= Matrix4.CreateRotationZ(Rotation.Z * Game.RCF);
			m *= Matrix4.CreateTranslation(Position);
			ModelMatrix = m;
		}

		/// <summary> Chainable method to set the scale of this object. </summary>
		public InterfaceImage SetScale(Vector3 scale) {
			this.Scale = scale;
			UpdateModelMatrix();
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public InterfaceImage SetScale(float scale) {
			return SetScale(new Vector3(scale, scale, scale));
		}

		/// <summary> Chainable method to set the rotation of this object. </summary>
		public InterfaceImage SetRotation(Vector3 rotation) {
			this.Rotation = rotation;
			UpdateModelMatrix();
			return this;
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public InterfaceImage SetPosition(Vector3 position) {
			this.Position = position;
			UpdateModelMatrix();
			return this;
		}
	}

	public class InterfaceString : Drawable {
		public string TextContent { get; private set; }
		public Vector2 Scale { get; private set; } = Vector2.One;
		public Vector2 Position { get; private set; } = Vector2.Zero;
		public float Width { get; private set; }
		public float Opacity { get; private set; } = 1.0f;

		private readonly int _elementBufferArrayID;
		private readonly int _vertexBufferObjectID;
		private FontAtlas _font;
		private int _indexLength;

		/// <summary> Creates a new InterfaceString and handles sending data to the GPU. </summary>
		public InterfaceString(string font, string TextContent) {
			_font = FontAtlas.GetFont(font);
			_elementBufferArrayID = GL.GenBuffer();
			_vertexBufferObjectID = GL.GenBuffer();

			this.TextContent = TextContent;

			UpdateStringOnGPU(TextContent);
		}

		/// <summary> Updates the vertex and index lists on the GPU for the new text. </summary>
		private void UpdateStringOnGPU(string text) {
			List<float> vertices = new List<float>();
			List<uint> indices = new List<uint>();

			List<int> unicodeList = new List<int>(text.Length);
			for (int i = 0; i < text.Length; i++) {
				unicodeList.Add(Char.ConvertToUtf32(text, i));
				if (Char.IsHighSurrogate(text[i]))
					i++;
			}
			float cursor = 0;
			for (int i = 0; i < unicodeList.Count; i++) {
				FontAtlas.Glyph g = _font.GetGlyph(unicodeList[i]);
				vertices.AddRange(new float[] {
					cursor + g.PositionOffset.X, 0 + g.PositionOffset.Y,
					g.UVs[0].X, g.UVs[0].Y,
					cursor + g.PositionOffset.X + g.Size.X, 0 + g.PositionOffset.Y,
					g.UVs[1].X, g.UVs[1].Y,
					cursor + g.PositionOffset.X, g.Size.Y + g.PositionOffset.Y,
					g.UVs[2].X, g.UVs[2].Y,
					cursor + g.PositionOffset.X + g.Size.X, g.Size.Y + g.PositionOffset.Y,
					g.UVs[3].X, g.UVs[3].Y,
				});
				indices.AddRange(new uint[] { ((uint)i * 4) + 0, ((uint)i * 4) + 1, ((uint)i * 4) + 2, ((uint)i * 4) + 1, ((uint)i * 4) + 2, ((uint)i * 4) + 3 });
				cursor += g.Advance;
			}

			float[] vert = vertices.ToArray();
			uint[] ind = indices.ToArray();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferArrayID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, ind.Length * sizeof(uint), ind, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObjectID);
			GL.BufferData(BufferTarget.ArrayBuffer, vert.Length * sizeof(float), vert, BufferUsageHint.StaticDraw);

			_indexLength = ind.Length;
			Width = cursor;
		}

		/// <summary> Draws the font on the GPU. This is done with the Interface shader, but the use of the font uniform causes this to be a text box. </summary>
		public override bool DrawSelf() {
			if (Game.CurrentPass != Game.RenderPass.InterfaceText) return false;

			Matrix4 modelMatrix = Matrix4.Identity;
			modelMatrix *= Matrix4.CreateScale(new Vector3(Scale.X, Scale.Y, 1f));
			modelMatrix *= Matrix4.CreateTranslation(new Vector3(Position.X, Position.Y, 0f));
			GL.UniformMatrix4(Game.InterfaceShader.UniformModel_ID, true, ref modelMatrix);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferArrayID);
			GL.BindVertexBuffer(0, _vertexBufferObjectID, (IntPtr)(0 * sizeof(float)), 4 * sizeof(float));
			GL.BindVertexBuffer(1, _vertexBufferObjectID, (IntPtr)(2 * sizeof(float)), 4 * sizeof(float));

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, _font.AtlasTexture.TextureID);

			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, _indexLength, DrawElementsType.UnsignedInt, 0);
			return true;
		}

		/// <summary> Chainable method to set the scale of this object. </summary>
		public InterfaceString SetScale(Vector2 scale) {
			this.Scale = scale;
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public InterfaceString SetScale(float scale) {
			return SetScale(new Vector2(scale, scale));
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public InterfaceString SetPosition(Vector2 position) {
			this.Position = position;
			return this;
		}

		public InterfaceString SetOpacity(float opacity) {
			this.Opacity = opacity;
			return this;
		}
	}

	/// <summary> Storage format for non-animated models. This does not preserve vertex data in CPU memory after upload. 
	/// <br/> Provides methods to bind, draw, and dispose.</summary>
	internal class Model : Drawable, IDisposable {
		private readonly int IndexLength;
		private readonly Texture[] textures;

		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		public Vector3 Scale { get; private set; } = Vector3.One;
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;
		public Matrix4 ModelMatrix { get; private set; } = Matrix4.Identity;

		/// <summary> Creates a drawable object with a given set of vertex data ([xyz][uv][rgba][qrs]).
		/// <br/>This represents a single model with specified data. It may be rendered many times with different uniforms,
		/// but the vertex data will remain static. Usage is hinted as StaticDraw. Both index and vertex data is
		/// discarded immediately after being sent to the GL context.
		/// <br/> !! Warning !! This is not a logical unit and exists on the render thread only! </summary>
		public Model(float[] vertexData, uint[] indices, params Texture[] textures) {
			IndexLength = indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);

			this.textures = textures;
			UpdateModelMatrix();
		}

		/// <summary> Binds the index and vertex buffers, binds textures, then draws. Does not recurse.
		/// <br/> !! Warning !! This may be performance heavy with large amounts of different models! </summary>
		public override bool DrawSelf() {
			if (Game.CurrentPass != Game.RenderPass.Geometry) return false;
			Matrix4 tempModelMatrix = ModelMatrix;
			GL.UniformMatrix4(Game.GeometryShader.UniformModel_ID, true, ref tempModelMatrix);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			int stride = 12 * sizeof(float);
			GL.BindVertexBuffer(0, VertexBufferObject_ID, IntPtr.Zero, stride);
			GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(3 * sizeof(float)), stride);
			GL.BindVertexBuffer(2, VertexBufferObject_ID, (IntPtr)(5 * sizeof(float)), stride);
			GL.BindVertexBuffer(3, VertexBufferObject_ID, (IntPtr)(9 * sizeof(float)), stride);
			for (int i = 0; i < textures.Length; i++) {
				GL.Uniform1(Game.GeometryShader.TextureUniforms[i], i);
				textures[i].Bind(i);
			}
			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
			return true;
		}

		/// <summary> Deletes buffers in OpenGL. This is automatically done by object garbage collection OR program close.
		/// <br/> !! Warning !! Avoid using this unless you know what you're doing! This can crash! </summary>
		public void Dispose() {
			GL.DeleteBuffer(VertexBufferObject_ID);
			GL.DeleteBuffer(ElementBufferArray_ID);
		}

		/// <summary> Regenerates the model matrix after an update to scale, translation, or rotation. </summary>
		private void UpdateModelMatrix() {
			Matrix4 m = Matrix4.CreateScale(Scale);
			m *= Matrix4.CreateRotationX(Rotation.X * Game.RCF);
			m *= Matrix4.CreateRotationY(Rotation.Y * Game.RCF);
			m *= Matrix4.CreateRotationZ(Rotation.Z * Game.RCF);
			m *= Matrix4.CreateTranslation(Position);
			ModelMatrix = m;
		}

		/// <summary> Chainable method to set the scale of this object. </summary>
		public Model SetScale(Vector3 scale) {
			this.Scale = scale;
			UpdateModelMatrix();
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public Model SetScale(float scale) {
			return SetScale(new Vector3(scale, scale, scale));
		}

		/// <summary> Chainable method to set the rotation of this object. </summary>
		public Model SetRotation(Vector3 rotation) {
			this.Rotation = rotation;
			UpdateModelMatrix();
			return this;
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public Model SetPosition(Vector3 position) {
			this.Position = position;
			UpdateModelMatrix();
			return this;
		}

		/// <summary> Creates a model with data from disk. </summary>
		public static Model CreateModelFromFile(string filename) {
			Console.WriteLine($"Loading model from file: \"{filename}\" exists: {File.Exists(filename)}");
			AssimpContext c = new AssimpContext();
			var scene = c.ImportFile(filename, PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);
			Model a = null;
			if (scene.HasMeshes) {
				//TODO(trent): Implement properly (children)
				foreach (Mesh m in scene.Meshes) {
					Vector3D[] vertices = m.Vertices.ToArray();
					Vector3D[] normals = m.Normals.ToArray();
					Vector3D[] uvs = m.TextureCoordinateChannels[0].ToArray();
					Material mat = scene.Materials[m.MaterialIndex];

					List<Texture> t = new List<Texture>();

					t.Add(Texture.CreateTexture($"{mat.Name}-diffuse", scene.Textures[mat.TextureDiffuse.TextureIndex], TextureMinFilter.Linear, OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToBorder));
					t.Add(Texture.CreateTexture($"{mat.Name}-gloss", scene.Textures[mat.TextureSpecular.TextureIndex], TextureMinFilter.Linear, OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToBorder));
					t.Add(Texture.CreateTexture($"{mat.Name}-ao", scene.Textures[mat.TextureAmbientOcclusion.TextureIndex], TextureMinFilter.Linear, OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToBorder));
					t.Add(Texture.CreateTexture($"{mat.Name}-normal", scene.Textures[mat.TextureNormal.TextureIndex], TextureMinFilter.Linear, OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToBorder));
					t.Add(Texture.CreateTexture($"{mat.Name}-height", scene.Textures[mat.TextureHeight.TextureIndex], TextureMinFilter.Linear, OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToBorder));

					List<float> vertexData = new List<float>(vertices.Length * 12);
					for (int i = 0; i < vertices.Length; i++) {
						vertexData.AddRange(new float[]{
							vertices[i].X, vertices[i].Y, vertices[i].Z,
							uvs[i][0], uvs[i][1],
							mat.ColorDiffuse.R, mat.ColorDiffuse.G, mat.ColorDiffuse.B, mat.ColorDiffuse.A,
							normals[i].X, normals[i].Y, normals[i].Z});
					}

					int[] ind = m.GetIndices();
					uint[] indices = (uint[])(object)ind; // nasty...
					a = new Model(vertexData.ToArray(), indices, t.ToArray());
				}
			}

			return a.SetPosition(new Vector3(20, -4, 5)).SetScale(2.0f).SetRotation(new Vector3(0, 135f, 0f));
		}

		/// <summary> Creates a flat plane in the XY plane given a texture list.</summary>
		public static Model CreateDrawablePlane(params Texture[] textures) {
			return new Model(new float[]{
				1.0f,  1.0f, 0.0f,   1.0f, 1.0f,   1.0f, 0.0f, 0.0f, 1.0f,   0.0f, 0.0f, -1.0f,
				1.0f, -1.0f, 0.0f,   1.0f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,   0.0f, 0.0f, -1.0f,
				-1.0f, -1.0f, 0.0f,  0.0f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,   0.0f, 0.0f, -1.0f,
				-1.0f,  1.0f, 0.0f,  0.0f, 1.0f,   1.0f, 0.0f, 0.0f, 1.0f,   0.0f, 0.0f, -1.0f,
			}, new uint[]{
				0, 1, 3,
				1, 2, 3
			}, textures);
		}

		/// <summary> Creates a default cube given a texture list. </summary>
		public static Model CreateDrawableCube(params Texture[] textures) {
			return new Model(new float[]{
				1.0f, 1.0f, 0.0f,   1.0f, 1.0f,   1.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,// 0
				0.0f, 1.0f, 0.0f,   0.0f, 1.0f,   1.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,// 1
				1.0f, 0.0f, 0.0f,   1.0f, 0.0f,   1.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,// 2
				0.0f, 0.0f, 0.0f,   0.0f, 0.0f,   1.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,// 3
				
				1.0f, 1.0f, 1.0f,   0.0f, 0.0f,   1.0f, 0.0f, 1.0f, 1.0f,   1.0f, 1.0f, 1.0f,// 4
				0.0f, 1.0f, 1.0f,   1.0f, 0.0f,   1.0f, 0.0f, 1.0f, 1.0f,   1.0f, 1.0f, 1.0f,// 5
				1.0f, 0.0f, 1.0f,   0.0f, 1.0f,   1.0f, 0.0f, 1.0f, 1.0f,   1.0f, 1.0f, 1.0f,// 6
				0.0f, 0.0f, 1.0f,   1.0f, 1.0f,   1.0f, 0.0f, 1.0f, 1.0f,   1.0f, 1.0f, 1.0f,// 7
			}, new uint[]{
				0, 1, 3,
				0, 2, 3,
				4, 5, 7,
				4, 6, 7,
				2, 0, 4,
				2, 6, 4,
				3, 7, 5,
				3, 1, 5,
				4, 1, 5,
				4, 1, 0,
				3, 6, 7,
				3, 6, 2,
			}, textures);
		}

		/// <summary> Creates a circle given a texture list and density. Density represents the number of vertices on the outside of the circle. </summary>
		public static Model CreateCircle(int density, params Texture[] textures) {
			List<float> vertexList = new List<float>();
			vertexList.AddRange(new List<float> { 0f, 0f, 0f, 0.5f, 0.5f, 1f, 0f, 0f, 1f, 0.0f, 0.0f, -1.0f, });
			for (int i = 1; i <= density; i++) {
				float angle = Game.RCF * i * (360.0f / (float)density);
				vertexList.AddRange(new List<float>{
					(float) Math.Cos(angle), (float) Math.Sin(angle), 0f,
					((float) Math.Cos(angle) + 1)/2.0f, ((float) Math.Sin(angle) + 1)/2.0f,
					1f, 0f, 0f, 1f, 0.0f, 0.0f, -1.0f,
				});
			}

			uint[] indexList = new uint[3 * (density)];
			for (uint i = 0; i < density; i++) {
				indexList[i * 3 + 0] = 0;
				indexList[i * 3 + 1] = i + 1;
				indexList[i * 3 + 2] = i + 2;
			}
			indexList[indexList.Length - 1] = 1;

			float[] vertList = vertexList.ToArray();
			return new Model(vertList, indexList, textures);
		}
	}

	/// <summary> Storage format for lights. This handles a single light which is not bound to a particular uniform in
	/// the lighting shader. 
	/// <br/> This supports point lights and spotlights, which is determined by Direction. If Direction is Vector3.Zero, 
	/// it acts as a spotlight in the shader.</summary>
	internal class Light : Drawable {
		public Vector3 Position { get; private set; }
		public Vector3 Color { get; private set; }
		public Vector3 Direction { get; private set; }
		public float Strength { get; private set; }

		/// <summary> Creates a white point light given position and strength. </summary>
		public Light(Vector3 position, float strength) {
			this.Position = position;
			this.Color = Vector3.One;
			this.Direction = Vector3.Zero;
			this.Strength = strength;
		}

		/// <summary> Creates a point light given position, color, and strength. </summary>
		public Light(Vector3 position, Vector3 color, float strength) {
			this.Position = position;
			this.Color = color;
			this.Direction = Vector3.Zero;
			this.Strength = strength;
		}

		/// <summary> Creates a spotlight given position, color, normalized direction, and strength. </summary>
		public Light(Vector3 position, Vector3 color, Vector3 Direction, float strength) {
			this.Position = position;
			this.Color = color;
			this.Direction = Direction;
			this.Strength = strength;
		}

		/// <summary> Sets appropriate uniforms in the lighting shader using the stored next light ID, and increments the ID.
		/// <br/> !! Warning !! This means that the lighting uniform IDs are not ensured to be consistent from frame to frame!</summary>
		public override bool DrawSelf() {
			if (Game.CurrentPass != Game.RenderPass.Lighting) return false;

			ShaderProgramLighting shader = Game.LightingShader;
			shader.SetLightUniform(shader.NextLightID, Strength, Position, Color, Direction);
			shader.NextLightID++;
			return false;
		}

		/// <summary> Chainable method to set position. </summary>
		public Light SetPosition(Vector3 position) {
			this.Position = position;
			return this;
		}

		/// <summary> Chainable method to set color. </summary>
		public Light SetColor(Vector3 color) {
			this.Color = color;
			return this;
		}

		/// <summary> Chainable method to set direction. If direction is Vector3.Zero, the light acts as a point light, otherwise a spotlight. </summary>
		public Light SetDirection(Vector3 direction) {
			this.Direction = direction;
			return this;
		}

		/// <summary> Chainable method to set light strength. This can be zero, and can remove light from a scene if so. </summary>
		public Light SetStrength(float strength) {
			this.Strength = strength;
			return this;
		}
	}
}
