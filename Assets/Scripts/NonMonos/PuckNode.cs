using Unity.Mathematics;
using UnityEngine;

namespace Pucks {
    public class PuckNode {
        /// <summary>
        /// IMPORTANT: stored as (x, y) = (row, col)
        /// </summary>
        public Vector2Int GridPosition;
        public bool IsStationary => MovementDirection == EPuckMovementDirection.Stationary;

        //uint _state;
        public EPuckMovementDirection MovementDirection;

        public PuckNode(int row=0, int col=0, EPuckMovementDirection moveDir=EPuckMovementDirection.Stationary) {
            GridPosition = new(row, col);
            MovementDirection = moveDir;
        }

        public void Destroy() {
            // TODO: destroy linked GameObject?
        }
    }
}
