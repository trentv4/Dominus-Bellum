using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace DominusCore {
	/// <summary> Wrapper class for the concept of an OpenGL framebuffer. This provides easy methods attaching 
	/// color or depth buffers, accessing them, using or clearing framebuffers, and other utility functions. </summary>
	public class Framebuffer {
		public int FramebufferID { get; private set; }
		public Texture Depth { get; private set; } = null;
		private List<Texture> _bufferTextures = new List<Texture>();

		/// <summary> Creates a generic Framebuffer with no ID and no attachments. </summary>
		public Framebuffer() {
			FramebufferID = GL.GenFramebuffer();
			Use();
		}

		/// <summary> Creates a Framebuffer given an existing buffer ID. Use this to wrap a pre-existing framebuffer. </summary>
		public Framebuffer(int id) {
			FramebufferID = id;
			Use();
		}

		/// <summary> Returns the Texture object that is currently attached to a specified index. </summary>
		public Texture GetAttachment(int attachment) {
			return _bufferTextures[attachment];
		}

		/// <summary> Creates a depth buffer texture with given depth component, then attaches it to this framebuffer. </summary>
		public Framebuffer AddDepthBuffer(PixelInternalFormat depthComponent) {
			Depth = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, Depth.TextureID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, depthComponent, Program.Renderer.Size.X,
						Program.Renderer.Size.Y, 0, PixelFormat.DepthComponent, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
									TextureTarget.Texture2D, Depth.TextureID, 0);
			return this;
		}

		/// <summary> Creates a color buffer of R8G8B8A8_UNORM, then attaches it to this framebuffer. </summary>
		public Framebuffer AddAttachment() {
			return AddAttachment(PixelInternalFormat.Rgba, PixelFormat.Rgba, "");
		}

		/// <summary> Creates a specified number of color buffers of R8G8B8A8_UNORM, then attaches them to this framebuffer. </summary>
		public Framebuffer AddAttachments(int count) {
			for (int i = 0; i < count; i++)
				AddAttachment();
			return this;
		}

		/// <summary> Creates a specified number of color buffers of R8G8B8A8_UNORM, sets a debug label, then attaches it to this framebuffer. </summary>
		public Framebuffer AddAttachments(string[] labeledBuffers) {
			for (int i = 0; i < labeledBuffers.Length; i++) {
				string label = labeledBuffers[i];
				AddAttachment();
				GL.ObjectLabel(ObjectLabelIdentifier.Texture, GetAttachment(i).TextureID, label.Length, label);
			}
			return this;
		}

		/// <summary> Creates a color buffer with specified formats, then attaches it to this framebuffer. </summary>
		public Framebuffer AddAttachment(PixelInternalFormat internalFormat, PixelFormat externalFormat, string label) {
			int attachment = _bufferTextures.Count;
			Texture buffer = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, buffer.TextureID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, Program.Renderer.Size.X,
						Program.Renderer.Size.Y, 0, externalFormat, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + attachment,
									TextureTarget.Texture2D, buffer.TextureID, 0);

			_bufferTextures.Add(buffer);
			if (label != "") {
				GL.ObjectLabel(ObjectLabelIdentifier.Texture, buffer.TextureID, label.Length, label);
			}

			DrawBuffersEnum[] colorAttachments = new DrawBuffersEnum[_bufferTextures.Count];
			for (int i = 0; i < _bufferTextures.Count; i++) {
				colorAttachments[i] = DrawBuffersEnum.ColorAttachment0 + i;
			}

			GL.DrawBuffers(colorAttachments.Length, colorAttachments);
			return this;
		}

		/// <summary> Performs a screen-size blit from the specified framebuffer to this, using the specified mask. </summary>
		public Framebuffer BlitFrom(Framebuffer from, ClearBufferMask mask) {
			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, from.FramebufferID);
			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, FramebufferID);
			GL.BlitFramebuffer(0, 0, Program.Renderer.Size.X, Program.Renderer.Size.Y, 0, 0, Program.Renderer.Size.X, Program.Renderer.Size.Y,
								mask, BlitFramebufferFilter.Nearest);
			return this;

		}

		/// <summary> Binds this framebuffer and updates Renderer.CurrentBuffer. </summary>
		public Framebuffer Use() {
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferID);
			return this;
		}

		/// <summary> Clears this framebuffer's color and depth buffers. </summary>
		public Framebuffer Reset() {
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			return this;
		}
	}
}