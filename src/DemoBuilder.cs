using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace DominusCore {
	public class DemoBuilder {
		private static Model heightmap;

		public static Drawable BuildGameplayWorld(GameData d) {
			var f = d.Level;
			if (heightmap == null)
				heightmap = Model.CreateModelFromHeightmap(f.HeightmapTexture, f.DiffuseTexture).SetPosition(new Vector3(0, 0, 0)).SetScale(new Vector3(10f, f.HeightScaling, 10f));

			float strength = 2f;
			return new Drawable().AddChildren(new Drawable[] {
				new Light(new Vector3(-5,5,-5), Vector3.One, strength),
				new Light(new Vector3(5,5,-5), Vector3.One, strength),
				new Light(new Vector3(5,5,5), Vector3.One, strength),
				new Light(new Vector3(-5,5,5), Vector3.One, strength),
				//Model.CreateModelFromFile("assets/truck_grey.glb").SetPosition(new Vector3(20, 4, 3)),
				heightmap
			});
		}

		public static Drawable BuildGameplayInterface(GameData d) {
			return new InterfaceImage(Texture.CreateTexture("assets/background.jpg"), Renderer.RenderPass.InterfaceBackground)
			.SetScale(new Vector3(Program.Renderer.Size.X / 2, Program.Renderer.Size.Y / 2, 1)).SetPosition(new Vector3(Program.Renderer.Size.X / 2, Program.Renderer.Size.Y / 2, 1));
		}
	}
}