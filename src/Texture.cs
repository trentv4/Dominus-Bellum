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
		// OpenGL max anisotropic filtering level, determined by GPU. It shoots for 16x
		private readonly float MaxAnisotrophy = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
		// OpenGL ID for the current texture
		public readonly int TextureID;
		public readonly TextureUnit Unit;
		private readonly int ProgramUniform;

		public Texture(string location, TextureUnit unit, int uniform)
		{
			Unit = unit;
			ProgramUniform = uniform;

			ImageResult image;
			using (FileStream stream = File.OpenRead(location))
			{
				image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}

			float anisotropicLevel = MathHelper.Clamp(16, 1f, MaxAnisotrophy);

			TextureID = GL.GenTexture();
			Bind();
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, anisotropicLevel);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
						  image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

		}

		public void Bind()
		{
			GL.ActiveTexture(Unit);
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.Uniform1(ProgramUniform, Unit - TextureUnit.Texture0);
		}
	}
}