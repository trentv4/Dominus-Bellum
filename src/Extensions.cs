using System.Collections.Generic;

namespace DominusCore {
	public static class Extensions {
		public static string Get(this Dictionary<string, string> dict, string key) {
			return dict.GetValueOrDefault(key);
		}
	}
}