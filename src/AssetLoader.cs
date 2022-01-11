using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DominusCore {
	public class GamepackLoader {
		public struct Level {
			public string diffuse;
			public string height;
			public float height_scaling;
			public int max_players;
			public int[][] player_spawns;
			public string map_name;
			public string author;
			public string date;
			public Dictionary<string, string> key_values;
		}

		private static Regex _categoryRegex = new Regex(@"(.+[\[\]])", RegexOptions.Compiled);
		private static Regex _keyRegex = new Regex(@"(.+)(?=\=)", RegexOptions.Compiled);

		public static Level LoadLevel(string directory) {
			string[] ini = new StreamReader($"{directory}/level.ini").ReadToEnd().Split(new string[] {
				"\r", "\n", "\r\n"
			}, StringSplitOptions.RemoveEmptyEntries);

			string currentCategory = "";
			Dictionary<string, string> keyValues = new Dictionary<string, string>();

			for (int i = 0; i < ini.Length; i++) {
				// Preprocess all lines to remove comments
				string current = ini[i].Split(";")[0].Trim();

				// Identify if it is a category
				if (_categoryRegex.Match(current).Success) {
					currentCategory = current.Substring(1, current.Length - 2);
					continue;
				}

				// Identify if this is a key-value
				if (_keyRegex.Match(current).Success) {
					string[] keyValue = current.Split("=");
					keyValues.Add($"{currentCategory}.{keyValue[0].Trim()}", $"{keyValue[1].Trim()}");
				}
			}

			try {
				// Look through all key-value pairs and assign them to the appropriate Level data
				int playerCount = int.Parse(keyValues.GetValueOrDefault("gameplay.max_players"));
				int[][] spawns = new int[playerCount][];
				for (int i = 1; i <= playerCount; i++) {
					string[] coords = keyValues.GetValueOrDefault($"gameplay.spawn_{i}").Split(",");
					spawns[i - 1] = new int[] {
						int.Parse(coords[0]), int.Parse(coords[1])
					};
				}
				return new Level() {
					diffuse = $"{directory}/{keyValues.GetValueOrDefault("geometry.diffuse")}",
					height = $"{directory}/{keyValues.GetValueOrDefault("geometry.height")}",
					height_scaling = float.Parse(keyValues.GetValueOrDefault("geometry.height_scaling")),
					max_players = playerCount,
					player_spawns = spawns,
					map_name = keyValues.GetValueOrDefault("authorship.name"),
					author = keyValues.GetValueOrDefault("authorship.author"),
					date = keyValues.GetValueOrDefault("authorship.date"),
					key_values = keyValues,
				};
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
				Game.Exit();
			}
			return new Level(); // Never reached
		}
	}

	public class ModelLoader {

	}
}
