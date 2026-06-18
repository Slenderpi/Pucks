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



		public PuckNode(int x=0, int y=0, Vector2Int direction=default) {
			GridPoint = new(x, y);
			PreviousGridPoint = GridPoint;
			Direction = direction;
			if (direction.x != 0 || direction.y != 0)
				IsMoving = true;
		}

	}
}
