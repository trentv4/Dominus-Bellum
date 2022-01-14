using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace DominusCore {
	public class Scene : Drawable {
		public Drawable Geometry = new Drawable();
		public Drawable Interface = new Drawable();

		public void Update(GameData d) {
			foreach (string s in d.EventQueue) {
				Console.WriteLine($"Handling event \"{s}\"");
				if (s == "RegenerateLevel") {
					Geometry = new Drawable();
					Geometry.AddChild(
							Model.CreateModelFromHeightmap(d.Level.HeightmapTexture, d.Level.DiffuseTexture)
							.SetPosition(new Vector3(0, 0, 0)).SetScale(new Vector3(10f, d.Level.HeightScaling, 10f)), "heightmap");
					Geometry.AddChildren(new Drawable[] {
						new Light(new Vector3(-5,5,-5), new Vector3(1.0f, 0.0f, 0.0f), 2f),
						new Light(new Vector3(5,5,-5), new Vector3(0.0f, 1.0f, 0.0f), 2f),
						new Light(new Vector3(5,5,5), new Vector3(0.0f, 0.0f, 1.0f), 2f),
						new Light(new Vector3(-5,5,5), new Vector3(1.0f, 0.0f, 1.0f), 2f),
						new Light(new Vector3(0,5,0), new Vector3(1.0f, 1.0f, 1.0f), 2f),
					});
				} // RegenerateLevel
				if (s == "UpdateInterface") {
					Interface = new InterfaceImage(Texture.CreateTexture("assets/background.jpg"), Renderer.RenderPass.InterfaceBackground)
					.SetScale(new Vector3(Program.Renderer.Size.X / 2, Program.Renderer.Size.Y / 2, 1)).SetPosition(new Vector3(Program.Renderer.Size.X / 2, Program.Renderer.Size.Y / 2, 1));
				}
			}
		}
	}
}