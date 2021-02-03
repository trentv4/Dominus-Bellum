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

namespace DominusCore
{
	class Drawable
	{
		public List<Drawable> children = new List<Drawable>();
		public bool isEnabled = true;

		public Drawable() { }

		public void Draw()
		{
			if (!isEnabled) return;
			foreach (Drawable child in children)
			{
				child.Draw();
			}
			DrawSelf();
		}

		public virtual void DrawSelf() { }

		public Drawable AddChild(Drawable child)
		{
			children.Add(child);
			return this;
		}

		public Drawable AddChildren(params Drawable[] children)
		{
			this.children.AddRange(children);
			return this;
		}

		public Drawable SetEnabled(bool state)
		{
			isEnabled = state;
			return this;
		}
	}

	/// <summary> Storage format for non-animated models. This does not preserve vertex data in CPU memory after upload. 
	/// <br/> Provides methods to bind, draw, and dispose.</summary>
	internal class Model : Drawable, IDisposable
	{
		private readonly int IndexLength;
		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		private readonly Texture[] textures;

		public Vector3 Scale { get; private set; } = new Vector3(1, 1, 1);
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;

		/// <summary> Creates a drawable object with a given set of vertex data ([xyz][uv][rgba][qrs]).
		/// <br/>This represents a single model with specified data. It may be rendered many times with different uniforms,
		/// but the vertex data will remain static. Usage is hinted as StaticDraw. Both index and vertex data is
		/// discarded immediately after being sent to the GL context.
		/// <br/> !! Warning !! This is not a logical unit and exists on the render thread only! </summary>
		public Model(float[] VertexData, uint[] Indices, params Texture[] textures)
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

		/// <summary> Binds the index and vertex buffers, binds textures, then draws. Does not recurse.
		/// <br/> !! Warning !! This may be performance heavy with large amounts of different models! </summary>
		public override void DrawSelf()
		{
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
		public Model SetScale(Vector3 Scale)
		{
			this.Scale = Scale;
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public Model SetScale(float scale)
		{
			this.Scale = new Vector3(scale, scale, scale);
			return this;
		}

		/// <summary> Chainable method to set the rotation of this object. </summary>
		public Model SetRotation(Vector3 Rotation)
		{
			this.Rotation = Rotation;
			return this;
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public Model SetPosition(Vector3 Position)
		{
			this.Position = Position;
			return this;
		}

		/// <summary> Creates a flat plane in the XY plane given a texture list. Use scale, rotation, and position to modify this. </summary>
		public static Model CreateDrawablePlane(Texture[] textures)
		{
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

		public static Model CreateDrawableCube(Texture[] textures)
		{
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

		public static Model CreateCircle(int density, params Texture[] textures)
		{
			List<float> vertexList = new List<float>();
			vertexList.AddRange(new List<float> { 0f, 0f, 0f, 0.5f, 0.5f, 1f, 0f, 0f, 1f, 0.0f, 0.0f, -1.0f, });
			for (int i = 1; i <= density; i++)
			{
				float angle = Game.RCF * i * (360 / density);
				vertexList.AddRange(new List<float>{
					(float) Math.Cos(angle), (float) Math.Sin(angle), 0f,
					((float) Math.Cos(angle) + 1)/2.0f, ((float) Math.Sin(angle) + 1)/2.0f,
					1f, 0f, 0f, 1f, 0.0f, 0.0f, -1.0f,
				});
			}

			uint[] indexList = new uint[3 * (density)];
			for (uint i = 0; i < density; i++)
			{
				indexList[i * 3 + 0] = 0;
				indexList[i * 3 + 1] = i + 1;
				indexList[i * 3 + 2] = i + 2;
			}
			indexList[indexList.Length - 1] = 1;

			float[] vertList = vertexList.ToArray();
			return new Model(vertList, indexList, textures);
		}
	}

	internal class Light : Drawable
	{
		public Vector3 Position { get; private set; }
		public Vector3 Color { get; private set; }
		public Vector3 Direction { get; private set; }
		float Strength;

		public Light(Vector3 position, Vector3 color, float strength)
		{
			this.Position = position;
			this.Color = color;
			this.Direction = Vector3.Zero;
			this.Strength = strength;
		}

		public Light(Vector3 position, Vector3 color, Vector3 Direction, float strength)
		{
			this.Position = position;
			this.Color = color;
			this.Direction = Direction;
			this.Strength = strength;
		}

		public override void DrawSelf()
		{
			if (Game.CurrentShader != Game.LightingShader) return;
			ShaderProgramLighting shader = Game.LightingShader;
			shader.SetLightUniform(shader.NextLightID, Strength, Position, Color, Direction);
			shader.NextLightID++;
		}

		public Light SetPosition(Vector3 Position)
		{
			this.Position = Position;
			return this;
		}

		public Light SetColor(Vector3 Color)
		{
			this.Color = Color;
			return this;
		}

		public Light SetDirection(Vector3 Direction)
		{
			this.Direction = Direction;
			return this;
		}

		public Light SetStrength(float Strength)
		{
			this.Strength = Strength;
			return this;
		}
	}
}
