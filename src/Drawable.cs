using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using OpenTK.Audio.OpenAL.Extensions.EXT.FloatFormat;
using System.Linq;

namespace DominusCore {
	/// <summary> Rendering node object, where anything drawn in a scene is a Drawable. This can be lights, models, fx, etc. </summary>
	class Drawable {
		public List<Drawable> children = new List<Drawable>();
		/// <summary> Determines if the element and its children will be drawn. </summary>
		public bool isEnabled = true;

		public Drawable() { }

		/// <summary> Draws all children of this Drawable recursively if applicable, then draws itself. </summary>
		public void Draw() {
			if (!isEnabled) return;
			foreach (Drawable child in children) {
				child.Draw();
			}
			DrawSelf();
		}

		/// <summary> Overridden method to handle drawing each subclass. </summary>
		public virtual void DrawSelf() { }

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

	/// <summary> Storage format for non-animated models. This does not preserve vertex data in CPU memory after upload. 
	/// <br/> Provides methods to bind, draw, and dispose.</summary>
	internal class Model : Drawable, IDisposable {
		private readonly int IndexLength;
		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		private readonly Texture[] textures;

		public Vector3 Scale { get; private set; } = new Vector3(1, 1, 1);
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;

		private Matrix4 ModelMatrix;

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
		public override void DrawSelf() {
			if (Game.CurrentShader != Game.GeometryShader) return;
			Matrix4 MatrixModel = GetModelMatrix();
			GL.UniformMatrix4(Game.GeometryShader.UniformModel_ID, true, ref MatrixModel);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			int stride = 12 * sizeof(float);
			GL.BindVertexBuffer(0, VertexBufferObject_ID, IntPtr.Zero, stride);
			GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(3 * sizeof(float)), stride);
			GL.BindVertexBuffer(2, VertexBufferObject_ID, (IntPtr)(5 * sizeof(float)), stride);
			GL.BindVertexBuffer(3, VertexBufferObject_ID, (IntPtr)(9 * sizeof(float)), stride);
			for (int i = 0; i < textures.Length; i++)
				textures[i].Bind(i);
			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
		}

		/// <summary> Deletes buffers in OpenGL. This is automatically done by object garbage collection OR program close.
		/// <br/> !! Warning !! Avoid using this unless you know what you're doing! This can crash! </summary>
		public void Dispose() {
			GL.DeleteBuffer(VertexBufferObject_ID);
			GL.DeleteBuffer(ElementBufferArray_ID);
		}

		/// <summary> Returns the model matrix to go from local to world space. This result is cached. </summary>
		public Matrix4 GetModelMatrix() {
			return ModelMatrix;
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
				float angle = Game.RCF * i * (360 / density);
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
		public override void DrawSelf() {
			if (Game.CurrentShader != Game.LightingShader) return;
			ShaderProgramLighting shader = Game.LightingShader;
			shader.SetLightUniform(shader.NextLightID, Strength, Position, Color, Direction);
			shader.NextLightID++;
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
