using UnityEngine;

namespace Pucks {
	public abstract class PuckNode {

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

		public Vector2Int PreviousGridPoint { get; protected set; }

		protected Vector2Int GridPos;
		
		/// <summary>
		/// Determines if this Puck is stationary or moving up/down/left/right.
		/// </summary>
		public EPuckMovementDirection MovementDirection;

		public bool IsStationary => MovementDirection == EPuckMovementDirection.Stationary;

		public bool IsMoving => MovementDirection switch {
			EPuckMovementDirection.Up or EPuckMovementDirection.Down or EPuckMovementDirection.Left or EPuckMovementDirection.Right => true,
			_ => false
		};



		/// <summary>
		/// Initialize default Puck at (0, 0) in Stationary state.
		/// </summary>
		public PuckNode() {
			GridPoint = new(0, 0);
			PreviousGridPoint = new(0, 0);
			MovementDirection = EPuckMovementDirection.Stationary;
		}

		//public PuckNode(int row=0, int col=0, EPuckMovementDirection moveDir=EPuckMovementDirection.Stationary) {
		//	GridPoint = new(row, col);
		//	PreviousGridPoint = GridPoint;
		//	MovementDirection = moveDir;
		//}

		/// <summary>
		/// Make this Puck update its GridPosition based on its current MovementDirection.
		/// </summary>
		/// <exception cref="UnityException">If the MovementDirection is not Up, Down, Left, or Right.</exception>
		public abstract void Move();

		/// <summary>
		/// This method handles Puck collision. Should only be called on a moving Puck.
		/// </summary>
		/// <param name="instigated"></param>
		public abstract void OnHitStationaryPuck(PuckNode instigated);

	}
}
