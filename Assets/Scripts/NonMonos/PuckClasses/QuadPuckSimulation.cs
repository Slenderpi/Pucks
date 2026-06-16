using Slenderpi.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pucks.Level.Quad {
	public class QuadPuckSimulation : PuckSimulation {

		enum EPuckSpotState {
			None,
			Up,
			Down,
			Left,
			Right,
			SplitHorizontal,
			SplitVertical,
			Claimed
		}

		public static Vector2Int UpDirection => new(-1, 0);
		public static Vector2Int DownDirection => new(1, 0);
		public static Vector2Int LeftDirection => new(0, -1);
		public static Vector2Int RightDirection => new(0, 1);

		public override void GenerateFilledLevel() {
			Difficulty = -1;
			for (int r = 0; r < HeightCount; r++)
				for (int c = 0; c < WidthCount; c++)
					CurrentLevel.Add(new(r, c));
			SolutionPosition = new(0, 0);
			SolutionDirection = new(0, 1);
		}

		public override bool GenerateLevel(int difficulty) {
			CurrentLevel.Clear();

			Dictionary<Vector2Int, EPuckSpotState> chosenPositions = new();
			Vector2Int lastPoint = new(UnityEngine.Random.Range(2, HeightCount - 3), UnityEngine.Random.Range(2, WidthCount - 3));
			// If |, that means it desires an instigator of ^ or v, and that its children to hit are < and >. That is, we create <|> or ^-v
			EPuckSpotState lastSplitDir = Util.UnityRandomBool() ? EPuckSpotState.SplitHorizontal : EPuckSpotState.SplitVertical;
			// A list of spots in the current row/col that a new sibling and parent will can be placed at.
			List<int> availableSpots = new(Math.Max(WidthCount, HeightCount));

			//Debug.Log($"[LevelManager]: GENERATE BEGIN | lastPoint: {lastPoint} | lastSplitDir: {PuckUtil.PuckMovementToChar(lastSplitDir)}");

			chosenPositions.Add(lastPoint, lastSplitDir);
			for (int i = difficulty; i > 0; i--) {
				Vector2Int sibling;
				EPuckSpotState siblingInputHitDir;
				Vector2Int parent;
				EPuckSpotState parentSplitDir;

				availableSpots.Clear();
				int lpindex = 0;
				if (lastSplitDir == EPuckSpotState.SplitHorizontal) {
					lpindex = DetermineHorizontalRangeNEW(lastPoint, chosenPositions, availableSpots);
					if (lpindex <= 1 && availableSpots.Count - lpindex <= 1) {
						Debug.LogWarning($"Can't fit iteration {i}! lastPoint: {lastPoint} | lastSplitDir: '{PuckSpotStateToChar(lastSplitDir)}' | availableSpots ({lpindex}): [{string.Join(", ", availableSpots)}]");
						lastSplitDir = EPuckSpotState.SplitVertical;
						return false;
					}

					// If (leftUnusable) use right
					// else if (rightUnusable) use left
					// else use random
					bool useLeftSide = lpindex <= 1 ? false : availableSpots.Count - lpindex <= 1 ? true : Util.UnityRandomBool();
					//bool useLeftSide = false;
					if (useLeftSide) {
						int siblingColToUseIndex = UnityEngine.Random.Range(1, lpindex - 1);
						int parentColToUseIndex = UnityEngine.Random.Range(0, siblingColToUseIndex - 1);
						sibling = new(lastPoint.x, availableSpots[siblingColToUseIndex]);
						siblingInputHitDir = EPuckSpotState.Left;
						parent = new(lastPoint.x, availableSpots[parentColToUseIndex]);
						// Fill in between spots as Claimed
						// ... from lastPoint to parent
						for (int j = 0; j < parentColToUseIndex; j++)
							chosenPositions.Add(new(lastPoint.x, availableSpots[j]), EPuckSpotState.Claimed);
						// ... then from parent to sibling
						for (int j = parentColToUseIndex + 1; j < siblingColToUseIndex; j++)
							chosenPositions.Add(new(lastPoint.x, availableSpots[j]), EPuckSpotState.Claimed);
					} else {
						int siblingColToUseIndex = UnityEngine.Random.Range(lpindex + 1, availableSpots.Count - 1);
						int parentColToUseIndex = UnityEngine.Random.Range(lpindex, siblingColToUseIndex - 1);
						sibling = new(lastPoint.x, availableSpots[siblingColToUseIndex]);
						siblingInputHitDir = EPuckSpotState.Right;
						parent = new(lastPoint.x, availableSpots[parentColToUseIndex]);
						// Fill in between spots as Claimed
						// ... from lastPoint to parent
						for (int j = lpindex; j < parentColToUseIndex; j++)
							chosenPositions.Add(new(lastPoint.x, availableSpots[j]), EPuckSpotState.Claimed);
						// ... then from parent to sibling
						for (int j = parentColToUseIndex + 1; j < siblingColToUseIndex; j++)
							chosenPositions.Add(new(lastPoint.x, availableSpots[j]), EPuckSpotState.Claimed);
					}
					parentSplitDir = EPuckSpotState.SplitVertical;
					chosenPositions[lastPoint] = useLeftSide ? EPuckSpotState.Right : EPuckSpotState.Left;
				} else {
					lpindex = DetermineVerticalRangeNEW(lastPoint, chosenPositions, availableSpots);
					if (lpindex <= 1 && availableSpots.Count - lpindex <= 1) {
						Debug.LogWarning($"Can't fit iteration {i}! lastPoint: {lastPoint} | lastSplitDir: '{PuckSpotStateToChar(lastSplitDir)}' | availableSpots ({lpindex}): [{string.Join(", ", availableSpots)}]");
						return false;
					}

					// If (upUnusable) use down
					// else if (downUnusable) use up
					// else use random
					bool useUpSide = lpindex <= 1 ? false : availableSpots.Count - lpindex <= 1 ? true : Util.UnityRandomBool();
					//bool useUpSide = true;
					if (useUpSide) {
						int siblingRowToUseIndex = UnityEngine.Random.Range(1, lpindex - 1);
						int parentColToUseIndex = UnityEngine.Random.Range(0, siblingRowToUseIndex - 1);
						sibling = new(availableSpots[siblingRowToUseIndex], lastPoint.y);
						siblingInputHitDir = EPuckSpotState.Up;
						parent = new(availableSpots[parentColToUseIndex], lastPoint.y);
						// Fill in between spots as Claimed
						// ... from lastPoint to parent
						for (int j = 0; j < parentColToUseIndex; j++)
							chosenPositions.Add(new(availableSpots[j], lastPoint.y), EPuckSpotState.Claimed);
						// ... then from parent to sibling
						for (int j = parentColToUseIndex + 1; j < siblingRowToUseIndex; j++)
							chosenPositions.Add(new(availableSpots[j], lastPoint.y), EPuckSpotState.Claimed);
					} else {
						int siblingRowToUseIndex = UnityEngine.Random.Range(lpindex + 1, availableSpots.Count - 1);
						int parentColToUseIndex = UnityEngine.Random.Range(lpindex, siblingRowToUseIndex - 1);
						sibling = new(availableSpots[siblingRowToUseIndex], lastPoint.y);
						siblingInputHitDir = EPuckSpotState.Down;
						parent = new(availableSpots[parentColToUseIndex], lastPoint.y);
						// Fill in between spots as Claimed
						// ... from lastPoint to parent
						for (int j = lpindex; j < parentColToUseIndex; j++)
							chosenPositions.Add(new(availableSpots[j], lastPoint.y), EPuckSpotState.Claimed);
						// ... then from parent to sibling
						for (int j = parentColToUseIndex + 1; j < siblingRowToUseIndex; j++)
							chosenPositions.Add(new(availableSpots[j], lastPoint.y), EPuckSpotState.Claimed);
					}
					parentSplitDir = EPuckSpotState.SplitHorizontal;
					chosenPositions[lastPoint] = useUpSide ? EPuckSpotState.Down : EPuckSpotState.Up;
				}

				chosenPositions.Add(sibling, siblingInputHitDir);
				chosenPositions.Add(parent, parentSplitDir);

				lastPoint = parent;
				lastSplitDir = parentSplitDir;

				//{
				//	StringBuilder str = new($"[LevelManager]: GENERATE ({difficulty}) | iteration: {i} | availableSpots ({lpindex}): {string.Join(", ", availableSpots)}");
				//	str.Append('\n').Append(GetChosenPositionsAsGridStringBuilder(chosenPositions));
				//	Debug.Log(str.ToString());
				//}
			}

			// Now create answer Puck
			if (lastSplitDir == EPuckSpotState.SplitHorizontal) {
				DetermineHorizontalRange(lastPoint, chosenPositions, out Vector2Int leftRange, out Vector2Int rightRange);
				bool useLeft = Util.UnityRandomBool();
				if (useLeft) {
					SolutionPosition = new(lastPoint.x, UnityEngine.Random.Range(leftRange.x, leftRange.y));
					SolutionDirection = RightDirection;
				} else {
					SolutionPosition = new(lastPoint.x, UnityEngine.Random.Range(rightRange.x, rightRange.y));
					SolutionDirection = LeftDirection;
				}
				if (chosenPositions.ContainsKey(SolutionPosition)) {
					// Try other side
					if (!useLeft) {
						SolutionPosition = new(lastPoint.x, UnityEngine.Random.Range(leftRange.x, leftRange.y));
						SolutionDirection = RightDirection;
					} else {
						SolutionPosition = new(lastPoint.x, UnityEngine.Random.Range(rightRange.x, rightRange.y));
						SolutionDirection = LeftDirection;
					}
					if (chosenPositions.ContainsKey(SolutionPosition)) {
						Debug.LogWarning("[LevelManager]: The final Puck does not have space to be given an answer Puck.");
						return false;
					}
				}
			} else {
				DetermineVerticalRange(lastPoint, chosenPositions, out Vector2Int upRange, out Vector2Int downRange);
				bool useUp = Util.UnityRandomBool();
				if (useUp) {
					SolutionPosition = new(UnityEngine.Random.Range(upRange.x, upRange.y), lastPoint.y);
					SolutionDirection = DownDirection;
				} else {
					SolutionPosition = new(UnityEngine.Random.Range(downRange.x, downRange.y), lastPoint.y);
					SolutionDirection = UpDirection;
				}
				if (chosenPositions.ContainsKey(SolutionPosition)) {
					// Try other side
					if (!useUp) {
						SolutionPosition = new(UnityEngine.Random.Range(upRange.x, upRange.y), lastPoint.y);
						SolutionDirection = DownDirection;
					} else {
						SolutionPosition = new(UnityEngine.Random.Range(downRange.x, downRange.y), lastPoint.y);
						SolutionDirection = UpDirection;
					}
					if (chosenPositions.ContainsKey(SolutionPosition)) {
						Debug.LogWarning("[LevelManager]: The final Puck does not have space to be given an answer Puck.");
						return false;
					}
				}
			}
			chosenPositions.Add(SolutionPosition, lastSplitDir);

			//{
			//	StringBuilder str = new($"[LevelManager]: GENERATE ({difficulty}) | iteration: {0}");
			//	str.Append('\n').Append(GetChosenPositionsAsGridStringBuilder(chosenPositions));
			//	Debug.Log(str.ToString());
			//}

			foreach (var (pos, dir) in chosenPositions)
				if (dir != EPuckSpotState.Claimed)
					CurrentLevel.Add(pos);
			Difficulty = difficulty;
			return true;
		}

		protected override void OnHitStationaryPuck(PuckNode instigator, PuckNode instigated) {
			if (instigator.Direction.x == 0) {
				instigator.Direction = new(1, 0);
				instigated.Direction = new(-1, 0);
			} else {
				instigator.Direction = new(0, 1);
				instigated.Direction = new(0, -1);
			}
		}

		/// <summary>
		/// Fills availableSpots with 
		/// </summary>
		/// <param name="lastPoint"></param>
		/// <param name="chosenPositions"></param>
		/// <param name="availableSpots"></param>
		/// <returns></returns>
		int DetermineHorizontalRangeNEW(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckSpotState> chosenPositions, List<int> availableSpots) {
			int currCol = lastPoint.y - 1;
			int lpindex = 0;
			// Walk left to determine smallest left
			while (currCol >= 0) {
				// Add the spot if there's nothing there or the movementdir there is Claimed
				Vector2Int currPos = new(lastPoint.x, currCol);
				if (!chosenPositions.ContainsKey(currPos)) {
					availableSpots.Add(currCol);
					lpindex++;
				} else {
					if (chosenPositions[currPos] != EPuckSpotState.Claimed)
						break;
				}
				currCol--;
			}

			// Now walk right
			currCol = lastPoint.y + 1;
			while (currCol < WidthCount) {
				// Add the spot if there's nothing there, end if there is something and it's not just a Claimed
				Vector2Int currPos = new(lastPoint.x, currCol);
				if (!chosenPositions.ContainsKey(currPos)) {
					availableSpots.Add(currCol);
				} else {
					if (chosenPositions[currPos] != EPuckSpotState.Claimed)
						break;
				}
				currCol++;
			}

			return lpindex;
		}

		/// <summary>
		/// Fills availableSpots with 
		/// </summary>
		/// <param name="lastPoint"></param>
		/// <param name="chosenPositions"></param>
		/// <param name="availableSpots"></param>
		/// <returns></returns>
		int DetermineVerticalRangeNEW(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckSpotState> chosenPositions, List<int> availableSpots) {
			int currRow = lastPoint.x - 1;
			int lpindex = 0;
			// Walk up to determine smallest up
			while (currRow >= 0) {
				// Add the spot if there's nothing there or the movementdir there is Claimed
				Vector2Int currPos = new(currRow, lastPoint.y);
				if (!chosenPositions.ContainsKey(currPos)) {
					availableSpots.Add(currRow);
					lpindex++;
				} else {
					if (chosenPositions[currPos] != EPuckSpotState.Claimed)
						break;
				}
				currRow--;
			}

			// Now walk down
			currRow = lastPoint.x + 1;
			while (currRow < HeightCount) {
				// Add the spot if there's nothing there, end if there is something and it's not just a Claimed
				Vector2Int currPos = new(currRow, lastPoint.y);
				if (!chosenPositions.ContainsKey(currPos)) {
					availableSpots.Add(currRow);
				} else {
					if (chosenPositions[currPos] != EPuckSpotState.Claimed)
						break;
				}
				currRow++;
			}

			return lpindex;
		}

		void DetermineHorizontalRange(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckSpotState> chosenPositions, out Vector2Int leftRange, out Vector2Int rightRange) {
			int smallestLeft = lastPoint.y - 1;
			int biggestRight = lastPoint.y + 1;
			// Walk left to determine smallest left
			while (smallestLeft - 1 >= 0 && !chosenPositions.ContainsKey(new(lastPoint.x, smallestLeft - 1)))
				smallestLeft--;
			// Walk right to determine biggest right
			while (biggestRight + 1 < WidthCount && !chosenPositions.ContainsKey(new(lastPoint.x, biggestRight + 1)))
				biggestRight++;
			leftRange = new(smallestLeft, lastPoint.y - 1);
			rightRange = new(lastPoint.y + 1, biggestRight);
		}

		void DetermineVerticalRange(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckSpotState> chosenPositions, out Vector2Int upRange, out Vector2Int downRange) {
			// In the grid, the Y axis expands downward. So going upward means y--, and downward y++
			int highestUp = lastPoint.x - 1;
			int lowestDown = lastPoint.x + 1;
			// Walk up to determine highest up
			while (highestUp - 1 >= 0 && !chosenPositions.ContainsKey(new(highestUp - 1, lastPoint.y)))
				highestUp--;
			// Walk down to determine lowest down
			while (lowestDown + 1 < HeightCount && !chosenPositions.ContainsKey(new(lowestDown + 1, lastPoint.y)))
				lowestDown++;
			upRange = new(highestUp, lastPoint.x - 1);
			downRange = new(lastPoint.x + 1, lowestDown);
		}

		char PuckSpotStateToChar(EPuckSpotState state) => state switch {
			EPuckSpotState.None => ' ',
			EPuckSpotState.Up => '^',
			EPuckSpotState.Down => 'v',
			EPuckSpotState.Left => '<',
			EPuckSpotState.Right => '>',
			EPuckSpotState.SplitHorizontal => '-',
			EPuckSpotState.SplitVertical => '|',
			EPuckSpotState.Claimed => 'x',
			_ => '?'
		};

		char DirectionToChar(Vector2Int dir) {
			if (dir == Vector2Int.zero)
				return '#';
			if (dir.y == 0) {
				return dir.x < 0 ? '^' : 'v';
			} else {
				return dir.y < 0 ? '<' : '>';
			}
		}

		public override char[,] LevelGridTo2dArray() {
			char[,] grid = new char[WidthCount, HeightCount];
			foreach (var (pos, puck) in StationaryPucks) {
				grid[pos.x, pos.y] = DirectionToChar(puck.Direction);
			}
			foreach (var puck in MovingPucks) {
				grid[puck.GridPoint.x, puck.GridPoint.y] = grid[puck.GridPoint.x, puck.GridPoint.y] switch {
					'^' or 'v' or '<' or '>' or '+' => '+',
					'#' => DirectionToChar(puck.Direction) switch {
						'^' or 'v' => '-',
						'<' or '>' => '|',
						_ => '?'
					},
					'|' => '|',
					'-' => '-',
					_ => DirectionToChar(puck.Direction),
				};
			}
			return grid;
		}

	}
}