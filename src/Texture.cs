using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace DominusCore {
	/// <summary> Class that wraps and provides helper methods for OpenGL textures and framebuffer textures. </summary>
	internal class Texture {
		/// <summary> Dictionary to associate filesystem links to OpenGL texture IDs to prevent costly reloads. </summary>
		private static readonly Dictionary<string, int> LOADED_TEXTURES = new Dictionary<string, int>();
		/// <summary> OpenGL max anisotropic filtering level, determined by GPU. It shoots for 16x. </summary>
		private readonly float MaxAnisotrophy = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
		/// <summary> OpenGL ID assigned via GL.GenTexture() to this texture. </summary>
		public readonly int TextureID;
		/// <summary> Uniform ID assigned via GL.GetUniformLocation() which corresponds to one specific shader uniform. </summary>
		private readonly int UniformLocation;

		/// <summary> Creates a texture using anisotropic filtering, primarily meant for regular texture use. </summary>
		public Texture(string location, int uniform) {
			UniformLocation = uniform;

			if (LOADED_TEXTURES.ContainsKey(location)) {
				this.TextureID = LOADED_TEXTURES[location];
			} else {
				ImageResult image;
				using (FileStream stream = File.OpenRead(location)) {
					image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				}

				float anisotropicLevel = MathHelper.Clamp(16, 1f, MaxAnisotrophy);

				TextureID = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, TextureID);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, anisotropicLevel);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
							  image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

				LOADED_TEXTURES.Add(location, TextureID);
			}
		}

		/// <summary> Creates a Framebuffer texture, NOT MEANT FOR USE ON MODELS. Does not use anisotropic filtering.
		/// <br/>The colorAttachment parameter is used as an offset from FramebufferAttachment.ColorAttachment0.</summary>
		public Texture(int colorAttachment, int UniformLocation) {
			this.UniformLocation = UniformLocation;

			TextureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, Game.WindowSize.X, Game.WindowSize.Y,
						  0, PixelFormat.Rgba, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + colorAttachment,
									TextureTarget.Texture2D, TextureID, 0);
		}

		/// <summary> Binds the texture to specified texture unit. 
		/// <br/>The textureUnit parameter used as an offset from TextureUnit.Texture0. </summary>
		public void Bind(int textureUnit) {
			GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.Uniform1(UniformLocation, textureUnit);
		}
	}
}