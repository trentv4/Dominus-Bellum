using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DominusCore {
	public class AssetLoader {
		private static Regex _categoryRegex = new Regex(@"(.+[\[\]])", RegexOptions.Compiled);
		private static Regex _keyRegex = new Regex(@"(.+)(?=\=)", RegexOptions.Compiled);
		private static Dictionary<string, Gamepack> _cachedGamepacks = new Dictionary<string, Gamepack>();
		private static Dictionary<string, Level> _cachedLevels = new Dictionary<string, Level>();

		public static Gamepack LoadGamepack(string directory) {
			if (_cachedGamepacks.ContainsKey(directory)) return _cachedGamepacks.GetValueOrDefault(directory);
			Dictionary<string, string> d = ReadIni($"{directory}/gamepack.ini");
			try {
				string[] levelList = d.Get("levels.list").Split(",", StringSplitOptions.TrimEntries);
				Gamepack gp = new Gamepack() {
					Name = d.Get("authorship.name"),
					Author = d.Get("authorship.author"),
					Date = d.Get("authorship.date"),
					Version = d.Get("authorship.version"),
					InterfaceIngame = $"{directory}/interface/{d.Get("interface.in_game")}",
					Directory = directory,
					Levels = levelList,
					KeyValues = d,
				};
				_cachedGamepacks.Add(directory, gp);
				return gp;
			} catch (Exception e) {
				Program.Crash(e);
			}
			return new Gamepack(); // Never reached
		}

		public static Level LoadLevel(string directory) {
			if (_cachedLevels.ContainsKey(directory)) return _cachedLevels.GetValueOrDefault(directory);
			Dictionary<string, string> keyValues = ReadIni($"{directory}/level.ini");
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
				Level l = new Level() {
					DiffuseTexture = $"{directory}/{keyValues.GetValueOrDefault("geometry.diffuse")}",
					HeightmapTexture = $"{directory}/{keyValues.GetValueOrDefault("geometry.height")}",
					HeightScaling = float.Parse(keyValues.GetValueOrDefault("geometry.height_scaling")),
					MaxPlayers = playerCount,
					PlayerSpawns = spawns,
					MapName = keyValues.GetValueOrDefault("authorship.name"),
					Author = keyValues.GetValueOrDefault("authorship.author"),
					Date = keyValues.GetValueOrDefault("authorship.date"),
					KeyValues = keyValues,
				};
				_cachedLevels.Add(directory, l);
				return l;
			} catch (Exception e) {
				Program.Crash(e);
			}
			return new Level(); // Never reached
		}

		public static Dictionary<string, string> ReadIni(string filename) {
			string[] ini = new StreamReader(filename).ReadToEnd().Split(new string[] {
				"\r", "\n", "\r\n"
			}, StringSplitOptions.RemoveEmptyEntries);

			string currentCategory = "";
			Dictionary<string, string> keyValues = new Dictionary<string, string>();

			for (int i = 0; i < ini.Length; i++) {
				// Preprocess all lines to remove comments
				string current = ini[i].Split(";")[0].Trim();

				// Identify if it is a category
				if (_categoryRegex.Match(current).Success) {
					currentCategory = $"{current.Substring(1, current.Length - 2)}.";
					continue;
				}

				// Identify if this is a key-value
				if (_keyRegex.Match(current).Success) {
					string[] keyValue = current.Split("=");
					keyValues.Add($"{currentCategory}{keyValue[0].Trim()}", keyValue[1].Trim());
				}
			}

			return keyValues;
		}
	}

	public struct Level {
		public string DiffuseTexture;
		public string HeightmapTexture;
		public float HeightScaling;
		public int MaxPlayers;
		public int[][] PlayerSpawns;
		public string MapName;
		public string Author;
		public string Date;
		public Dictionary<string, string> KeyValues;
	}

	public struct Gamepack {
		public string Name;
		public string Author;
		public string Date;
		public string Version;
		public string InterfaceIngame;
		public string[] Levels;
		public string Directory;
		public Dictionary<string, string> KeyValues;
	}
}
