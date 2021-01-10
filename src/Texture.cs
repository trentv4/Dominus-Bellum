using System;
using System.IO;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace DominusCore
{
	// Class representing a single texture storing only the texture ID
	internal class Texture
	{
		/// <summary> OpenGL max anisotropic filtering level, determined by GPU. It shoots for 16x. </summary>
		private readonly float MaxAnisotrophy = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
		public readonly int TextureID;
		private readonly int ProgramUniform;

		public Texture(string location, int uniform)
		{
			ProgramUniform = uniform;

			ImageResult image;
			using (FileStream stream = File.OpenRead(location))
			{
				image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}

			float anisotropicLevel = MathHelper.Clamp(16, 1f, MaxAnisotrophy);

			TextureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, anisotropicLevel);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
						  image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

		}

		public void Bind(int textureUnit)
		{
			GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.Uniform1(ProgramUniform, textureUnit);
		}
	}

	internal class FramebufferTexture
	{
		public readonly int TextureID;
		public readonly int ProgramUniform;

		public FramebufferTexture(int colorAttachment, int ProgramUniform)
		{
			this.ProgramUniform = ProgramUniform;

			TextureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16, Game.WindowSize.X, Game.WindowSize.Y,
						  0, PixelFormat.Rgba, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + colorAttachment,
									TextureTarget.Texture2D, TextureID, 0);
		}

		public void Bind(int textureUnit)
		{
			GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.Uniform1(ProgramUniform, textureUnit);
		}
	}
}