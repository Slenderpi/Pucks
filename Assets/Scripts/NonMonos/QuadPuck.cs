using UnityEngine;

namespace Pucks {
	public class QuadPuck : PuckNode {

		public QuadPuck(int row = 0, int col = 0, EPuckMovementDirection moveDir = EPuckMovementDirection.Stationary) {
			GridPoint = new(row, col);
			PreviousGridPoint = GridPoint;
			MovementDirection = moveDir;
		}

		public override void Move() => GridPoint = MovementDirection switch {
			EPuckMovementDirection.Up => new(GridPoint.x - 1, GridPoint.y),
			EPuckMovementDirection.Down => new(GridPoint.x + 1, GridPoint.y),
			EPuckMovementDirection.Left => new(GridPoint.x, GridPoint.y - 1),
			EPuckMovementDirection.Right => new(GridPoint.x, GridPoint.y + 1),
			_ => throw new UnityException("[PuckNode]: Move() was called on a Puck that shouldn't be moving."),
		};

		public override void OnHitStationaryPuck(PuckNode instigated) {
			if (MovementDirection == EPuckMovementDirection.Up || MovementDirection == EPuckMovementDirection.Down) {
				MovementDirection = EPuckMovementDirection.Left;
				instigated.MovementDirection = EPuckMovementDirection.Right;
			} else {
				MovementDirection = EPuckMovementDirection.Up;
				instigated.MovementDirection = EPuckMovementDirection.Down;
			}
		}

	}
}