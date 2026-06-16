using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace Pucks.Level {
	/// <summary>
	/// A PuckSimulation is a class that handles the movement, collision detection/logic, etc.
	/// </summary>
	public abstract class PuckSimulation {

		/// <summary>
		/// Number of columns in the level grid.
		/// </summary>
		public int WidthCount = 40;
		/// <summary>
		/// Number of rows in the level grid.
		/// </summary>
		public int HeightCount = 32;

		public int StepCount { get; protected set; }

		public bool HasLevelStarted { get; protected set; }

		public bool HasLevelSpawned { get; private set; }

		public int CurrentDifficulty => Difficulty;

		public int NumMovingPucks => MovingPucks.Count;

		public int NumStationaryPucks => StationaryPucks.Count;

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
		/// Increment Difficulty and generate its level.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool GenerateNextLevel() => GenerateLevel(++Difficulty);

		/// <summary>
		/// Decrement Difficulty and generate its level. Stops at difficulty 0.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool GeneratePrevLevel() => GenerateLevel(Difficulty > 0 ? --Difficulty : Difficulty);

		/// <summary>
		/// Generates a level with a given difficulty, and updates Difficulty.
		/// </summary>
		/// <param name="difficulty"></param>
		/// <returns></returns>
		public abstract bool GenerateLevel(int difficulty);

		/// <summary>
		/// Fills the grid full with Pucks.
		/// Remember to call ClearLevel() if a level already exists.
		/// </summary>
		public abstract void GenerateFilledLevel();

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
		public virtual int Step() {
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
		/// Returns a list of integers where the index represents the manhattan distance of a Puck and the value is
		/// the spawn order value for use in PuckMover.OnSpawned().
		/// </summary>
		public virtual List<int> SpawnLevel() {
			Assert.IsFalse(HasLevelSpawned, "[PuckSimulation]: The level has already been spawned! Call ClearLevel() first before calling SpawnLevel() again.");
			HasLevelSpawned = true;
			// Track manhattan distances for each Puck
			List<int> puckManDists = new();
			foreach (var pos in CurrentLevel) {
				PuckNode puck = new(pos.x, pos.y);
				// TODO
				//int manDist = pos.x + pos.y;
				//int index = puckManDists.BinarySearch(manDist);
				//if (index < 0) {
				//	index = ~index; // insertion point
				//	puckManDists.Insert(index, manDist);
				//}
				int manDist = pos.x + pos.y;
				int index = puckManDists.BinarySearch(manDist);
				if (index < 0) {
					index = ~index; // insertion point
					puckManDists.Insert(index, manDist);
				}
				StationaryPucks.Add(pos, puck);
			}
			return puckManDists;
		}

		/// <summary>
		/// Clears all spawned Pucks. Does not reset the generated level layout.
		/// </summary>
		public virtual void ClearLevel() {
			Assert.IsTrue(HasLevelSpawned, "[PuckSimulation]: The level is already clear. Please call SpawnLevel() first.");
			HasLevelSpawned = false;
			HasLevelStarted = false;
			StepCount = 0;
			StationaryPucks.Clear();
			MovingPucks.Clear();
			ExitedPucks.Clear();
		}

		/// <summary>
		/// Convenience method to re-spawn the level. Internally just calls ClearLevel() and SpawnLevel().
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
			Assert.IsFalse(HasLevelStarted, "[PuckSimulation]: PushPuck() was called when the level has already been started.");
			HasLevelStarted = true;
			TransferPuckFromStationaryToMoving(point).Direction = direction;
			Step();

			//{
			//	StringBuilder str = new($"[LevelManager]: START ({position.x}, {position.y}, {PuckUtil.PuckMovementToChar(direction)}) |");
			//	str.Append(Singleton.GetLevelString());
			//	Debug.Log(str.ToString());
			//}
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
			if (HasLevelSpawned) {
				Debug.LogWarning("[PuckSimulation]: TestGeneratedLevel() was called when the level is currently spawned in. The level will be cleared.");
				ClearLevel();
			}
			SpawnLevel();
			PushSolutionPuck();
			// Brute force solution validation by stepping it until completion
			while (NumMovingPucks > 0 || NumStationaryPucks > 0)
				Step();
			return NumStationaryPucks == 0;
		}

		public PuckNode GetPuckAt(Vector3 position, float puckSize) {
			Vector2Int point = PositionToPoint(position, puckSize);
			return StationaryPucks.ContainsKey(point)
				? StationaryPucks[point]
				: null;
		}

		public Dictionary<Vector2Int, PuckNode> GetStationaryPucks() => StationaryPucks;

		public List<PuckNode> GetMovingPucks() => MovingPucks;

		public List<PuckNode> GetExitedPucks() => ExitedPucks;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 PointToPosition(Vector2Int point, float puckSize) => new(
			point.y * puckSize + puckSize * 0.5f,
			(HeightCount - point.x - 1) * puckSize + puckSize * 0.5f,
			0
		);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2Int PositionToPoint(Vector3 position, float puckSize) => new(
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