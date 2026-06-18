using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pucks.Level {
	/// <summary>
	/// A PuckSimulation is a class that handles the movement, collision detection/logic, etc.
	/// </summary>
	public abstract class PuckSimulation {

		/// <summary>
		/// Broadcasted after every Step() call.<br/>
		/// int: numCollisions, the number of moving->stationary collisions this step
		/// </summary>
		public Action<int> A_OnLevelStepped;
		/// <summary>
		/// Broadcasted after GenerateLevel() has finished.
		/// </summary>
		public Action A_OnLevelSpawned;
		/// <summary>
		/// Broadcasted after ClearLevel() is called.
		/// </summary>
		public Action A_OnLevelCleared;
		/// <summary>
		/// Broadcasted when GenerateLevel() generates a level.
		/// </summary>
		public Action A_OnLevelGenerated;
		/// <summary>
		/// Broadcasted if GenerateLevel() fails to generate a level.<br/>
		/// int: difficulty<br/>
		/// int: numGenProcessFails<br/>
		/// int: numUnsovlableFails
		/// </summary>
		public Action<int, int, int> A_OnLevelGenFailed;
		/// <summary>
		/// Broadcast if the level results in the win state.
		/// </summary>
		public Action A_OnLevelWon;
		/// <summary>
		/// Broadcast if the level results in the loss state.
		/// </summary>
		public Action A_OnLevelLost;

		/// <summary>
		/// Number of columns in the level grid.
		/// </summary>
		public int WidthCount = 40;
		/// <summary>
		/// Number of rows in the level grid.
		/// </summary>
		public int HeightCount = 32;

		/// <summary>
		/// The current Step() iteration.
		/// </summary>
		public int StepCount { get; protected set; }

		/// <summary>
		/// Determines if a level has start moving Pucks.
		/// </summary>
		public bool HasLevelStarted { get; protected set; }

		/// <summary>
		/// Determines if a level has been spawned in.<br/>
		/// Levels are first generated via GenerateLevel(), then spawned in via SpawnLevel().
		/// </summary>
		public bool HasLevelSpawned { get; private set; }

		/// <summary>
		/// Determines if the level has reached either the won or lost state.
		/// </summary>
		public bool HasLevelEnded { get; private set; }

		/// <summary>
		/// The Difficulty of the current generated level.
		/// </summary>
		public int CurrentDifficulty => Difficulty;

		/// <summary>
		/// Number of moving Pucks.
		/// </summary>
		public int NumMovingPucks => MovingPucks.Count;

		/// <summary>
		/// Number of stationary Pucks.
		/// </summary>
		public int NumStationaryPucks => StationaryPucks.Count;

		/// <summary>
		/// Number of Pucks that are moving and have exited the level grid.
		/// </summary>
		public int NumExitedPuck => ExitedPucks.Count;

		/// <summary>
		/// Contains all stationary Pucks, indexed by their grid position.
		/// </summary>
		protected readonly Dictionary<Vector2Int, PuckNode> StationaryPucks = new();
		/// <summary>
		/// Contains all moving Pucks.
		/// </summary>
		protected readonly List<PuckNode> MovingPucks = new();
		/// <summary>
		/// Contains all Pucks that have exited the level grid.
		/// </summary>
		protected readonly List<PuckNode> ExitedPucks = new();

		/// <summary>
		/// Consists of where the current level's Pucks start at.
		/// </summary>
		protected readonly List<Vector2Int> CurrentLevel = new();
		/// <summary>
		/// The position of the Solution puck
		/// </summary>
		public Vector2Int SolutionPosition { get; protected set; }
		/// <summary>
		///  The direction the Solution puck should move in to solve the current level.
		/// </summary>
		public Vector2Int SolutionDirection { get; protected set; }
		protected int Difficulty = 0;

		/// <summary>
		/// List of integers where the integer is the spawn order (for use by PuckMovers) and
		/// the index is the manhattan distance of a PuckNode's GridPoint.
		/// </summary>
		public List<int> PuckSpawnOrderList { get; protected set; } = new();



		/// <summary>
		/// Increment Difficulty and generate its level.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GenerateNextLevel() => GenerateLevel(Difficulty + 1);

		/// <summary>
		/// Decrement Difficulty and generate its level. Stops at difficulty 0.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GeneratePrevLevel() => GenerateLevel(Math.Max(Difficulty - 1, 0));

		/// <summary>
		/// Generates a level for the current value of Difficulty. CurrentLevel will already be clear before this function is called.
		/// </summary>
		/// <param name="difficulty"></param>
		/// <returns></returns>
		protected abstract bool GenerateLevel_Implementation();

		/// <summary>
		/// Generates a level for a specific difficulty.<br/>
		/// The generated level will be regenerated if generation fails or if TestGeneratedLevel() fails,
		/// up to a maximum number of tries.<br/>
		/// A_OnLevelGenerated is broadcast if a valid level is generated.<br/>
		/// A_OnLevelGenFailed is broadcast if a valid level cannot be generated within the number of max tries.
		/// </summary>
		/// <param name="difficulty"></param>
		public void GenerateLevel(int difficulty) {
			ClearLevel();
			Difficulty = difficulty;
			int MAX_TRIES = 100;
			int numGenProcessFails = 0;
			int numUnsovlableFails = 0;
			do {
				ClearGeneratedLevel();
				if (!GenerateLevel_Implementation()) {
					numGenProcessFails++;
					continue;
				}
				if (TestGeneratedLevel())
					break;
				numUnsovlableFails++;
			} while (numGenProcessFails + numUnsovlableFails < MAX_TRIES);
			if (numGenProcessFails + numUnsovlableFails > 0) {
				Debug.LogWarning($"[LevelManager]: Level generation failed {numGenProcessFails + numUnsovlableFails} (max {MAX_TRIES}) times for difficulty {difficulty}. Of them, {numGenProcessFails} were generation issues, and {numUnsovlableFails} were from impossible puzzles.");
				if (numGenProcessFails + numUnsovlableFails == MAX_TRIES) {
					ClearLevel();
					A_OnLevelGenFailed?.Invoke(difficulty, numGenProcessFails, numUnsovlableFails);
					if (numGenProcessFails > 0) {
						// Allow level spawning anyway if the only issue was unsolvability. Otherwise, clear generated level.
						ClearGeneratedLevel();
						return;
					}
				}
			}
			A_OnLevelGenerated?.Invoke();
		}

		/// <summary>
		/// Regenerates the current level with the same difficulty.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegenerateLevel() {
			GenerateLevel(Difficulty);
		}

		/// <summary>
		/// Determines how a Puck moves.<br/>
		/// Example: for a square Puck, MovePuck() will simply update its GridPoint by 1 in one of the cardinal directions.<br/>
		/// By default, adds Direction to GridPoint.
		/// </summary>
		/// <param name="p">The Puck to move.</param>
		protected virtual void MovePuck(PuckNode p) {
			p.GridPoint += p.Direction;
		}

		/// <summary>
		/// This method handles Puck collision, in that it modifies instigator and instigated as necessary.
		/// </summary>
		/// <param name="instigated"></param>
		protected abstract void OnHitStationaryPuck(PuckNode instigator, PuckNode instigated);

		public void Step() {
			A_OnLevelStepped?.Invoke(Step_Implementation());
			if (HasLevelEnded)
				return;
			if (IsInWinState()) {
				HasLevelEnded = true;
				A_OnLevelWon?.Invoke();
			} else if (IsInLoseState()) {
				HasLevelEnded = true;
				A_OnLevelLost?.Invoke();
			}
		}

		/// <summary>
		/// Step the level.<br/>
		/// By default, moves Pucks and triggers collisions. Pseudocode:<br/>
		/// <code>
		/// - foreach p in ExitedPucks:
		///	  - MovePuck(p)
		/// - foreach p in MovingPucks:
		///   - MovePuck(p);
		///   - if p hit stationary:
		///     - OnHitStationryPuck(p, instigated);
		///     - TransferPuckFromStationaryToMoving(instigated);
		/// - foreach p in MovingPucks:
		///   - if HasPuckExitedGrid(p):
		///		- Transfer p to from MovingPucks to ExitedPucks
		/// - return number of collisions
		/// </code>
		/// </summary>
		/// <returns>The number of collisions this step.</returns>
		protected virtual int Step_Implementation() {
			foreach (PuckNode p in ExitedPucks) {
				MovePuck(p);
			}
			int numMovingPucks = MovingPucks.Count;
			int numCollisions = 0;
			for (int i = 0; i < numMovingPucks; i++) {
				PuckNode p = MovingPucks[i];
				MovePuck(p);
				if (StationaryPucks.ContainsKey(p.GridPoint)) {
					numCollisions++;
					PuckNode instigated = StationaryPucks[p.GridPoint];
					OnHitStationaryPuck(p, instigated);
					TransferPuckFromStationaryToMoving(instigated);
				}
			}
			for (int i = 0; i < MovingPucks.Count; i++) {
				PuckNode p = MovingPucks[i];
				if (HasPuckExitedGrid(p)) {
					MovingPucks.RemoveAtSwapBack(i--);
					ExitedPucks.Add(p);
				}
			}

			StepCount++;
			return numCollisions;
		}

		/// <summary>
		/// Fills the internal StationaryPucks memory with PuckNodes based on the current generated level layout.<br/>
		/// Ensure you called ClearLevel() before already.<br/>
		/// A_OnLevelSpawned is broadcast at the end.
		/// </summary>
		public void SpawnLevel() {
			Assert.IsFalse(HasLevelSpawned, "[PuckSimulation]: The level has already been spawned! Call ClearLevel() first before calling SpawnLevel() again.");
			if (CurrentLevel.Count == 0) {
				Debug.LogWarning("[PuckSimulation]: SpawnLevel() was called but no level has been generated, possibly due to level generation failure.");
				return;
			}
			HasLevelSpawned = true;
			PuckSpawnOrderList.Clear();
			SpawnLevel_Implementation();
			A_OnLevelSpawned?.Invoke();
		}

		/// <summary>
		/// Fills the internal StationaryPucks memory with PuckNodes based on the current generated level layout.<br/>
		/// Ensure you called ClearLevel() before already.<br/>
		/// This function does not broadcast A_OnLevelSpawned.
		/// </summary>
		protected virtual void SpawnLevel_Implementation() {
			// Track manhattan distances for each Puck
			foreach (var pos in CurrentLevel) {
				PuckNode puck = new(pos.x, pos.y);
				int manDist = pos.x + pos.y;
				int index = PuckSpawnOrderList.BinarySearch(manDist);
				if (index < 0) {
					index = ~index; // insertion point
					PuckSpawnOrderList.Insert(index, manDist);
				}
				StationaryPucks.Add(pos, puck);
			}
		}

		/// <summary>
		/// Clears all spawned Pucks. Does not reset the generated level layout.<br/>
		/// A_OnLevelCleared is broadcast at the end.
		/// </summary>
		public void ClearLevel() {
			ClearLevel_Implementation();
			A_OnLevelCleared?.Invoke();
		}

		/// <summary>
		/// Clears all spawned Pucks. Does not reset the generated level layout.<br/>
		/// This function does not broadcast A_OnLevelCleared at the end.
		/// </summary>
		protected virtual void ClearLevel_Implementation() {
			HasLevelSpawned = false;
			HasLevelStarted = false;
			HasLevelEnded = false;
			StepCount = 0;
			StationaryPucks.Clear();
			MovingPucks.Clear();
			ExitedPucks.Clear();
		}

		/// <summary>
		/// Convenience method to re-spawn the level. Internally just calls ClearLevel() and SpawnLevel().<br/>
		/// Returns a list of integers where the index represents the manhattan distance of a Puck and the value is
		/// the spawn order value for use in PuckMover.OnSpawned().
		/// </summary>
		public void RespawnLevel() {
			if (HasLevelSpawned)
				ClearLevel();
			SpawnLevel();
		}

		/// <summary>
		/// Clears the internally generated level layout.
		/// </summary>
		public virtual void ClearGeneratedLevel() {
			CurrentLevel.Clear();
		}

		/// <summary>
		/// Make a Puck move. This function is what should be called for the Player to move a Puck. You cannot
		/// call this function if the level has already started.<br/>
		/// Step() is called at the end of the function.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="direction"></param>
		public virtual void PushPuck(Vector2Int point, Vector2Int direction) {
			Assert.IsTrue(HasLevelSpawned, "[PuckSimulation]: PushPuck() was called when the level has not yet been spawned. Call SpawnLevel() first.");
			if (HasLevelStarted) {
				Debug.LogWarning("[PuckSimulation]: PushPuck() was called when the level has already been started. No more moves are allowed until a restart.");
				return;
			}
			PushPuck_Implementation(point, direction);
			Step();

			//{
			//	StringBuilder str = new($"[LevelManager]: START ({position.x}, {position.y}, {PuckUtil.PuckMovementToChar(direction)}) |");
			//	str.Append(Singleton.GetLevelString());
			//	Debug.Log(str.ToString());
			//}
		}

		protected virtual void PushPuck_Implementation(Vector2Int point, Vector2Int direction) {
			HasLevelStarted = true;
			TransferPuckFromStationaryToMoving(point).Direction = direction;
		}

		/// <summary>
		/// Calls PushPuck() but with the solution position/direction.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushSolutionPuck() => PushPuck(SolutionPosition, SolutionDirection);

		/// <summary>
		/// Run a full simulation of the generated level.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public bool TestGeneratedLevel() {
			// TODO: create ability to duplicate a PuckSimulation so that we can run the simulation on that one instead
			Assert.IsFalse(HasLevelStarted, "[PuckSimulation]: TestGeneratedLevel() was called when the level already started, which should not occur.") ;
			if (HasLevelSpawned)
				ClearLevel_Implementation();
			SpawnLevel_Implementation();
			PushPuck_Implementation(SolutionPosition, SolutionDirection);
			// Brute force solution validation by stepping it until completion
			while (NumMovingPucks > 0 && NumStationaryPucks > 0)
				Step_Implementation();
			bool success = NumStationaryPucks == 0;
			ClearLevel_Implementation();
			return success;
		}

		public PuckNode GetPuckAt(Vector3 position, float puckSize) {
			Vector2Int point = PositionToPoint(position, puckSize);
			return StationaryPucks.ContainsKey(point)
				? StationaryPucks[point]
				: null;
		}

		/// <summary>
		/// For use by the PlayerSliderController to determine desired Puck direction given a dragVector
		/// from mouse input.<br/>
		/// By default, determines the closest cardinal direction to the drag vector.
		/// </summary>
		/// <param name="dragVector"></param>
		/// <returns></returns>
		public abstract Vector2Int DragVectorToDirection(Vector3 dragVector);

		public Dictionary<Vector2Int, PuckNode> GetStationaryPucks() => StationaryPucks;

		public List<PuckNode> GetMovingPucks() => MovingPucks;

		public List<PuckNode> GetExitedPucks() => ExitedPucks;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual Vector3 PointToPosition(Vector2Int point, float puckSize) => new(
			point.y * puckSize + puckSize * 0.5f,
			(HeightCount - point.x - 1) * puckSize + puckSize * 0.5f,
			0
		);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual Vector2Int PositionToPoint(Vector3 position, float puckSize) => new(
			HeightCount - 1 - Mathf.RoundToInt((position.y - puckSize * 0.5f) / puckSize),
			Mathf.RoundToInt((position.x - puckSize * 0.5f) / puckSize)
		);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected PuckNode TransferPuckFromStationaryToMoving(Vector2Int point) {
			PuckNode p = StationaryPucks[point];
			StationaryPucks.Remove(point);
			MovingPucks.Add(p);
			return p;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void TransferPuckFromStationaryToMoving(PuckNode p) {
			StationaryPucks.Remove(p.GridPoint);
			MovingPucks.Add(p);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool HasPuckExitedGrid(PuckNode puck) => puck.GridPoint.x < 0 || puck.GridPoint.x >= HeightCount || puck.GridPoint.y < 0 || puck.GridPoint.y >= WidthCount;

		/// <summary>
		/// Checks if the win state has been reached, i.e. no more stationary pucks are left.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsInWinState() {
			Assert.IsTrue(HasLevelSpawned, "[PuckSimulation]: IsInWinState() cannot be called before the level has been spawned.");
			Assert.IsTrue(HasLevelStarted, "[PuckSimulation]: IsInWinState() cannot be called before the level has started.");
			return NumStationaryPucks == 0;
		}

		/// <summary>
		/// Checks if the guaranteed loss state has been reached, i.e. no more moving pucks are
		/// left but some stationary ones remain.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsInLoseState() {
			Assert.IsTrue(HasLevelSpawned, "[PuckSimulation]: IsInLoseState() cannot be called before the level has been spawned.");
			Assert.IsTrue(HasLevelStarted, "[PuckSimulation]: IsInLoseState() cannot be called before the level has started.");
			return NumMovingPucks == 0 && NumStationaryPucks > 0;
		}

		/// <summary>
		/// Creates a character representation of the current level state.<br/>
		/// <br/>
		/// Called by GetLevelGridStringBuilder().
		/// </summary>
		/// <returns></returns>
		public abstract char[,] LevelGridTo2dArray();

		/// <summary>
		/// Create a string representation of the level grid. Internally calls LevelGridTo2dArray().
		/// Prints the characters given by that computed array, unless a character is the null character, in which case
		/// it gets replaced with a space.
		/// </summary>
		/// <returns></returns>
		public StringBuilder GetLevelGridStringBuilder() {
			char[,] grid = LevelGridTo2dArray();
			StringBuilder str = new("  ");
			// Print col header
			for (int i = 0; i < WidthCount; i++)
				str.AppendFormat("{0,3}", i);
			str.Append('\n');
			// Print each row
			for (int r = 0; r < HeightCount; r++) {
				str.AppendFormat("{0,2}", r);
				for (int c = 0; c < WidthCount; c++) {
					str.Append("  ").Append(grid[r, c] == '\0' ? ' ' : grid[r, c]);
				}
				if (r < HeightCount - 1)
					str.Append('\n');
			}
			return str;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string GetLevelGridString() => GetLevelGridStringBuilder().ToString();

	}
}