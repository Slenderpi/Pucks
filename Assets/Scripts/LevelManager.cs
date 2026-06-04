using Pucks;
using Pucks.Utilities;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class LevelManager : MonoBehaviour {

	/// <summary>
	/// Number of columns in the level grid.
	/// </summary>
	public int WidthCount = 20;
	/// <summary>
	/// Number of rows in the level grid.
	/// </summary>
	public int HeightCount = 16;
	/// <summary>
	/// Physical size of a Puck.
	/// </summary>
	public float PuckSize = 1f;

	public int StepCount => _stepCount;

	/// <summary>
	/// Consists of where the current level's Pucks start at.
	/// </summary>
	List<Vector2Int> _currentLevel = new();
	/// <summary>
	/// The position of the Solution puck
	/// </summary>
	Vector2Int _solutionPosition = new();
	/// <summary>
	///  The direction the Solution puck should move in to solve the current level.
	/// </summary>
	EPuckMovementDirection _solutionDirection = EPuckMovementDirection.Up;
	int _difficulty = 0;

	/// <summary>
	/// Contains all stationary Pucks, indexed by their grid position.
	/// </summary>
	Dictionary<Vector2Int, PuckNode> _stationaryPucks = new();
	/// <summary>
	/// Contains all moving Pucks.
	/// </summary>
	List<PuckNode> _movingPucks = new();
	/// <summary>
	/// Contains all Pucks that have exited the level grid.
	/// </summary>
	List<PuckNode> _exitedPucks = new();

	int _stepCount = 0;

	/*
	no grid variable, only function
	store stationary pucks, moving pucks, and shouldSplit pucks
	store in Dictionary<Vector2Int, PuckNode> _stationaryPucks, _movingPucks
	//array _splittingPucks
	array _exitedPucks

	- # -
	- - -
	# # -

	Set (0, 1) to v:
	- - -
	- v -
	# # -

	after step():
	- - -
	- - -
	# _ -

	after step():
	- - -
	- - -
	| - >

	after step():
	- - -
	^ - -
	- - -

	etc.

	step() {
		foreach Puck p in _exitedPucks:
			move p based on its EPuckMovementDirection
		//for int i = 0 to _movingPucks.Count, i += 2:
		//	Puck pA = _movingPucks[i]
		//	Puck pB = _movingPucks[i + 1]
		//	set pA movement direction one way
		//	set pB movement direction the other way
		//	don't actually move the pucks
		//	add both to _movingPucks
		foreach Puck p in _movingPucks:
			move p based on its EPuckMovementDirection
			if p is now off grid, remove from _movingPucks and add to _exitedPucks
			if new puck position is occupied by stationary (_stationaryPucks.ContainsKey(newPosition)):
				change movement direction of both pucks accordingly
				//move both the statinary puck and p to _splittingPucks
				//	they should be added in pairs
				//set both to appropriate EPuckState.SplitHorizontal or EPuckState.SplitVertical
	}
     */



	private void Start() {
		BindDebugActions();
		GameManager.DebugActions.Enable();
			GenerateLevel(1);
	}

	public void GenerateLevel(int difficulty) {
		_currentLevel.Clear();

		// TODO

		// temporary hardcoded level for testing)
		_currentLevel.Add(new(1, 5));
		_currentLevel.Add(new(3, 5));
		_currentLevel.Add(new(3, 1));
		//_movingPucks.Add(new(1, 5, EPuckMovementDirection.Left));

		_solutionPosition = new(1, 5);
		_solutionDirection = EPuckMovementDirection.Down;

		ResetLevel();
	}

	public void StepLevel() {
		// TODO

		_stepCount++;

		{
			StringBuilder str = new("[LevelManager]: STEP |");
			str.Append(GetLevelString());
			Debug.Log(str.ToString());
		}
	}

	public void ResetLevel() {
		_stepCount = 0;
		ClearLevel();
		foreach (var pos in _currentLevel) {
			PuckNode puck = new(pos.x, pos.y);
			_stationaryPucks.Add(pos, puck);
		}

		{
			StringBuilder str = new("[LevelManager]: RESET |");
			str.Append(GetLevelString());
			Debug.Log(str.ToString());
		}
	}

	/// <summary>
	/// Destroys all Pucks currently in the level.
	/// </summary>
	public void ClearLevel() {
		foreach (var (_, puck) in _stationaryPucks)
			puck.Destroy();
		foreach (var puck in _movingPucks)
			puck.Destroy();
		foreach (var puck in _exitedPucks)
			puck.Destroy();
		_stationaryPucks.Clear();
		_movingPucks.Clear();
		_exitedPucks.Clear();
	}

	/// <summary>
	/// Get the current level as a string representation but still in its StringBuilder form.<br/>
	/// Moving Pucks are also printed with an arrow indicating their travel direction (^, v, &lt;, &gt;)<br/>
	/// <br/>
	/// Example result:<br/>
	/// <code> 
	///      0   1   2   3   4   5   6   7   8   9  10
	///  0   -   #   #   #   &lt;   -   &gt;   -   -   ^   -
	///  1   -   -   -   -   -   -   -   -   -   -   -
	///  2   -   -   #   -   &lt;   -   &gt;   -   -   v   -
	/// </code>
	/// </summary>
	public StringBuilder GetLevelGridStringBuilder() {
		EPuckMovementDirection[,] grid = LevelTo2dArrayDirectionOnly();
		StringBuilder str = new("  ");
		// Print col header
		for (int i = 0; i < WidthCount; i++)
			str.AppendFormat("{0,3}", i);
		str.Append('\n');
		// Print each row
		for (int r = 0; r < HeightCount; r++) {
			str.AppendFormat("{0,2}", r);
			for (int c = 0; c < WidthCount; c++) {
				str.Append("  ").Append(PuckUtil.PuckMovementToChar(grid[r, c]));
			}
			if (r < HeightCount - 1)
				str.Append('\n');
		}
		return str;
	}

	/// <summary>
	/// Get the current level as a string representation.<br/>
	/// Moving Pucks are also printed with an arrow indicating their travel direction (^, v, &lt;, &gt;)<br/>
	/// <br/>
	/// Example result:<br/>
	/// <code> 
	///      0   1   2   3   4   5   6   7   8   9  10
	///  0   -   #   #   #   &lt;   -   &gt;   -   -   ^   -
	///  1   -   -   -   -   -   -   -   -   -   -   -
	///  2   -   -   #   -   &lt;   -   &gt;   -   -   v   -
	/// </code>
	/// </summary>
	public string GetLevelGridString() => GetLevelGridStringBuilder().ToString();

	/// <summary>
	/// Gets level information such as grid size, difficulty, solution, and step count. Format:<br/>
	/// (H: {HeightCount}, W: {WidthCount}) | Solution: (r: {_solPos.x}, c: {_solPos.y}, dir: {solDir}) | Step: {_step}
	/// </summary>
	/// <returns>A StringBuilder containing the information.</returns>
	public StringBuilder GetLevelMetaDataStringBuilder() => new StringBuilder()
			.Append("(H: ").Append(HeightCount)
			.Append(", W: ").Append(WidthCount)
			.Append(") | ")
			.Append("Solution: (r: ").Append(_solutionPosition.x)
			.Append(", c: ").Append(_solutionPosition.y)
			.Append(", dir: ").Append(PuckUtil.PuckMovementToChar(_solutionDirection))
			.Append(") | ")
			.Append("Step: ").Append(_stepCount);

	/// <summary>
	/// Gets level information such as grid size, difficulty, solution, and step count. Format:<br/>
	/// (H: {HeightCount}, W: {WidthCount}) | Solution: (r: {_solPos.x}, c: {_solPos.y}, dir: {solDir}) | Step: {_step}
	/// </summary>
	/// <returns>A StringBuilder containing the information.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetLevelMetaDataString() => GetLevelMetaDataStringBuilder().ToString();

	/// <summary>
	/// Get level information, such as size, step count, and the current level as a string.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetLevelString() => new StringBuilder()
			.Append(GetLevelMetaDataStringBuilder())
			.Append('\n')
			.Append(GetLevelGridStringBuilder())
			.ToString();

	/// <summary>
	/// Print level information, such as size, step count, and the current level.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PrintLevel() => Debug.Log(GetLevelString());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool HasPuckExitedGrid(PuckNode puck) => puck.GridPosition.x < 0 || puck.GridPosition.x >= WidthCount || puck.GridPosition.y < 0 || puck.GridPosition.y >= HeightCount;

	private void BindDebugActions() {
		GameManager.DebugActions.ResetLevel.started += OnResetLevelActionStarted;
		GameManager.DebugActions.StepPucks.started += OnStepPucksActionStarted;
	}

	private void OnResetLevelActionStarted(InputAction.CallbackContext context) {
		Debug.Log("Reset level pressed");
		ResetLevel();
	}

	private void OnStepPucksActionStarted(InputAction.CallbackContext context) {
		if (_movingPucks.Count == 0)
			Debug.LogWarning("[LevelManager]: No moving Pucks to step.");
		StepLevel();
	}

	/// <summary>
	/// Creates a 2d array representing the current level, where each Puck is represented purely by its movement direction.
	/// </summary>
	EPuckMovementDirection[,] LevelTo2dArrayDirectionOnly() {
		EPuckMovementDirection[,] grid = new EPuckMovementDirection[HeightCount, WidthCount];
		foreach (var (pos, puck) in _stationaryPucks) {
			grid[pos.x, pos.y] = puck.MovementDirection;
		}
		foreach (var puck in _movingPucks) {
			if (grid[puck.GridPosition.x, puck.GridPosition.y] == EPuckMovementDirection.Stationary) {
				if (puck.MovementDirection == EPuckMovementDirection.Up || puck.MovementDirection == EPuckMovementDirection.Down)
					grid[puck.GridPosition.x, puck.GridPosition.y] = EPuckMovementDirection.SplitHorizontal;
				else
					grid[puck.GridPosition.x, puck.GridPosition.y] = EPuckMovementDirection.SplitVertical;
			} else {
				// This will still overwrite moving Pucks at the same position.
				grid[puck.GridPosition.x, puck.GridPosition.y] = puck.MovementDirection;
			}
		}
		return grid;
	}

}
