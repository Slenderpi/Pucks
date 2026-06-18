using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Pucks.Utilities {
	public static class PuckUtil {
		public readonly static char PUCK_CHAR_NONE = ' ';
		public readonly static char PUCK_CHAR_STATIONARY = '#';
		public readonly static char PUCK_CHAR_UP = '^';
		public readonly static char PUCK_CHAR_DOWN = 'v';
		public readonly static char PUCK_CHAR_RIGHT = '>';
		public readonly static char PUCK_CHAR_LEFT = '<';
		public readonly static char PUCK_CHAR_SPLIT_HORIZONTAL = '-';
		public readonly static char PUCK_CHAR_SPLIT_VERTICAL = '|';
		public readonly static char PUCK_CHAR_CLAIMED = 'x';

		/// <summary>
		/// Convenient method to add a puck to a &lt;position, puck&gt; dictionary.
		/// </summary>
		/// <param name="dict"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add(this Dictionary<Vector2Int, PuckNode> dict, PuckNode puck) {
			dict.Add(puck.GridPoint, puck);
		}
	}
}
