using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace DominusCore {
	public class DemoBuilder {
		public static Drawable BuildDemoInterface_IngameTest() {
			Drawable Root = new Drawable();

			Vector2i s = Game.WindowSize;

			InterfaceImage interfaceTest = new InterfaceImage(Texture.CreateTexture("assets/InterfaceTest.png"), Game.RenderPass.InterfaceForeground)
			.SetScale(new Vector3(200f, 200f, 1)).SetPosition(new Vector3(200, 200, 1));
			InterfaceImage background = new InterfaceImage(Texture.CreateTexture("assets/background.jpg"), Game.RenderPass.InterfaceBackground)
			.SetScale(new Vector3(s.X / 2, s.Y / 2, 1)).SetPosition(new Vector3(s.X / 2, s.Y / 2, 1));
			InterfaceString str = new InterfaceString("calibri", "Testing testing 123").SetScale(new Vector2(35)).SetPosition(new Vector2(s.X / 2, 10));

			Root.AddChildren(interfaceTest, background, str);
			return Root;
		}

		public static Drawable BuildDemoScene_TextureTest() {
			float strength = 2f;
			var f = GamepackLoader.LoadLevel("assets/levels/DesertValley");
			return new Drawable().AddChildren(new Drawable[] {
				new Light(new Vector3(-5,5,-5), Vector3.One, strength),
				new Light(new Vector3(5,5,-5), Vector3.One, strength),
				new Light(new Vector3(5,5,5), Vector3.One, strength),
				new Light(new Vector3(-5,5,5), Vector3.One, strength),
				//Model.CreateModelFromFile("assets/truck_grey.glb").SetPosition(new Vector3(20, 4, 3)),
				Model.CreateModelFromHeightmap(f.height, f.diffuse).SetPosition(new Vector3(0,0,0)).SetScale(new Vector3(10f, f.height_scaling, 10f)),
		});
		}
	}
}