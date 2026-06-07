using Pucks;
using Pucks.Utilities;
using Slenderpi.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour {

	/// <summary>
	/// Broadcasted after every StepLevel() call.<br/>
	/// int: numCollisions, the number of moving->stationary collisions this step
	/// </summary>
	public static Action<int> A_OnLevelStepped;

	[Tooltip("The value should be set to something that is <= WidthCount * HeightCount.")]
	[SerializeField]
	int PUCK_MOVER_POOL_SIZE = 20 * 16;

	public static LevelManager Singleton;

	[SerializeField]
	bool _updateManually = false;

	[SerializeField]
	PuckMover _puckPrefab;

	/// <summary>
	/// Number of columns in the level grid.
	/// </summary>
	[Min(5)]
	public int WidthCount = 20;
	/// <summary>
	/// Number of rows in the level grid.
	/// </summary>
	[Min(5)]
	public int HeightCount = 16;
	/// <summary>
	/// Physical size of a Puck.
	/// </summary>
	public float PuckSize = 1f;

	/// <summary>
	/// Time between StepLevel() calls.
	/// </summary>
	[Min(0.005f)]
	public float StepUpdateDelay = 0.1f;

	public int StepCount => _stepCount;

	/// <summary>
	/// Consists of where the current level's Pucks start at.
	/// </summary>
	readonly List<Vector2Int> _currentLevel = new();
	/// <summary>
	/// The position of the Solution puck
	/// </summary>
	Vector2Int _solutionPosition = new();
	/// <summary>
	///  The direction the Solution puck should move in to solve the current level.
	/// </summary>
	EPuckMovementDirection _solutionDirection = EPuckMovementDirection.Up;
	int _difficulty = 0;

	Vector3 _positionOffset = new();

	/// <summary>
	/// Contains all stationary Pucks, indexed by their grid position.
	/// </summary>
	readonly Dictionary<Vector2Int, PuckNode> _stationaryPucks = new();
	/// <summary>
	/// Contains all moving Pucks.
	/// </summary>
	readonly List<PuckNode> _movingPucks = new();
	/// <summary>
	/// Contains all Pucks that have exited the level grid.
	/// </summary>
	readonly List<PuckNode> _exitedPucks = new();
	/// <summary>
	/// Hashmap of currently active PuckMovers.
	/// </summary>
	readonly Dictionary<PuckNode, PuckMover> _activePuckMovers = new();
	readonly List<PuckMover> _puckMoverPool = new();
	int _puckPoolHeader = 0;

	bool _hasLevelStarted = false;
	float _timeSinceLastStep = 0;

	int _stepCount = 0;



	private void Awake() {
		if (!Singleton) {
			Singleton = this;
			//DontDestroyOnLoad(gameObject);

			_positionOffset = new(WidthCount * PuckSize / -2f, HeightCount * PuckSize / -2f, 0);
			CreatePuckMoverPool();
		} else if (Singleton != this) {
			Destroy(gameObject);
		}
	}

	private void Start() {
		BindDebugActions();
		GameManager.DebugActions.Enable();
			GenerateLevel(0);
	}

	private void Update() {
		D_DrawLevelGridOutline();
		if (!_hasLevelStarted)
			return;
		if (!_updateManually) {
			while (_timeSinceLastStep >= StepUpdateDelay) {
				_timeSinceLastStep -= StepUpdateDelay;
				StepLevel();
			}
			foreach (var (pn, pm) in _activePuckMovers) {
				pm.transform.position = GetLerpedPosition(pn);
			}
			_timeSinceLastStep += Time.deltaTime;
		} else {
			foreach (var (pn, pm) in _activePuckMovers) {
				pm.transform.position = PointToPosition(pn.GridPoint);
			}
		}
	}

	private void OnDestroy() {
		if (Singleton == this) {
			Singleton = null;
		}
	}

	public static PuckNode GetPuckAt(Vector3 position) {
		Vector2Int point = Singleton.PositionToPoint(position);
		return Singleton._stationaryPucks.ContainsKey(point)
			? Singleton._stationaryPucks[point]
			: null;
	}

	public static PuckMover GetPuckMoverFromPuck(PuckNode puckNode) => Singleton._activePuckMovers[puckNode];

	public void GenerateLevel(int difficulty) {
		Assert.IsTrue(difficulty >= 0, $"[LevelManager]: GeneratedLevel() was given an invalid difficulty value of {difficulty}.");

		_currentLevel.Clear();
		_difficulty = difficulty;

		Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions = new();
		Vector2Int lastPoint = new(UnityEngine.Random.Range(2, HeightCount - 3), UnityEngine.Random.Range(2, WidthCount - 3));
		// If |, that means it desires an instigator of ^ or v, and that its children to hit are < and >. That is, we create <|> or ^-v
		EPuckMovementDirection lastSplitDir = Util.UnityRandomBool() ? EPuckMovementDirection.SplitHorizontal : EPuckMovementDirection.SplitVertical;

		Debug.Log($"[LevelManager]: GENERATE BEGIN | lastPoint: {lastPoint} | lastSplitDir: {PuckUtil.PuckMovementToChar(lastSplitDir)}");

		chosenPositions.Add(lastPoint, lastSplitDir);
		for (int i = difficulty; i > 0; i--) {
			Vector2Int sibling;
			EPuckMovementDirection siblingInputHitDir;
			Vector2Int parent;
			EPuckMovementDirection parentSplitDir;
			if (lastSplitDir == EPuckMovementDirection.SplitHorizontal) {
				// Walk left and right
				// Choose left/right based on greater space
				// Create sibling right/left and parent middle
				// Set parent middle to - and lastPoint to middle
				DetermineHorizontalRange(lastPoint, chosenPositions, out Vector2Int leftRange, out Vector2Int rightRange);

				// Determine range for sibling and parent Pucks. The range is [min, max]
				bool useLeftSide = leftRange.y - leftRange.x > rightRange.y - rightRange.x;
				if ((useLeftSide ? leftRange.y - leftRange.x : rightRange.y - rightRange.x) < 1) {
					Debug.LogWarning($"Can't fit iteration {i}! lastPoint: {lastPoint} | Split dir: '{PuckUtil.PuckMovementToChar(lastSplitDir)}' | Ranges: ({leftRange}), ({rightRange})");
					break;
				}
				if (useLeftSide) {
					sibling = new(lastPoint.x, UnityEngine.Random.Range(leftRange.x, leftRange.y - 1));
					siblingInputHitDir = EPuckMovementDirection.Left;
					parent = new(lastPoint.x, UnityEngine.Random.Range(sibling.y + 1, lastPoint.y - 1));
				} else {
					sibling = new(lastPoint.x, UnityEngine.Random.Range(rightRange.x + 1, rightRange.y));
					siblingInputHitDir = EPuckMovementDirection.Right;
					parent = new(lastPoint.x, UnityEngine.Random.Range(lastPoint.y + 1, sibling.y - 1));
				}
				parentSplitDir = EPuckMovementDirection.SplitVertical;
				chosenPositions[lastPoint] = useLeftSide ? EPuckMovementDirection.Right : EPuckMovementDirection.Left;
			} else {
				// Walk up and down
				// Choose up/down based on greater space
				// Create sibling down/up and parent middle
				// Set parent middle to - and lastPoint to middle
				// In the grid, the Y axis expands downward. So going upward means y--, and downward y++
				DetermineVerticalRange(lastPoint, chosenPositions, out Vector2Int upRange, out Vector2Int downRange);

				// Determine range for sibling and parent Pucks. The range is [min, max]
				bool useUpSide = upRange.y - upRange.x > downRange.y - downRange.x;
				if ((useUpSide ? upRange.y - upRange.x : downRange.y - downRange.x) < 1) {
					Debug.LogWarning($"Can't fit iteration {i}! lastPoint: {lastPoint} | Split dir: '{PuckUtil.PuckMovementToChar(lastSplitDir)}' | Ranges: ({upRange}), ({downRange})");
					break;
				}
				if (useUpSide) {
					sibling = new(UnityEngine.Random.Range(upRange.x, upRange.y - 1), lastPoint.y);
					siblingInputHitDir = EPuckMovementDirection.Up;
					parent = new(UnityEngine.Random.Range(sibling.x + 1, lastPoint.x - 1), lastPoint.y);
				} else {
					sibling = new(UnityEngine.Random.Range(downRange.x + 1, downRange.y), lastPoint.y);
					siblingInputHitDir = EPuckMovementDirection.Down;
					parent = new(UnityEngine.Random.Range(lastPoint.x + 1, sibling.x - 1), lastPoint.y);
				}
				parentSplitDir = EPuckMovementDirection.SplitHorizontal;
				chosenPositions[lastPoint] = useUpSide ? EPuckMovementDirection.Down : EPuckMovementDirection.Up;
			}

			chosenPositions.Add(sibling, siblingInputHitDir);
			chosenPositions.Add(parent, parentSplitDir);

			lastPoint = parent;
			lastSplitDir = parentSplitDir;

			{
				StringBuilder str = new($"[LevelManager]: GENERATE ({difficulty}) | iteration: {i}");
				str.Append('\n').Append(GetChosenPositionsAsGridStringBuilder(chosenPositions));
				Debug.Log(str.ToString());
			}
		}

		// Now create answer Puck
		if (lastSplitDir == EPuckMovementDirection.SplitHorizontal) {
			DetermineHorizontalRange(lastPoint, chosenPositions, out Vector2Int leftRange, out Vector2Int rightRange);
			bool useLeft = Util.UnityRandomBool();
			Debug.Log("lastSplitDir was HORIZONTAL");
			if (useLeft) {
				_solutionPosition = new(lastPoint.x, UnityEngine.Random.Range(leftRange.x, leftRange.y));
				_solutionDirection = EPuckMovementDirection.Right;
			} else {
				_solutionPosition = new(lastPoint.x, UnityEngine.Random.Range(rightRange.x, rightRange.y));
				_solutionDirection = EPuckMovementDirection.Left;
			}
			if (chosenPositions.ContainsKey(_solutionPosition)) {
				// Try other side
				if (!useLeft) {
					_solutionPosition = new(lastPoint.x, UnityEngine.Random.Range(leftRange.x, leftRange.y));
					_solutionDirection = EPuckMovementDirection.Right;
				} else {
					_solutionPosition = new(lastPoint.x, UnityEngine.Random.Range(rightRange.x, rightRange.y));
					_solutionDirection = EPuckMovementDirection.Left;
				}
				if (chosenPositions.ContainsKey(_solutionPosition))
					Debug.LogWarning("[LevelManager]: The final Puck does not have space to be given an answer Puck.");
			}
		} else {
			DetermineVerticalRange(lastPoint, chosenPositions, out Vector2Int upRange, out Vector2Int downRange);
			bool useUp = Util.UnityRandomBool();
			Debug.Log("lastSplitDir was VERTICAL");
			if (useUp) {
				_solutionPosition = new(UnityEngine.Random.Range(upRange.x, upRange.y), lastPoint.y);
				_solutionDirection = EPuckMovementDirection.Down;
				Debug.Log("1: useUp");
			} else {
				_solutionPosition = new(UnityEngine.Random.Range(downRange.x, downRange.y), lastPoint.y);
				_solutionDirection = EPuckMovementDirection.Up;
				Debug.Log("1: useDown");
			}
			if (chosenPositions.ContainsKey(_solutionPosition)) {
				// Try other side
				if (!useUp) {
					_solutionPosition = new(UnityEngine.Random.Range(upRange.x, upRange.y), lastPoint.y);
					_solutionDirection = EPuckMovementDirection.Down;
					Debug.Log("2: useUp");
				} else {
					_solutionPosition = new(UnityEngine.Random.Range(downRange.x, downRange.y), lastPoint.y);
					_solutionDirection = EPuckMovementDirection.Up;
					Debug.Log("2: useDown");
				}
				if (chosenPositions.ContainsKey(_solutionPosition))
					Debug.LogWarning("[LevelManager]: The final Puck does not have space to be given an answer Puck.");
			}
		}
		chosenPositions.Add(_solutionPosition, _solutionDirection);

		{
			StringBuilder str = new($"[LevelManager]: GENERATE ({difficulty}) | iteration: {0}");
			str.Append('\n').Append(GetChosenPositionsAsGridStringBuilder(chosenPositions));
			Debug.Log(str.ToString());
		}

		foreach (var (pos, _) in chosenPositions)
			_currentLevel.Add(pos);

		ResetLevel();
	}

	public void StepLevel() {
		foreach (PuckNode p in _exitedPucks) {
			p.Move();
		}
		int numMovingPucks = _movingPucks.Count;
		int numCollisions = 0;
		for (int i = 0; i < numMovingPucks; i++) {
			PuckNode p = _movingPucks[i];
			p.Move();
			if (_stationaryPucks.ContainsKey(p.GridPoint)) {
				numCollisions++;
				if (p.GetSplitDirection() == EPuckMovementDirection.SplitHorizontal) {
					p.MovementDirection = EPuckMovementDirection.Left;
					MoveStationaryPuck(p.GridPoint, EPuckMovementDirection.Right);
				} else {
					p.MovementDirection = EPuckMovementDirection.Up;
					MoveStationaryPuck(p.GridPoint, EPuckMovementDirection.Down);
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

		//{
		//	StringBuilder str = new("[LevelManager]: STEP |");
		//	str.Append(GetLevelString());
		//	Debug.Log(str.ToString());
		//}
		A_OnLevelStepped?.Invoke(numCollisions);
	}

	public void ResetLevel() {
		_hasLevelStarted = false;
		_stepCount = 0;
		_timeSinceLastStep = 0f;
		ClearLevel();

		// Track manhattan distances for each Puck
		List<int> puckManDists = new();
		// Create stationary PuckNodes
		foreach (var pos in _currentLevel) {
			PuckNode puck = new(pos.x, pos.y);
			int manDist = pos.x + pos.y;
			int index = puckManDists.BinarySearch(manDist);
			if (index < 0) {
				index = ~index; // insertion point
				puckManDists.Insert(index, manDist);
			}
			_stationaryPucks.Add(pos, puck);
		}
		// Bind PuckMovers
		foreach (var (pos, pn) in _stationaryPucks) {
			PuckMover pm = SpawnPuckMover();
			pm.transform.position = PointToPosition(pn.GridPoint);
			pm.gameObject.SetActive(true);
			pm.OnSpawned(puckManDists.BinarySearch(pos.x + pos.y));
			_activePuckMovers.Add(pn, pm);
		}

		//{
		//	StringBuilder str = new("[LevelManager]: RESET |");
		//	str.Append(GetLevelString());
		//	Debug.Log(str.ToString());
		//}
	}

	/// <summary>
	/// Destroys all Pucks currently in the level.
	/// </summary>
	public void ClearLevel() {
		//foreach (var (_, puck) in _stationaryPucks)
		//	puck.Destroy();
		//foreach (var puck in _movingPucks)
		//	puck.Destroy();
		//foreach (var puck in _exitedPucks)
		//	puck.Destroy();
		foreach (var (_, pm) in _activePuckMovers) {
			pm.gameObject.SetActive(false);
		}
		_stationaryPucks.Clear();
		_movingPucks.Clear();
		_exitedPucks.Clear();
		_activePuckMovers.Clear();
		_puckPoolHeader = 0;
	}

	/// <summary>
	/// Fills the grid full with Pucks. Assumes the Puck pool is big enough.
	/// </summary>
	public void GenerateFilledLevel() {
		_currentLevel.Clear();
		for (int r = 0; r < HeightCount; r++)
			for (int c = 0; c < WidthCount; c++)
				_currentLevel.Add(new(r, c));
		_solutionPosition = new(0, 0);
		_solutionDirection = EPuckMovementDirection.Right;
		ResetLevel();
	}

	StringBuilder GetChosenPositionsAsGridStringBuilder(Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions) {
		EPuckMovementDirection[,] grid = new EPuckMovementDirection[HeightCount, WidthCount];
		foreach (var (pos, val) in chosenPositions)
			grid[pos.x, pos.y] = val;
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

	void DetermineHorizontalRange(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions, out Vector2Int leftRange, out Vector2Int rightRange) {
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

	void DetermineVerticalRange(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions, out Vector2Int upRange, out Vector2Int downRange) {
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

	/// <summary>
	/// This function is what should be called for the Player to move a Puck.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="direction"></param>
	public static void StartLevelWithChoice(Vector2Int position, EPuckMovementDirection direction) {
		Singleton._hasLevelStarted = true;
		Singleton.MoveStationaryPuck(position, direction);
		Singleton.StepLevel();

		//{
		//	StringBuilder str = new($"[LevelManager]: START ({position.x}, {position.y}, {PuckUtil.PuckMovementToChar(direction)}) |");
		//	str.Append(Singleton.GetLevelString());
		//	Debug.Log(str.ToString());
		//}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 PointToPosition(Vector2Int point) => new(
		point.y * PuckSize + _positionOffset.x + PuckSize * 0.5f,
		(HeightCount - point.x - 1) * PuckSize + _positionOffset.y + PuckSize * 0.5f,
		_positionOffset.z
	);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2Int PositionToPoint(Vector3 position) => new(
		HeightCount - 1 - Mathf.RoundToInt((position.y - _positionOffset.y - PuckSize * 0.5f) / PuckSize),
		Mathf.RoundToInt((position.x - _positionOffset.x - PuckSize * 0.5f) / PuckSize)
	);

	void MoveStationaryPuck(Vector2Int position, EPuckMovementDirection direction) {
		PuckNode p = _stationaryPucks[position];
		_stationaryPucks.Remove(position);
		p.MovementDirection = direction;
		_movingPucks.Add(p);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool HasPuckExitedGrid(PuckNode puck) => puck.GridPoint.x < 0 || puck.GridPoint.x >= HeightCount || puck.GridPoint.y < 0 || puck.GridPoint.y >= WidthCount;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void TestSolution() => StartLevelWithChoice(_solutionPosition, _solutionDirection);

	//StringBuilder GetGeneratedLevelGridStringBuilder() {
	//	EPuckMovementDirection[,] grid = GetGeneratedLevelAs2dArray();
	//	StringBuilder str = new("  ");
	//	// Print col header
	//	for (int i = 0; i < WidthCount; i++)
	//		str.AppendFormat("{0,3}", i);
	//	str.Append('\n');
	//	// Print each row
	//	for (int r = 0; r < HeightCount; r++) {
	//		str.AppendFormat("{0,2}", r);
	//		for (int c = 0; c < WidthCount; c++) {
	//			str.Append("  ").Append(PuckUtil.PuckMovementToChar(grid[r, c]));
	//		}
	//		if (r < HeightCount - 1)
	//			str.Append('\n');
	//	}
	//	return str;
	//}

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
		var da = GameManager.DebugActions;
		da.ResetLevel.started += OnResetLevelActionStarted;
		da.StepPucks.started += OnStepPucksActionStarted;
		da.GenerateFilledLevel.started += _ => GenerateFilledLevel();
		da.GenerateLevel0.started += _ => GenerateLevel(0);
		da.GenerateLevel1.started += _ => GenerateLevel(1);
		da.GenerateLevel2.started += _ => GenerateLevel(2);
		da.GenerateLevel3.started += _ => GenerateLevel(3);
		da.GenerateLevel4.started += _ => GenerateLevel(4);
		da.GenerateLevel5.started += _ => GenerateLevel(5);
		da.GenerateLevel6.started += _ => GenerateLevel(6);
		da.GenerateLevel7.started += _ => GenerateLevel(7);
		da.GenerateLevel8.started += _ => GenerateLevel(8);
		da.GenerateLevel9.started += _ => GenerateLevel(9);
	}

	private void OnResetLevelActionStarted(InputAction.CallbackContext context) {
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
	/// Doesn't actually spawn a PuckMover. Rather, it takes from the _puckMoverPool and increments the _puckPoolHeader pointer.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	PuckMover SpawnPuckMover() {
		Assert.IsTrue(
			_puckPoolHeader < PUCK_MOVER_POOL_SIZE,
			$"[LevelManager]: Attempted to spawn another PuckMover but the pool has been used up! Pool size: {PUCK_MOVER_POOL_SIZE}."
		);
		return _puckMoverPool[_puckPoolHeader++];
	}

	void CreatePuckMoverPool() {
		for (int i = 0; i < PUCK_MOVER_POOL_SIZE; i++) {
			PuckMover pm = Instantiate(_puckPrefab);
			pm.gameObject.SetActive(false);
			_puckMoverPool.Add(pm);
		}
	}

	Vector3 GetLerpedPosition(PuckNode p) => Vector3.Lerp(
		PointToPosition(p.PreviousGridPoint),
		PointToPosition(p.GridPoint),
		_timeSinceLastStep / StepUpdateDelay
	);

	/// <summary>
	/// Creates a 2d array representing the current level, where each Puck is represented purely by its movement direction.
	/// </summary>
	EPuckMovementDirection[,] LevelTo2dArrayDirectionOnly() {
		EPuckMovementDirection[,] grid = new EPuckMovementDirection[HeightCount, WidthCount];
		foreach (var (pos, puck) in _stationaryPucks) {
			grid[pos.x, pos.y] = puck.MovementDirection;
		}
		foreach (var puck in _movingPucks) {
			switch (grid[puck.GridPoint.x, puck.GridPoint.y]) {
				case EPuckMovementDirection.Up:
				case EPuckMovementDirection.Down:
					grid[puck.GridPoint.x, puck.GridPoint.y] = EPuckMovementDirection.SplitVertical;
					break;
				case EPuckMovementDirection.Left:
				case EPuckMovementDirection.Right:
					grid[puck.GridPoint.x, puck.GridPoint.y] = EPuckMovementDirection.SplitHorizontal;
					break;
				case EPuckMovementDirection.None:
					grid[puck.GridPoint.x, puck.GridPoint.y] = puck.MovementDirection;
					break;
				default:
					break;
			}
		}
		return grid;
	}

	/// <summary>
	/// Creates a 2d array representing the current generated level.
	/// </summary>
	EPuckMovementDirection[,] GetGeneratedLevelAs2dArray() {
		EPuckMovementDirection[,] grid = new EPuckMovementDirection[HeightCount, WidthCount];
		foreach (var (pos, puck) in _stationaryPucks) {
			grid[pos.x, pos.y] = puck.MovementDirection;
		}
		foreach (var puck in _movingPucks) {
			switch (grid[puck.GridPoint.x, puck.GridPoint.y]) {
				case EPuckMovementDirection.Up:
				case EPuckMovementDirection.Down:
					grid[puck.GridPoint.x, puck.GridPoint.y] = EPuckMovementDirection.SplitVertical;
					break;
				case EPuckMovementDirection.Left:
				case EPuckMovementDirection.Right:
					grid[puck.GridPoint.x, puck.GridPoint.y] = EPuckMovementDirection.SplitHorizontal;
					break;
				case EPuckMovementDirection.None:
					grid[puck.GridPoint.x, puck.GridPoint.y] = puck.MovementDirection;
					break;
				default:
					break;
			}
		}
		return grid;
	}

	void D_DrawLevelGridOutline(float duration=0f) {
		float3 box = new(WidthCount * PuckSize, HeightCount * PuckSize, PuckSize);
		Util.D_DrawBox(box + new float3(_positionOffset) - box / 2f, box, Color.black, duration, false);
	}

}
