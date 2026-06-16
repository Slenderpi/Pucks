using UnityEngine;

namespace Pucks {
	public class PuckNode {

		public EPuckType Type = EPuckType.Quad;

		/// <summary>
		/// IMPORTANT: stored as (x, y) = (row, col)
		/// </summary>
		public Vector2Int GridPoint {
			get => GridPos;
			set {
				PreviousGridPoint = GridPoint;
				GridPos = value;
			}
		
		}

		public Vector2Int Direction;

		public Vector2Int PreviousGridPoint { get; protected set; }

		protected Vector2Int GridPos;

		public bool IsMoving { get => _isMoving; set => _isMoving = value; }
		bool _isMoving = false;

		/// <summary>
		/// Determines if this Puck is stationary or moving up/down/left/right.
		/// </summary>
		// TODO: replace
		//public EPuckMovementDirection MovementDirection = EPuckMovementDirection.Stationary;

		// TODO: replace
		//public bool IsStationary => MovementDirection == EPuckMovementDirection.Stationary;

		// TODO: replace
		//public bool IsMoving => MovementDirection switch {
		//	EPuckMovementDirection.Up or EPuckMovementDirection.Down or EPuckMovementDirection.Left or EPuckMovementDirection.Right => true,
		//	_ => false
		//};



		///// <summary>
		///// Initialize default Puck at (0, 0) in Stationary state.
		///// </summary>
		// TODO: replace
		//public PuckNode() {
		//	GridPoint = new(0, 0);
		//	PreviousGridPoint = new(0, 0);
		//	MovementDirection = EPuckMovementDirection.Stationary;
		//}

		public PuckNode(int x=0, int y=0, Vector2Int direction=default) {
			GridPoint = new(x, y);
			PreviousGridPoint = GridPoint;
			Direction = direction;
			if (direction.x != 0 || direction.y != 0)
				IsMoving = true;
		}

		//public PuckNode(int row=0, int col=0, EPuckMovementDirection moveDir=EPuckMovementDirection.Stationary) {
		//	GridPoint = new(row, col);
		//	PreviousGridPoint = GridPoint;
		//	MovementDirection = moveDir;
		//}

		// TODO: replace
		///// <summary>
		///// Make this Puck update its GridPosition based on its current MovementDirection.
		///// </summary>
		///// <exception cref="UnityException">If the MovementDirection is not Up, Down, Left, or Right.</exception>
		//public abstract void Move();

		// TODO: replace
		///// <summary>
		///// This method handles Puck collision. Should only be called on a moving Puck.
		///// </summary>
		///// <param name="instigated"></param>
		//public abstract void OnHitStationaryPuck(PuckNode instigated);

	}
}
