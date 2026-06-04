using System.Runtime.CompilerServices;
using UnityEngine;

namespace Pucks.Utilities.PuckUtils {
	public static class PuckUtil {
		public readonly static char PUCK_CHAR_NONE = ' ';
		public readonly static char PUCK_CHAR_STATIONARY = '#';
		public readonly static char PUCK_CHAR_UP = '^';
		public readonly static char PUCK_CHAR_DOWN = 'v';
		public readonly static char PUCK_CHAR_RIGHT = '>';
		public readonly static char PUCK_CHAR_LEFT = '<';

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static char PuckStateToChar(EPuckState state) => state switch {
		//	EPuckState.None => PUCK_CHAR_NONE,
		//	EPuckState.Puck => PUCK_CHAR_STATIONARY,
		//	EPuckState.MovingUp => PUCK_CHAR_UP,
		//	EPuckState.MovingDown => PUCK_CHAR_DOWN,
		//	EPuckState.MovingRight => PUCK_CHAR_RIGHT,
		//	EPuckState.MovingLeft => PUCK_CHAR_LEFT,
		//	EPuckState.OffScreen => '!',
		//	EPuckState.ShouldSplitUpDown => '|',
		//	EPuckState.ShouldSplitLeftRight => '-',
		//	_ => '?'
		//};

	}
}
