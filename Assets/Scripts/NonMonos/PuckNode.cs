using UnityEngine;

namespace Pucks {
	public class PuckNode {

		public int Id { get; }
		/// <summary>
		/// IMPORTANT: stored as (x, y) = (row, col)
		/// </summary>
		public Vector2Int GridPosition {
			get => _gridPos;
			set {
				PreviousGridPosition = GridPosition;
				_gridPos = value;
			}
		
		}
		public Vector2Int PreviousGridPosition { get; private set; }

		Vector2Int _gridPos;
		
		/// <summary>
		/// Determines if this Puck is stationary or moving up/down/left/right.
		/// </summary>
		public EPuckMovementDirection MovementDirection;

		public bool IsStationary => MovementDirection == EPuckMovementDirection.Stationary;



		public PuckNode(int row=0, int col=0, EPuckMovementDirection moveDir=EPuckMovementDirection.Stationary) {
			GridPosition = new(row, col);
			PreviousGridPosition = GridPosition;
			MovementDirection = moveDir;
			Id = GenerateId();
		}

		/// <summary>
		/// Make this Puck update its GridPosition based on its current MovementDirection.
		/// </summary>
		/// <exception cref="UnityException">If the MovementDirection is not Up, Down, Left, or Right.</exception>
		public void Move() {
			switch (MovementDirection) {
				case EPuckMovementDirection.Up:
					GridPosition = new(GridPosition.x - 1, GridPosition.y);
					break;
				case EPuckMovementDirection.Down:
					GridPosition = new(GridPosition.x + 1, GridPosition.y);
					break;
				case EPuckMovementDirection.Left:
					GridPosition = new(GridPosition.x, GridPosition.y - 1);
					break;
				case EPuckMovementDirection.Right:
					GridPosition = new(GridPosition.x, GridPosition.y + 1);
					break;
				default:
					throw new UnityException("[PuckNode]: Move() was called on a Puck that shouldn't be moving.");
			}
		}

		/// <summary>
		/// Returns the split type that would result if this moving Puck hit a stationary Puck.
		/// </summary>
		public EPuckMovementDirection GetSplitDirection() =>
			MovementDirection == EPuckMovementDirection.Up || MovementDirection == EPuckMovementDirection.Down
			? EPuckMovementDirection.SplitHorizontal
			: EPuckMovementDirection.SplitVertical;

		static int _id_DoNotTouch = 1;
		protected static int GenerateId() {
			_id_DoNotTouch = (_id_DoNotTouch + 1) % 10000000;
			return _id_DoNotTouch;
		}

	}
}
