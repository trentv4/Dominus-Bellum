using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace DominusCore {
	public class DemoBuilder {

		public static Drawable BuildDemoInterface_MainMenuTest() {
			Drawable Root = new Drawable();
			InterfaceImage img = new InterfaceImage(Texture.CreateTexture("assets/background.jpg"), Game.RenderPass.InterfaceForeground);
			img.SetScale(0.25f);
			img.SetPosition(new Vector3(0.5f, 0.0f, 0.0f));
			Root.AddChild(img);
			return Root;
		}

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
			return new Drawable().AddChildren(new Drawable[] {
				new Light(new Vector3(10f, 5f, 6f), Vector3.One, 3f),
				new Light(new Vector3(20f, 5f, -10f), new Vector3(0.0f, 0.5f, 1.0f), 2f),
				new Light(new Vector3(20f, 0f, 6f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1, 0, -1), 5.5f),
//				Model.CreateModelFromFile("assets/truck_grey.glb").SetPosition(new Vector3(20, 4, 3)),
				Model.CreateModelFromHeightmap("assets/heightmap.png").SetPosition(new Vector3(0,0,0)).SetScale(30f),
		});
		}
	}
}