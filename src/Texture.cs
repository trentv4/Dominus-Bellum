using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using System.Collections.Generic;
using System;
using System.Drawing;

namespace DominusCore {
	/// <summary> Wrapper class for OpenGL textures. This allows for loading textures multiple times by retreiving loaded textures from cache. </summary>
	public class Texture {
		/// <summary> OpenGL texture ID, retreived from a GL call. This changes from execution to execution. </summary>
		public readonly int TextureID;

		private static readonly Dictionary<string, int> _textureCache = new Dictionary<string, int>();
		/// <summary> Parameter controlling texture anisotropic filtering. This is set to 16 all the time, if supported by the GPU. </summary>
		private static readonly float _anisotropicLevel = MathHelper.Clamp(16, 1f, GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy));

		public Texture(int TextureID) {
			this.TextureID = TextureID;
		}

		public void Bind() {
			Bind(TextureUnit.Texture0);
		}

		public void Bind(int unit) {
			Bind(TextureUnit.Texture0 + unit);
		}

		public void Bind(TextureUnit unit) {
			GL.ActiveTexture(unit);
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
		}

		/// <summary> Creates a texture with an image loaded from disk. The result is cached. </summary>
		public static Texture CreateTexture(string diskLocation) {
			return CreateTexture(diskLocation, TextureMinFilter.LinearMipmapLinear, TextureWrapMode.Repeat);
		}

		/// <summary> Creates a texture with an image loaded from disk. The result is cached. This allows for specified filtering when resized. </summary>
		public static Texture CreateTexture(string diskLocation, TextureMinFilter filter) {
			return CreateTexture(diskLocation, filter, TextureWrapMode.Repeat);
		}

		/// <summary> Creates a texture with custom settings. The result is cached. </summary>
		public static Texture CreateTexture(string diskLocation, TextureMinFilter filter, TextureWrapMode wrapMode) {
			string cacheName = $"{diskLocation}-{filter.ToString()}";
			if (_textureCache.ContainsKey(cacheName)) {
				return new Texture(_textureCache[cacheName]);
			}

			StbImage.stbi__vertically_flip_on_load = 1;
			ImageResult image;
			using (FileStream stream = File.OpenRead(diskLocation)) {
				image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}
			return CreateTexture(cacheName, image, filter, wrapMode);
		}

		/// <summary> Creates a texture with a provided image, filter, and wrap mode. </summary>
		public static Texture CreateTexture(string cacheName, ImageResult image, TextureMinFilter filter, TextureWrapMode wrapMode) {
			if (_textureCache.ContainsKey(cacheName)) {
				return new Texture(_textureCache[cacheName]);
			}
			Texture value = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, value.TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
			GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, _anisotropicLevel);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
							image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

			GL.ObjectLabel(ObjectLabelIdentifier.Texture, value.TextureID, cacheName.Length, cacheName);
			_textureCache.Add(cacheName, value.TextureID);
			return value;
		}

		public static Texture CreateTexture(string cacheName, Assimp.EmbeddedTexture image, TextureMinFilter filter, TextureWrapMode wrapMode) {
			if (_textureCache.ContainsKey(cacheName)) {
				return new Texture(_textureCache[cacheName]);
			}
			Texture value = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, value.TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
			GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, _anisotropicLevel);

			if (image.IsCompressed) {
				Bitmap compressedBitmap = new Bitmap(new MemoryStream(image.CompressedData));
				System.Drawing.Imaging.BitmapData rawBits = compressedBitmap.LockBits(
								new System.Drawing.Rectangle(0, 0, compressedBitmap.Width, compressedBitmap.Height),
								System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
								compressedBitmap.Width, compressedBitmap.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rawBits.Scan0);

				compressedBitmap.UnlockBits(rawBits);
				compressedBitmap.Dispose();
			} else {
				Console.WriteLine($"Uncompressed texture data found in texture ${cacheName}.");
			}

			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			GL.ObjectLabel(ObjectLabelIdentifier.Texture, value.TextureID, cacheName.Length, cacheName);
			_textureCache.Add(cacheName, value.TextureID);
			return value;
		}
	}
}
