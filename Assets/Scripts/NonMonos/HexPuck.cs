using UnityEngine;

namespace Pucks {
	public class HexPuck : PuckNode {

		// TODO: replace
		//public HexPuck(int row=0, int col=0, EPuckMovementDirection moveDir=EPuckMovementDirection.Stationary) {
		//	GridPoint = new(row, col);
		//	PreviousGridPoint = GridPoint;
		//	MovementDirection = moveDir;
		//}

		// TODO: replace
		//public override void Move() {
		//	// TODO
		//	// The below code is stuff just to make it different from QuadPuck. It is not an actual HexPuck implementation.
		//	GridPoint = MovementDirection switch {
		//		EPuckMovementDirection.Up => new(GridPoint.x - 1, GridPoint.y),
		//		EPuckMovementDirection.Down => new(GridPoint.x + 1, GridPoint.y),
		//		EPuckMovementDirection.Left => new(GridPoint.x, GridPoint.y - 1),
		//		EPuckMovementDirection.Right => new(GridPoint.x, GridPoint.y + 1),
		//		_ => throw new UnityException("[PuckNode]: Move() was called on a Puck that shouldn't be moving."),
		//	};
		//}

		// TODO: replace
		//public override void OnHitStationaryPuck(PuckNode instigated) {
		//	// TODO
		//	// The below code is stuff just to make it different from QuadPuck. It is not an actual HexPuck implementation.
		//	switch (MovementDirection) {
		//		case EPuckMovementDirection.Up:
		//			MovementDirection = EPuckMovementDirection.Down;
		//			instigated.MovementDirection = EPuckMovementDirection.Right;
		//			break;
		//		case EPuckMovementDirection.Right:
		//			MovementDirection = EPuckMovementDirection.Left;
		//			instigated.MovementDirection = EPuckMovementDirection.Down;
		//			break;
		//		case EPuckMovementDirection.Down:
		//			MovementDirection = EPuckMovementDirection.Up;
		//			instigated.MovementDirection = EPuckMovementDirection.Left;
		//			break;
		//		case EPuckMovementDirection.Left:
		//			MovementDirection = EPuckMovementDirection.Right;
		//			instigated.MovementDirection = EPuckMovementDirection.Up;
		//			break;
		//	}
		//}

	}
}
