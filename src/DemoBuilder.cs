using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace DominusCore {
	public class DemoBuilder {

		public static Drawable BuildDemoInterface_MainMenuTest() {
			Drawable Root = new Drawable();
			InterfaceImage img = new InterfaceImage(new Texture("assets/background.jpg"), Game.RenderPass.InterfaceForeground);
			img.SetScale(0.25f);
			img.SetPosition(new Vector3(0.5f, 0.0f, 0.0f));
			Root.AddChild(img);
			return Root;
		}

		public static Drawable BuildDemoInterface_IngameTest() {
			Drawable Root = new Drawable();
			InterfaceImage interfaceTest = new InterfaceImage(new Texture("assets/InterfaceTest.png"), Game.RenderPass.InterfaceForeground);
			InterfaceImage background = new InterfaceImage(new Texture("assets/background.jpg"), Game.RenderPass.InterfaceBackground);
			InterfaceString str = new InterfaceString("calibri", "Testing testing 123").SetScale(new Vector2(0.1f, 0.25f)).SetPosition(new Vector2(-0.5f, -0.5f));

			Root.AddChildren(interfaceTest, background, str);
			return Root;
		}

		public static Drawable BuildDemoScene_TextureTest() {
			Texture[] tilesTextures = {
					new Texture("assets/tiles_diffuse.jpg"),
					new Texture("assets/tiles_gloss.jpg"),
					new Texture("assets/tiles_ao.jpg"),
					new Texture("assets/tiles_normal.jpg"),
					new Texture("assets/tiles_height.jpg")
			};
			Model circle = Model.CreateCircle(90, tilesTextures).SetPosition(new Vector3(20, 4, 5)).SetScale(1.0f);
			Model plane = Model.CreateDrawablePlane(tilesTextures).SetPosition(new Vector3(20, 2, 10)).SetScale(10.0f);
			Model cube = Model.CreateDrawableCube(tilesTextures).SetPosition(new Vector3(20, 4, 5)).SetScale(new Vector3(0.25f, 0.25f, 0.25f));
			Model lightCube = Model.CreateDrawableCube(new Texture[] {
					new Texture("assets/tiles_blank.png"),
					new Texture("assets/tiles_blank.png"),
					new Texture("assets/tiles_blank.png"),
					new Texture("assets/tiles_blank.png"),
					new Texture("assets/tiles_blank.png")
				}).SetPosition(new Vector3(20, 5, 6)).SetScale(0.25f);

			Light[] SceneLights = new Light[] {
				new Light(new Vector3(10f, 5f, 6f), Vector3.One, 3f),
				new Light(new Vector3(20f, 5f, -10f), new Vector3(0.0f, 0.5f, 1.0f), 2f),
				new Light(new Vector3(20f, 0f, 6f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1, 0, -1), 5.5f),
			};

			int density = 10;
			List<float> vertexData = new List<float>();
			for (float x = 0; x < density; x++) {
				for (float y = 0; y < density; y++) {
					float value = ((float)Math.Sin(x / 4) + (float)Math.Sin(y / 4)) / 5;
					vertexData.AddRange(new float[] { x / density, value, y / density, x / density, y / density, 1, 0, 0, 1, 0, 0, -1 });
				}
			}
			List<uint> indices = new List<uint>();
			for (int x = 0; x < density - 1; x++) {
				for (int y = 0; y < density - 1; y++) {
					uint value = (uint)(x + (y * density));
					uint udensity = (uint)density;
					indices.AddRange(new uint[] { value, value + 1, value + udensity });
					indices.AddRange(new uint[] { value + 1, value + udensity, value + 1 + udensity });
				}
			}

			Model a = new Model(vertexData.ToArray(), indices.ToArray(), tilesTextures)
			.SetPosition(new Vector3(20, -5, 0)).SetScale(20.0f);

			Model b = Model.CreateModelFromFile("assets/truck_grey.glb").SetPosition(new Vector3(20, 4, 3));

			Drawable SceneRoot = new Drawable();
			SceneRoot.AddChildren(plane, lightCube, circle);
			SceneRoot.AddChildren(SceneLights);
			SceneRoot.AddChildren(a, b);
			return SceneRoot;
		}
	}
}