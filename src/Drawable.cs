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
	internal class Drawable : IDisposable
	{
		private readonly int IndexLength;
		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		private readonly Texture[] textures;

		public Vector3 Scale { get; private set; } = Vector3.Zero;
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;

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

		/// <summary> Sends the GL.DrawElements() call. </summary>
		public void Draw()
		{
			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
		}

		/// <summary> Binds the vertex data, index data, then calls GL.DrawElements(). All warnings from Drawable.Bind() also apply here.</summary>
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

		/// <summary> Calculates and returns the model matrix to go from local to world space. This result is not cached. </summary>
		public Matrix4 GetModelMatrix()
		{
			Matrix4 m = Matrix4.CreateScale(Scale);
			m *= Matrix4.CreateRotationX(Rotation.X * Game.RCF);
			m *= Matrix4.CreateRotationY(Rotation.Y * Game.RCF);
			m *= Matrix4.CreateRotationZ(Rotation.Z * Game.RCF);
			m *= Matrix4.CreateTranslation(Position);
			return m;
		}

		/// <summary> Chainable method to set the scale of this object. </summary>
		public Drawable SetScale(Vector3 Scale)
		{
			this.Scale = Scale;
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public Drawable SetScale(float scale)
		{
			this.Scale = new Vector3(scale, scale, scale);
			return this;
		}

		/// <summary> Chainable method to set the rotation of this object. </summary>
		public Drawable SetRotation(Vector3 Rotation)
		{
			this.Rotation = Rotation;
			return this;
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public Drawable SetPosition(Vector3 Position)
		{
			this.Position = Position;
			return this;
		}

		/// <summary> Creates a flat plane in the XY plane given a texture list. Use scale, rotation, and position to modify this. </summary>
		public static Drawable CreateDrawablePlane(Texture[] textures)
		{
			return new Drawable(new float[]{
				1.0f,  1.0f, 0.0f,   1.0f, 1.0f,   1.0f, 0.0f, 0.0f, 1.0f,
				1.0f, -1.0f, 0.0f,   1.0f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,
				-1.0f, -1.0f, 0.0f,  0.0f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,
				-1.0f,  1.0f, 0.0f,  0.0f, 1.0f,   1.0f, 0.0f, 0.0f, 1.0f,
			}, new uint[]{
				0, 1, 3,
				1, 2, 3
			}, textures);
		}
	}
}