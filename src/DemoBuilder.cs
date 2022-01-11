using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace DominusCore {
	public class DemoBuilder {
		public static Drawable BuildDemoInterface_IngameTest() {
			Drawable Root = new Drawable();

			float x = Game.WindowSize.X;
			float y = Game.WindowSize.Y;
			var g = AssetLoader.LoadGamepack("assets/gamepacks/debug");

			InterfaceImage interfaceTest = new InterfaceImage(Texture.CreateTexture(g.InterfaceIngame), Game.RenderPass.InterfaceForeground)
			.SetScale(new Vector3(x / 2, y / 2, 1)).SetPosition(new Vector3(x / 2, y / 2, 1));
			InterfaceImage background = new InterfaceImage(Texture.CreateTexture("assets/background.jpg"), Game.RenderPass.InterfaceBackground)
			.SetScale(new Vector3(x / 2, y / 2, 1)).SetPosition(new Vector3(x / 2, y / 2, 1));
			InterfaceString str = new InterfaceString("calibri", "Testing testing 123").SetScale(new Vector2(35)).SetPosition(new Vector2(x / 2, 10));

			Root.AddChildren(interfaceTest, background, str);
			return Root;
		}

		public static Drawable BuildDemoScene_TextureTest() {
			float strength = 2f;
			var g = AssetLoader.LoadGamepack("assets/gamepacks/debug");
			var f = AssetLoader.LoadLevel("assets/gamepacks/debug/levels/DesertValley");
			return new Drawable().AddChildren(new Drawable[] {
				new Light(new Vector3(-5,5,-5), Vector3.One, strength),
				new Light(new Vector3(5,5,-5), Vector3.One, strength),
				new Light(new Vector3(5,5,5), Vector3.One, strength),
				new Light(new Vector3(-5,5,5), Vector3.One, strength),
				//Model.CreateModelFromFile("assets/truck_grey.glb").SetPosition(new Vector3(20, 4, 3)),
				Model.CreateModelFromHeightmap(f.HeightmapTexture, f.DiffuseTexture).SetPosition(new Vector3(0,0,0)).SetScale(new Vector3(10f, f.HeightScaling, 10f)),
		});
		}
	}
}