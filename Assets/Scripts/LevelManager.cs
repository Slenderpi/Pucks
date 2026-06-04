using Pucks;
using Pucks.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour {

	public static LevelManager Singleton;

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

	public float UpdateDelay = 0.5f;

	public Vector3 PositionOffset = new();

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

	bool _hasLevelStarted = false;

	int _stepCount = 0;



	private void Awake() {
		if (!Singleton) {
			Singleton = this;
			DontDestroyOnLoad(gameObject);
		} else if (Singleton != this) {
			Destroy(gameObject);
		}
	}

	private void Start() {
		BindDebugActions();
		GameManager.DebugActions.Enable();
			GenerateLevel(1);
	}

	private void OnDestroy() {
		if (Singleton == this) {
			Singleton = null;
		}
	}

	IEnumerator LevelUpdateCoroutine() {
		while (_movingPucks.Count > 0) {
			yield return new WaitForSeconds(UpdateDelay);
			StepLevel();
		}
	}

	public static PuckNode GetPuckAt(Vector3 position) {
		// TODO
		return null;
	}

	public void GenerateLevel(int difficulty) {
		_currentLevel.Clear();
		_difficulty = difficulty;

		// TODO

		// temporary hardcoded level for testing)
		_currentLevel.Add(new(0, 5));
		_currentLevel.Add(new(1, 5));
		_currentLevel.Add(new(1, 4));
		_currentLevel.Add(new(2, 4));
		_currentLevel.Add(new(2, 5));
		_currentLevel.Add(new(3, 5));
		_currentLevel.Add(new(3, 1));
		//_movingPucks.Add(new(1, 5, EPuckMovementDirection.Left));

		_solutionPosition = new(0, 5);
		_solutionDirection = EPuckMovementDirection.Down;

		ResetLevel();
	}

	public void StepLevel() {
		foreach (PuckNode p in _exitedPucks) {
			p.Move();
		}
		int numMovingPucks = _movingPucks.Count;
		for (int i = 0; i < numMovingPucks; i++) {
			PuckNode p = _movingPucks[i];
			p.Move();
			if (_stationaryPucks.ContainsKey(p.GridPosition)) {
				if (p.GetSplitDirection() == EPuckMovementDirection.SplitHorizontal) {
					p.MovementDirection = EPuckMovementDirection.Left;
					MoveStationaryPuck(p.GridPosition, EPuckMovementDirection.Right);
				} else {
					p.MovementDirection = EPuckMovementDirection.Up;
					MoveStationaryPuck(p.GridPosition, EPuckMovementDirection.Down);
				}
			}
		}
		for (int i = 0; i < _movingPucks.Count; i++) {
			PuckNode p = _movingPucks[i];
			if (HasPuckExitedGrid(p)) {
				_movingPucks.RemoveAtSwapBack(i--);
				_exitedPucks.Add(p);
			}
		}

		_stepCount++;

		{
			StringBuilder str = new("[LevelManager]: STEP |");
			str.Append(GetLevelString());
			Debug.Log(str.ToString());
		}
	}

	public void ResetLevel() {
		_hasLevelStarted = false;
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
	/// This function is what should be called for the Player to move a Puck.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="direction"></param>
	public void StartLevelWithChoice(Vector2Int position, EPuckMovementDirection direction) {
		_hasLevelStarted = true;
		MoveStationaryPuck(position, direction);

		{
			StringBuilder str = new($"[LevelManager]: START ({position.x}, {position.y}, {PuckUtil.PuckMovementToChar(direction)}) |");
			str.Append(GetLevelString());
			Debug.Log(str.ToString());
		}

		StartCoroutine(LevelUpdateCoroutine());
	}

	void MoveStationaryPuck(Vector2Int position, EPuckMovementDirection direction) {
		PuckNode p = _stationaryPucks[position];
		_stationaryPucks.Remove(position);
		p.MovementDirection = direction;
		_movingPucks.Add(p);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool HasPuckExitedGrid(PuckNode puck) => puck.GridPosition.x < 0 || puck.GridPosition.x >= HeightCount || puck.GridPosition.y < 0 || puck.GridPosition.y >= WidthCount;

	void TestSolution() => StartLevelWithChoice(_solutionPosition, _solutionDirection);

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
	/// (H: {HeightCount}, W: {WidthCount}) | Solution: (r: {_solPos.x}, c: {_solPos.y}, dir: {solDir}) | Step: {_step} | ({#moving}+{#exited}) / {#stationary}
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
			.Append("Step: ").Append(_stepCount)
			.Append(" | (").Append(_movingPucks.Count).Append('+').Append(_exitedPucks.Count).Append(") / ").Append(_stationaryPucks.Count);

	/// <summary>
	/// Gets level information such as grid size, difficulty, solution, and step count. Format:<br/>
	/// (H: {HeightCount}, W: {WidthCount}) | Solution: (r: {_solPos.x}, c: {_solPos.y}, dir: {solDir}) | Step: {_step} | ({#moving}+{#exited}) / {#stationary}
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

	private void BindDebugActions() {
		GameManager.DebugActions.ResetLevel.started += OnResetLevelActionStarted;
		GameManager.DebugActions.StepPucks.started += OnStepPucksActionStarted;
	}

	private void OnResetLevelActionStarted(InputAction.CallbackContext context) {
		Debug.Log("Reset level pressed");
		ResetLevel();
	}

	private void OnStepPucksActionStarted(InputAction.CallbackContext context) {
		if (!_hasLevelStarted) {
			TestSolution();
		} else {
			StepLevel();
		}
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
			switch (grid[puck.GridPosition.x, puck.GridPosition.y]) {
				case EPuckMovementDirection.Up:
				case EPuckMovementDirection.Down:
					grid[puck.GridPosition.x, puck.GridPosition.y] = EPuckMovementDirection.SplitVertical;
					break;
				case EPuckMovementDirection.Left:
				case EPuckMovementDirection.Right:
					grid[puck.GridPosition.x, puck.GridPosition.y] = EPuckMovementDirection.SplitHorizontal;
					break;
				case EPuckMovementDirection.None:
					grid[puck.GridPosition.x, puck.GridPosition.y] = puck.MovementDirection;
					break;
				default:
					break;
			}
		}
		return grid;
	}

	void D_DrawLevelGridOutline(float duration=0f) {

	}

}
