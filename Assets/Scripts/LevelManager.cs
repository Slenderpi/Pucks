using Pucks;
using Pucks.Level;
using Pucks.Level.Quad;
using Slenderpi.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour {

	///// <summary>
	///// Broadcasted after every StepLevel() call.<br/>
	///// int: numCollisions, the number of moving->stationary collisions this step
	///// </summary>
	//public static Action<int> A_OnLevelStepped;
	///// <summary>
	///// Broadcasted after GenerateLevel() has finished.<br/>
	///// int: difficulty
	///// </summary>
	//public static Action<int> A_OnLevelSpawned;
	///// <summary>
	///// Broadcasted if GenerateLevel() fails to generate a level.<br/>
	///// int: difficulty<br/>
	///// int: numGenProcessFails<br/>
	///// int: numUnsovlableFails
	///// </summary>
	//public static Action<int, int, int> A_OnLevelGenFailed;
	/// <summary>
	/// Broadcast when the current PuckSimulator changes.<br/>
	/// EPuckType: the EPuckType enum specified for the change.
	/// </summary>
	public static Action<EPuckType> A_OnPuckSimulatorChanged;

	//[Tooltip("The value should be set to something that is <= WidthCount * HeightCount.")]
	//[SerializeField]
	//int PUCK_MOVER_POOL_SIZE = 20 * 16;

	public static LevelManager Singleton;

	public EPuckType PuckType = EPuckType.Quad;

	[Tooltip("Reference to a PuckMoverPool.")]
	public PuckMoverPool PuckMoverPool;

	[SerializeField]
	PuckMover _quadPuckPrefab;
	[SerializeField]
	PuckMover _hexPuckPrefab;
	

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

	// TODO: replace
	//public int StepCount => _stepCount;

	[Header("Debug")]
	[SerializeField]
	bool _updateManually = false;
	[SerializeField]
	int _startingDifficulty = 0;

	/// <summary>
	/// Access the current PuckSimulator.
	/// </summary>
	public PuckSimulation PuckSimulator => _puckSimulator;

	// TODO: replace
	///// <summary>
	///// Consists of where the current level's Pucks start at.
	///// </summary>
	//readonly List<Vector2Int> _currentLevel = new();
	///// <summary>
	///// The position of the Solution puck
	///// </summary>
	//Vector2Int _solutionPosition = new();
	///// <summary>
	/////  The direction the Solution puck should move in to solve the current level.
	///// </summary>
	//EPuckMovementDirection _solutionDirection = EPuckMovementDirection.Up;
	//int _difficulty = 0;

	Vector3 _positionOffset = new();

	PuckSimulation _puckSimulator;

	// TODO: replace
	///// <summary>
	///// Contains all stationary Pucks, indexed by their grid position.
	///// </summary>
	//readonly Dictionary<Vector2Int, PuckNode> _stationaryPucks = new();
	///// <summary>
	///// Contains all moving Pucks.
	///// </summary>
	//readonly List<PuckNode> _movingPucks = new();
	///// <summary>
	///// Contains all Pucks that have exited the level grid.
	///// </summary>
	//readonly List<PuckNode> _exitedPucks = new();
	/// <summary>
	/// Hashmap of currently active PuckMovers.
	/// </summary>
	readonly Dictionary<PuckNode, PuckMover> _activePuckMovers = new();
	//readonly List<PuckMover> _puckMoverPool = new();
	//int _puckPoolHeader = 0;

	float _timeSinceLastStep = 0;
	
	// TODO: replace
	//bool _hasLevelStarted = false;
	//int _stepCount = 0;



	private void Awake() {
		Assert.IsNotNull(PuckMoverPool, "[LevelManager]: Missing reference to PuckMoverPool.");
		if (!Singleton) {
			Singleton = this;
			//DontDestroyOnLoad(gameObject);

			ChangePuckSimulator(EPuckType.Quad);
			_positionOffset = new(WidthCount * PuckSize / -2f, HeightCount * PuckSize / -2f, 0);
			//CreatePuckMoverPool();
		} else if (Singleton != this) {
			Destroy(gameObject);
		}
	}

	private void Start() {
		BindDebugActions();
		GameManager.DebugActions.Enable();
		_puckSimulator.GenerateLevel(_startingDifficulty);
	}

	private void Update() {
		D_DrawLevelGridOutline();
		if (!_puckSimulator.HasLevelStarted)
			return;
		if (!_updateManually) {
			while (_timeSinceLastStep >= StepUpdateDelay) {
				_timeSinceLastStep -= StepUpdateDelay;
				_puckSimulator.Step();
				//if (_puckSimulator.IsInWinState()) {
				//	Debug.Log("Won!");
				//} else if (_puckSimulator.IsInLoseState()) {
				//	Debug.Log("Lost!");
				//}
			}
			foreach (var (pn, pm) in _activePuckMovers) {
				pm.transform.position = GetLerpedPosition(pn) + _positionOffset;
			}
			_timeSinceLastStep += Time.deltaTime;
		} else {
			foreach (var (pn, pm) in _activePuckMovers) {
				pm.transform.position = _puckSimulator.PointToPosition(pn.GridPoint, PuckSize) + _positionOffset;
			}
		}
	}

	private void OnDestroy() {
		if (Singleton == this) {
			Singleton = null;
		}
	}

	public void ChangePuckSimulator(EPuckType puckType) {
		_puckSimulator = puckType switch {
			EPuckType.Quad => new QuadPuckSimulation() {
				HeightCount = HeightCount,
				WidthCount = WidthCount
			},
			_ => throw new ArgumentException("[LevelManager]: ChangePuckSimulator() called with invalid EPuckType value: {puckType}."),
		};
		_puckSimulator.A_OnLevelSpawned += OnLevelSpawned;
		_puckSimulator.A_OnLevelCleared += ClearActivePuckMovers;
		_puckSimulator.A_OnLevelGenerated += () => {
			_puckSimulator.ClearLevel();
			_puckSimulator.SpawnLevel();
		};
		A_OnPuckSimulatorChanged?.Invoke(puckType);
	}

	// TODO: replace
	//public static PuckNode GetPuckAt(Vector3 position) {
	//	Vector2Int point = Singleton.PositionToPoint(position);
	//	return Singleton._stationaryPucks.ContainsKey(point)
	//		? Singleton._stationaryPucks[point]
	//		: null;
	//}

	public static PuckMover GetPuckMoverFromPuck(PuckNode puckNode) => Singleton._activePuckMovers[puckNode];

	//public void GenerateLevel(int difficulty) {
	//	Assert.IsTrue(difficulty >= 0, $"[LevelManager]: GeneratedLevel() was given an invalid difficulty value of {difficulty}.");
	//	//_difficulty = difficulty;
	//	int MAX_TRIES = 100;
	//	int numGenProcessFails = 0;
	//	int numUnsovlableFails = 0;
	//	// TODO: replace
	//	do {
	//		if (!_puckSimulator.GenerateLevel(difficulty)) {
	//			numGenProcessFails++;
	//			continue;
	//		}
	//		if (_puckSimulator.TestGeneratedLevel())
	//			break;
	//		numUnsovlableFails++;
	//	} while (numGenProcessFails + numUnsovlableFails < MAX_TRIES);
	//	//do {
	//	//	if (!GenerateLevel_Implementation(difficulty)) {
	//	//		numGenProcessFails++;
	//	//		continue;
	//	//	}
	//	//	ResetLevel();
	//	//	MoveStationaryPuck(_solutionPosition, _solutionDirection);
	//	//	while (_movingPucks.Count > 0) // Brute force solution validation by stepping it until completion
	//	//		StepLevel_Implementation();
	//	//	if (_stationaryPucks.Count == 0)
	//	//		break;
	//	//	numUnsovlableFails++;
	//	//} while (numGenProcessFails + numUnsovlableFails < MAX_TRIES);
	//	if (numGenProcessFails + numUnsovlableFails > 1) {
	//		Debug.LogWarning($"[LevelManager]: Level generation failed {numGenProcessFails + numUnsovlableFails} (max {MAX_TRIES}) times for difficulty {difficulty}. Of them, {numGenProcessFails} were generation issues, and {numUnsovlableFails} were from impossible puzzles.");
	//		if (numGenProcessFails + numUnsovlableFails == MAX_TRIES) {
	//			//ClearLevel();
	//			A_OnLevelGenFailed?.Invoke(difficulty, numGenProcessFails, numUnsovlableFails);
	//			return;
	//		}
	//	}
	//	RespawnLevel();
	//	A_OnLevelSpawned?.Invoke(difficulty);
	//}

	//public void SpawnLevelAndBindPuckMovers() {
	//	ClearActivePuckMovers();
	//	_puckSimulator.SpawnLevel();
	//	// Bind PuckMovers to PuckNodes
	//	foreach (var (pos, pn) in _puckSimulator.GetStationaryPucks()) {
	//		PuckMover pm = PuckMoverPool.SpawnPuckMover();
	//		pm.transform.position = _positionOffset + _puckSimulator.PointToPosition(pos, PuckSize);
	//		pm.gameObject.SetActive(true);
	//		pm.OnSpawned(_puckSimulator.PuckSpawnOrderList.BinarySearch(pos.x + pos.y));
	//		_activePuckMovers.Add(pn, pm);
	//	}
	//}

	/// <summary>
	/// Fills availableSpots with 
	/// </summary>
	/// <param name="lastPoint"></param>
	/// <param name="chosenPositions"></param>
	/// <param name="availableSpots"></param>
	/// <returns></returns>
	// TODO: replace
	//int DetermineHorizontalRangeNEW(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions, List<int> availableSpots) {
	//	int currCol = lastPoint.y - 1;
	//	int lpindex = 0;
	//	// Walk left to determine smallest left
	//	while (currCol >= 0) {
	//		// Add the spot if there's nothing there or the movementdir there is Claimed
	//		Vector2Int currPos = new(lastPoint.x, currCol);
	//		if (!chosenPositions.ContainsKey(currPos)) {
	//			availableSpots.Add(currCol);
	//			lpindex++;
	//		} else {
	//			if (chosenPositions[currPos] != EPuckMovementDirection.Claimed)
	//				break;
	//		}
	//		currCol--;
	//	}

	//	// Now walk right
	//	currCol = lastPoint.y + 1;
	//	while (currCol < WidthCount) {
	//		// Add the spot if there's nothing there, end if there is something and it's not just a Claimed
	//		Vector2Int currPos = new(lastPoint.x, currCol);
	//		if (!chosenPositions.ContainsKey(currPos)) {
	//			availableSpots.Add(currCol);
	//		} else {
	//			if (chosenPositions[currPos] != EPuckMovementDirection.Claimed)
	//				break;
	//		}
	//		currCol++;
	//	}

	//	return lpindex;
	//}

	// TODO: replace
	/// <summary>
	/// Fills availableSpots with 
	/// </summary>
	/// <param name="lastPoint"></param>
	/// <param name="chosenPositions"></param>
	/// <param name="availableSpots"></param>
	/// <returns></returns>
	//int DetermineVerticalRangeNEW(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions, List<int> availableSpots) {
	//	int currRow = lastPoint.x - 1;
	//	int lpindex = 0;
	//	// Walk up to determine smallest up
	//	while (currRow >= 0) {
	//		// Add the spot if there's nothing there or the movementdir there is Claimed
	//		Vector2Int currPos = new(currRow, lastPoint.y);
	//		if (!chosenPositions.ContainsKey(currPos)) {
	//			availableSpots.Add(currRow);
	//			lpindex++;
	//		} else {
	//			if (chosenPositions[currPos] != EPuckMovementDirection.Claimed)
	//				break;
	//		}
	//		currRow--;
	//	}

	//	// Now walk down
	//	currRow = lastPoint.x + 1;
	//	while (currRow < HeightCount) {
	//		// Add the spot if there's nothing there, end if there is something and it's not just a Claimed
	//		Vector2Int currPos = new(currRow, lastPoint.y);
	//		if (!chosenPositions.ContainsKey(currPos)) {
	//			availableSpots.Add(currRow);
	//		} else {
	//			if (chosenPositions[currPos] != EPuckMovementDirection.Claimed)
	//				break;
	//		}
	//		currRow++;
	//	}

	//	return lpindex;
	//}

	// TODO: replace
	//private bool GenerateLevel_Implementation(int difficulty) {
	//	_currentLevel.Clear();

	//	Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions = new();
	//	Vector2Int lastPoint = new(UnityEngine.Random.Range(2, HeightCount - 3), UnityEngine.Random.Range(2, WidthCount - 3));
	//	// If |, that means it desires an instigator of ^ or v, and that its children to hit are < and >. That is, we create <|> or ^-v
	//	EPuckMovementDirection lastSplitDir = Util.UnityRandomBool() ? EPuckMovementDirection.SplitHorizontal : EPuckMovementDirection.SplitVertical;
	//	// A list of spots in the current row/col that a new sibling and parent will can be placed at.
	//	List<int> availableSpots = new(Math.Max(WidthCount, HeightCount));

	//	//Debug.Log($"[LevelManager]: GENERATE BEGIN | lastPoint: {lastPoint} | lastSplitDir: {PuckUtil.PuckMovementToChar(lastSplitDir)}");

	//	chosenPositions.Add(lastPoint, lastSplitDir);
	//	for (int i = difficulty; i > 0; i--) {
	//		Vector2Int sibling;
	//		EPuckMovementDirection siblingInputHitDir;
	//		Vector2Int parent;
	//		EPuckMovementDirection parentSplitDir;

	//		availableSpots.Clear();
	//		int lpindex = 0;
	//		if (lastSplitDir == EPuckMovementDirection.SplitHorizontal) {
	//			lpindex = DetermineHorizontalRangeNEW(lastPoint, chosenPositions, availableSpots);
	//			if (lpindex <= 1 && availableSpots.Count - lpindex <= 1) {
	//				Debug.LogWarning($"Can't fit iteration {i}! lastPoint: {lastPoint} | lastSplitDir: '{PuckUtil.PuckMovementToChar(lastSplitDir)}' | availableSpots ({lpindex}): [{string.Join(", ", availableSpots)}]");
	//				lastSplitDir = EPuckMovementDirection.SplitVertical;
	//				return false;
	//			}

	//			// If (leftUnusable) use right
	//			// else if (rightUnusable) use left
	//			// else use random
	//			bool useLeftSide = lpindex <= 1 ? false : availableSpots.Count - lpindex <= 1 ? true : Util.UnityRandomBool();
	//			//bool useLeftSide = false;
	//			if (useLeftSide) {
	//				int siblingColToUseIndex = UnityEngine.Random.Range(1, lpindex - 1);
	//				int parentColToUseIndex = UnityEngine.Random.Range(0, siblingColToUseIndex - 1);
	//				sibling = new(lastPoint.x, availableSpots[siblingColToUseIndex]);
	//				siblingInputHitDir = EPuckMovementDirection.Left;
	//				parent = new(lastPoint.x, availableSpots[parentColToUseIndex]);
	//				// Fill in between spots as Claimed
	//				// ... from lastPoint to parent
	//				for (int j = 0; j < parentColToUseIndex; j++)
	//					chosenPositions.Add(new(lastPoint.x, availableSpots[j]), EPuckMovementDirection.Claimed);
	//				// ... then from parent to sibling
	//				for (int j = parentColToUseIndex + 1; j < siblingColToUseIndex; j++)
	//					chosenPositions.Add(new(lastPoint.x, availableSpots[j]), EPuckMovementDirection.Claimed);
	//			} else {
	//				int siblingColToUseIndex = UnityEngine.Random.Range(lpindex + 1, availableSpots.Count - 1);
	//				int parentColToUseIndex = UnityEngine.Random.Range(lpindex, siblingColToUseIndex - 1);
	//				sibling = new(lastPoint.x, availableSpots[siblingColToUseIndex]);
	//				siblingInputHitDir = EPuckMovementDirection.Right;
	//				parent = new(lastPoint.x, availableSpots[parentColToUseIndex]);
	//				// Fill in between spots as Claimed
	//				// ... from lastPoint to parent
	//				for (int j = lpindex; j < parentColToUseIndex; j++)
	//					chosenPositions.Add(new(lastPoint.x, availableSpots[j]), EPuckMovementDirection.Claimed);
	//				// ... then from parent to sibling
	//				for (int j = parentColToUseIndex + 1; j < siblingColToUseIndex; j++)
	//					chosenPositions.Add(new(lastPoint.x, availableSpots[j]), EPuckMovementDirection.Claimed);
	//			}
	//			parentSplitDir = EPuckMovementDirection.SplitVertical;
	//			chosenPositions[lastPoint] = useLeftSide ? EPuckMovementDirection.Right : EPuckMovementDirection.Left;
	//		} else {
	//			lpindex = DetermineVerticalRangeNEW(lastPoint, chosenPositions, availableSpots);
	//			if (lpindex <= 1 && availableSpots.Count - lpindex <= 1) {
	//				Debug.LogWarning($"Can't fit iteration {i}! lastPoint: {lastPoint} | lastSplitDir: '{PuckUtil.PuckMovementToChar(lastSplitDir)}' | availableSpots ({lpindex}): [{string.Join(", ", availableSpots)}]");
	//				return false;
	//			}

	//			// If (upUnusable) use down
	//			// else if (downUnusable) use up
	//			// else use random
	//			bool useUpSide = lpindex <= 1 ? false : availableSpots.Count - lpindex <= 1 ? true : Util.UnityRandomBool();
	//			//bool useUpSide = true;
	//			if (useUpSide) {
	//				int siblingRowToUseIndex = UnityEngine.Random.Range(1, lpindex - 1);
	//				int parentColToUseIndex = UnityEngine.Random.Range(0, siblingRowToUseIndex - 1);
	//				sibling = new(availableSpots[siblingRowToUseIndex], lastPoint.y);
	//				siblingInputHitDir = EPuckMovementDirection.Up;
	//				parent = new(availableSpots[parentColToUseIndex], lastPoint.y);
	//				// Fill in between spots as Claimed
	//				// ... from lastPoint to parent
	//				for (int j = 0; j < parentColToUseIndex; j++)
	//					chosenPositions.Add(new(availableSpots[j], lastPoint.y), EPuckMovementDirection.Claimed);
	//				// ... then from parent to sibling
	//				for (int j = parentColToUseIndex + 1; j < siblingRowToUseIndex; j++)
	//					chosenPositions.Add(new(availableSpots[j], lastPoint.y), EPuckMovementDirection.Claimed);
	//			} else {
	//				int siblingRowToUseIndex = UnityEngine.Random.Range(lpindex + 1, availableSpots.Count - 1);
	//				int parentColToUseIndex = UnityEngine.Random.Range(lpindex, siblingRowToUseIndex - 1);
	//				sibling = new(availableSpots[siblingRowToUseIndex], lastPoint.y);
	//				siblingInputHitDir = EPuckMovementDirection.Down;
	//				parent = new(availableSpots[parentColToUseIndex], lastPoint.y);
	//				// Fill in between spots as Claimed
	//				// ... from lastPoint to parent
	//				for (int j = lpindex; j < parentColToUseIndex; j++)
	//					chosenPositions.Add(new(availableSpots[j], lastPoint.y), EPuckMovementDirection.Claimed);
	//				// ... then from parent to sibling
	//				for (int j = parentColToUseIndex + 1; j < siblingRowToUseIndex; j++)
	//					chosenPositions.Add(new(availableSpots[j], lastPoint.y), EPuckMovementDirection.Claimed);
	//			}
	//			parentSplitDir = EPuckMovementDirection.SplitHorizontal;
	//			chosenPositions[lastPoint] = useUpSide ? EPuckMovementDirection.Down : EPuckMovementDirection.Up;
	//		}

	//		chosenPositions.Add(sibling, siblingInputHitDir);
	//		chosenPositions.Add(parent, parentSplitDir);

	//		lastPoint = parent;
	//		lastSplitDir = parentSplitDir;

	//		//{
	//		//	StringBuilder str = new($"[LevelManager]: GENERATE ({difficulty}) | iteration: {i} | availableSpots ({lpindex}): {string.Join(", ", availableSpots)}");
	//		//	str.Append('\n').Append(GetChosenPositionsAsGridStringBuilder(chosenPositions));
	//		//	Debug.Log(str.ToString());
	//		//}
	//	}

	//	// Now create answer Puck
	//	if (lastSplitDir == EPuckMovementDirection.SplitHorizontal) {
	//		DetermineHorizontalRange(lastPoint, chosenPositions, out Vector2Int leftRange, out Vector2Int rightRange);
	//		bool useLeft = Util.UnityRandomBool();
	//		if (useLeft) {
	//			_solutionPosition = new(lastPoint.x, UnityEngine.Random.Range(leftRange.x, leftRange.y));
	//			_solutionDirection = EPuckMovementDirection.Right;
	//		} else {
	//			_solutionPosition = new(lastPoint.x, UnityEngine.Random.Range(rightRange.x, rightRange.y));
	//			_solutionDirection = EPuckMovementDirection.Left;
	//		}
	//		if (chosenPositions.ContainsKey(_solutionPosition)) {
	//			// Try other side
	//			if (!useLeft) {
	//				_solutionPosition = new(lastPoint.x, UnityEngine.Random.Range(leftRange.x, leftRange.y));
	//				_solutionDirection = EPuckMovementDirection.Right;
	//			} else {
	//				_solutionPosition = new(lastPoint.x, UnityEngine.Random.Range(rightRange.x, rightRange.y));
	//				_solutionDirection = EPuckMovementDirection.Left;
	//			}
	//			if (chosenPositions.ContainsKey(_solutionPosition)) {
	//				Debug.LogWarning("[LevelManager]: The final Puck does not have space to be given an answer Puck.");
	//				return false;
	//			}
	//		}
	//	} else {
	//		DetermineVerticalRange(lastPoint, chosenPositions, out Vector2Int upRange, out Vector2Int downRange);
	//		bool useUp = Util.UnityRandomBool();
	//		if (useUp) {
	//			_solutionPosition = new(UnityEngine.Random.Range(upRange.x, upRange.y), lastPoint.y);
	//			_solutionDirection = EPuckMovementDirection.Down;
	//		} else {
	//			_solutionPosition = new(UnityEngine.Random.Range(downRange.x, downRange.y), lastPoint.y);
	//			_solutionDirection = EPuckMovementDirection.Up;
	//		}
	//		if (chosenPositions.ContainsKey(_solutionPosition)) {
	//			// Try other side
	//			if (!useUp) {
	//				_solutionPosition = new(UnityEngine.Random.Range(upRange.x, upRange.y), lastPoint.y);
	//				_solutionDirection = EPuckMovementDirection.Down;
	//			} else {
	//				_solutionPosition = new(UnityEngine.Random.Range(downRange.x, downRange.y), lastPoint.y);
	//				_solutionDirection = EPuckMovementDirection.Up;
	//			}
	//			if (chosenPositions.ContainsKey(_solutionPosition)) {
	//				Debug.LogWarning("[LevelManager]: The final Puck does not have space to be given an answer Puck.");
	//				return false;
	//			}
	//		}
	//	}
	//	chosenPositions.Add(_solutionPosition, _solutionDirection);

	//	//{
	//	//	StringBuilder str = new($"[LevelManager]: GENERATE ({difficulty}) | iteration: {0}");
	//	//	str.Append('\n').Append(GetChosenPositionsAsGridStringBuilder(chosenPositions));
	//	//	Debug.Log(str.ToString());
	//	//}

	//	foreach (var (pos, dir) in chosenPositions)
	//		if (dir != EPuckMovementDirection.Claimed)
	//			_currentLevel.Add(pos);
	//	return true;
	//}

	//public void StepSimulator() {
	//	A_OnLevelStepped?.Invoke(_puckSimulator.Step());
	//	// TODO: replace
	//	//int numCollisions = StepLevel_Implementation();
	//	//{
	//	//	StringBuilder str = new("[LevelManager]: STEP |");
	//	//	str.Append(GetLevelString());
	//	//	Debug.Log(str.ToString());
	//	//}
	//	//A_OnLevelStepped?.Invoke(numCollisions);
	//}

	// TODO: replace
	//int StepLevel_Implementation() {
	//	foreach (PuckNode p in _exitedPucks) {
	//		p.Move();
	//	}
	//	int numMovingPucks = _movingPucks.Count;
	//	int numCollisions = 0;
	//	for (int i = 0; i < numMovingPucks; i++) {
	//		PuckNode p = _movingPucks[i];
	//		p.Move();
	//		if (_stationaryPucks.ContainsKey(p.GridPoint)) {
	//			numCollisions++;
	//			PuckNode instigated = _stationaryPucks[p.GridPoint];
	//			p.OnHitStationaryPuck(instigated);
	//			TransferPuckFromStationaryToMoving(instigated);
	//		}
	//	}
	//	for (int i = 0; i < _movingPucks.Count; i++) {
	//		PuckNode p = _movingPucks[i];
	//		if (HasPuckExitedGrid(p)) {
	//			_movingPucks.RemoveAtSwapBack(i--);
	//			_exitedPucks.Add(p);
	//		}
	//	}

	//	_stepCount++;
	//	return numCollisions;
	//}

	void OnLevelSpawned() {
		_timeSinceLastStep = 0f;
		BindPuckMoversToSpawnedLevel();
	}

	void BindPuckMoversToSpawnedLevel() {
		Assert.IsTrue(_puckSimulator.HasLevelSpawned, "[LevelManager]: Cannot bind PuckMovers because the level has not been spawned yet.");
		Assert.IsFalse(_puckSimulator.HasLevelStarted, "[LevelManager]: Cannot bind PuckMovers when the level has already started. Bind before starting the level.");
		// Bind PuckMovers to PuckNodes
		foreach (var (pos, pn) in _puckSimulator.GetStationaryPucks()) {
			PuckMover pm = PuckMoverPool.SpawnPuckMover();
			pm.transform.position = _positionOffset + _puckSimulator.PointToPosition(pos, PuckSize);
			pm.gameObject.SetActive(true);
			pm.OnSpawned(_puckSimulator.PuckSpawnOrderList.BinarySearch(pos.x + pos.y));
			_activePuckMovers.Add(pn, pm);
		}
	}

	void ClearActivePuckMovers() {
		_activePuckMovers.Clear();
		PuckMoverPool.Clear();
	}

	//public void RespawnLevel() {
	//	// TODO: complete
	//	_timeSinceLastStep = 0f;
	//	_puckSimulator.RespawnLevel();
	//	BindPuckMoversToSpawnedLevel();
	//}

	// TODO: replace
	//public void ResetLevel() {
	//	_hasLevelStarted = false;
	//	_stepCount = 0;
	//	_timeSinceLastStep = 0f;
	//	ClearLevel();

	//	// Track manhattan distances for each Puck
	//	List<int> puckManDists = new();
	//	// Create stationary PuckNodes
	//	foreach (var pos in _currentLevel) {
	//		//PuckNode puck = PuckType switch {
	//		//	EPuckType.Quad => new QuadPuck(pos.x, pos.y),
	//		//	EPuckType.Hex => new HexPuck(pos.x, pos.y),
	//		//	_ => new QuadPuck(pos.x, pos.y)
	//		//};
	//		int manDist = pos.x + pos.y;
	//		int index = puckManDists.BinarySearch(manDist);
	//		if (index < 0) {
	//			index = ~index; // insertion point
	//			puckManDists.Insert(index, manDist);
	//		}
	//		_stationaryPucks.Add(pos, puck);
	//	}
	//	// Bind PuckMovers
	//	foreach (var (pos, pn) in _stationaryPucks) {
	//		PuckMover pm = SpawnPuckMover();
	//		pm.transform.position = PointToPosition(pn.GridPoint);
	//		pm.gameObject.SetActive(true);
	//		pm.OnSpawned(puckManDists.BinarySearch(pos.x + pos.y));
	//		_activePuckMovers.Add(pn, pm);
	//	}

	//	//{
	//	//	StringBuilder str = new("[LevelManager]: RESET |");
	//	//	str.Append(GetLevelString());
	//	//	Debug.Log(str.ToString());
	//	//}
	//}

	/// <summary>
	/// Destroys all Pucks currently in the level.
	/// </summary>
	// TODO: replace
	//public void ClearLevel() {
	//	//foreach (var (_, puck) in _stationaryPucks)
	//	//	puck.Destroy();
	//	//foreach (var puck in _movingPucks)
	//	//	puck.Destroy();
	//	//foreach (var puck in _exitedPucks)
	//	//	puck.Destroy();
	//	foreach (var (_, pm) in _activePuckMovers) {
	//		pm.gameObject.SetActive(false);
	//	}
	//	//_stationaryPucks.Clear(); // TODO: replace
	//	//_movingPucks.Clear();
	//	//_exitedPucks.Clear();
	//	_activePuckMovers.Clear();
	//	//_puckPoolHeader = 0;
	//}

	///// <summary>
	///// Fills the grid full with Pucks. Assumes the Puck pool is big enough.
	///// </summary>
	//public void GenerateFilledLevel() {
	//	_currentLevel.Clear();
	//	for (int r = 0; r < HeightCount; r++)
	//		for (int c = 0; c < WidthCount; c++)
	//			_currentLevel.Add(new(r, c));
	//	_solutionPosition = new(0, 0);
	//	_solutionDirection = EPuckMovementDirection.Right;
	//	ResetLevel();
	//	A_OnLevelSpawned?.Invoke(-1);
	//}

	// TODO: replace
	//StringBuilder GetChosenPositionsAsGridStringBuilder(Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions) {
	//	EPuckMovementDirection[,] grid = new EPuckMovementDirection[HeightCount, WidthCount];
	//	foreach (var (pos, val) in chosenPositions)
	//		grid[pos.x, pos.y] = val;
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

	//void DetermineHorizontalRange(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions, out Vector2Int leftRange, out Vector2Int rightRange) {
	//	int smallestLeft = lastPoint.y - 1;
	//	int biggestRight = lastPoint.y + 1;
	//	// Walk left to determine smallest left
	//	while (smallestLeft - 1 >= 0 && !chosenPositions.ContainsKey(new(lastPoint.x, smallestLeft - 1)))
	//		smallestLeft--;
	//	// Walk right to determine biggest right
	//	while (biggestRight + 1 < WidthCount && !chosenPositions.ContainsKey(new(lastPoint.x, biggestRight + 1)))
	//		biggestRight++;
	//	leftRange = new(smallestLeft, lastPoint.y - 1);
	//	rightRange = new(lastPoint.y + 1, biggestRight);
	//}

	//void DetermineVerticalRange(Vector2Int lastPoint, Dictionary<Vector2Int, EPuckMovementDirection> chosenPositions, out Vector2Int upRange, out Vector2Int downRange) {
	//	// In the grid, the Y axis expands downward. So going upward means y--, and downward y++
	//	int highestUp = lastPoint.x - 1;
	//	int lowestDown = lastPoint.x + 1;
	//	// Walk up to determine highest up
	//	while (highestUp - 1 >= 0 && !chosenPositions.ContainsKey(new(highestUp - 1, lastPoint.y)))
	//		highestUp--;
	//	// Walk down to determine lowest down
	//	while (lowestDown + 1 < HeightCount && !chosenPositions.ContainsKey(new(lowestDown + 1, lastPoint.y)))
	//		lowestDown++;
	//	upRange = new(highestUp, lastPoint.x - 1);
	//	downRange = new(lastPoint.x + 1, lowestDown);
	//}

	/// <summary>
	/// This function is what should be called for the Player to move a Puck.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="direction"></param>
	// TODO: replace
	//public static void StartLevelWithChoice(Vector2Int position, Vector3 dragVector) {
	//	Singleton._hasLevelStarted = true;
	//	Singleton.MoveStationaryPuck(position, direction);
	//	Singleton.StepLevel();

	//	//{
	//	//	StringBuilder str = new($"[LevelManager]: START ({position.x}, {position.y}, {PuckUtil.PuckMovementToChar(direction)}) |");
	//	//	str.Append(Singleton.GetLevelString());
	//	//	Debug.Log(str.ToString());
	//	//}
	//}

	// TODO: replace
	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//public Vector3 PointToPosition(Vector2Int point) => new(
	//	point.y * PuckSize + _positionOffset.x + PuckSize * 0.5f,
	//	(HeightCount - point.x - 1) * PuckSize + _positionOffset.y + PuckSize * 0.5f,
	//	_positionOffset.z
	//);

	// TODO: replace
	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//public Vector2Int PositionToPoint(Vector3 position) => new(
	//	HeightCount - 1 - Mathf.RoundToInt((position.y - _positionOffset.y - PuckSize * 0.5f) / PuckSize),
	//	Mathf.RoundToInt((position.x - _positionOffset.x - PuckSize * 0.5f) / PuckSize)
	//);

	// TODO: replace
	//void TransferPuckFromStationaryToMoving(PuckNode p) {
	//	_stationaryPucks.Remove(p.GridPoint);
	//	_movingPucks.Add(p);
	//}

	// TODO: replace
	//void MoveStationaryPuck(Vector2Int position, EPuckMovementDirection direction) {
	//	PuckNode p = _stationaryPucks[position];
	//	_stationaryPucks.Remove(position);
	//	p.MovementDirection = direction;
	//	_movingPucks.Add(p);
	//}

	// TODO: replace
	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//bool HasPuckExitedGrid(PuckNode puck) => puck.GridPoint.x < 0 || puck.GridPoint.x >= HeightCount || puck.GridPoint.y < 0 || puck.GridPoint.y >= WidthCount;

	// TODO: replace
	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//void TestSolution() => StartLevelWithChoice(_solutionPosition, _solutionDirection);

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
	// TODO: replace
	//public StringBuilder GetLevelGridStringBuilder() {
	//	EPuckMovementDirection[,] grid = LevelTo2dArrayDirectionOnly();
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
	/// Gets level information such as grid size, difficulty, solution, and step count. Format:<br/>
	/// (H: {HeightCount}, W: {WidthCount}) | Solution: (r: {_solPos.x}, c: {_solPos.y}, dir: {solDir}) | Step: {_step} | ({#moving}+{#exited}) / {#stationary}
	/// </summary>
	/// <returns>A StringBuilder containing the information.</returns>
	public StringBuilder GetLevelMetaDataStringBuilder() => new StringBuilder()
			.Append("(H: ").Append(HeightCount)
			.Append(", W: ").Append(WidthCount)
			.Append(") | ")
			.Append("Solution: (r: ").Append(_puckSimulator.SolutionPosition.x)
			.Append(", c: ").Append(_puckSimulator.SolutionPosition.y)
			.Append(", dir: ").Append(_puckSimulator.SolutionDirection)
			.Append(") | ")
			.Append("Step: ").Append(_puckSimulator.StepCount)
			.Append(" | (").Append(_puckSimulator.NumMovingPucks).Append('+').Append(_puckSimulator.NumExitedPuck).Append(") / ").Append(_puckSimulator.NumStationaryPucks);

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
			.Append(_puckSimulator.GetLevelGridStringBuilder())
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
		da.GenerateFilledLevel.started += _ => _puckSimulator.GenerateLevel(-1);
		da.GenerateLevel0.started += _ => _puckSimulator.GenerateLevel(0);
		da.GenerateLevel1.started += _ => _puckSimulator.GenerateLevel(1);
		da.GenerateLevel2.started += _ => _puckSimulator.GenerateLevel(2);
		da.GenerateLevel3.started += _ => _puckSimulator.GenerateLevel(3);
		da.GenerateLevel4.started += _ => _puckSimulator.GenerateLevel(4);
		da.GenerateLevel5.started += _ => _puckSimulator.GenerateLevel(5);
		da.GenerateLevel6.started += _ => _puckSimulator.GenerateLevel(6);
		da.GenerateLevel7.started += _ => _puckSimulator.GenerateLevel(7);
		da.GenerateLevel8.started += _ => _puckSimulator.GenerateLevel(8);
		da.GenerateLevel9.started += _ => _puckSimulator.GenerateLevel(9);
		da.GenerateEasierLevel.started += _ => _puckSimulator.GeneratePrevLevel();
		da.GenerateHarderLevel.started += _ => _puckSimulator.GenerateNextLevel();
		da.RegenerateCurrent.started += _ => _puckSimulator.RegenerateLevel();
	}

	private void OnResetLevelActionStarted(InputAction.CallbackContext context) {
		//RespawnLevel();
		_puckSimulator.RespawnLevel();
	}

	private void OnStepPucksActionStarted(InputAction.CallbackContext context) {
		if (!_puckSimulator.HasLevelStarted) {
			//TestSolution();
			if (_puckSimulator.HasLevelSpawned)
				_puckSimulator.PushSolutionPuck();
			else
				Debug.LogWarning("[LevelManager]: Cannot push solution Puck because no level is currently spawned.");
		} else {
			//StepSimulator();
			_puckSimulator.Step();
		}
	}

	/// <summary>
	/// Doesn't actually spawn a PuckMover. Rather, it takes from the _puckMoverPool and increments the _puckPoolHeader pointer.
	/// </summary>
	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	// TODO: replace
	//PuckMover SpawnPuckMover() {
	//	Assert.IsTrue(
	//		_puckPoolHeader < PUCK_MOVER_POOL_SIZE,
	//		$"[LevelManager]: Attempted to spawn another PuckMover but the pool has been used up! Pool size: {PUCK_MOVER_POOL_SIZE}."
	//	);
	//	return _puckMoverPool[_puckPoolHeader++];
	//}

	// TODO: replace
	//void CreatePuckMoverPool() {
	//	for (int i = 0; i < PUCK_MOVER_POOL_SIZE; i++) {
	//		PuckMover pm = Instantiate(PuckType switch {
	//			EPuckType.Quad => _quadPuckPrefab,
	//			EPuckType.Hex => _hexPuckPrefab,
	//			_ => throw new InvalidOperationException("[LevelManager]: CreatePuckMoverPool() called with an invalid EPuckType to use!")
	//		});
	//		pm.gameObject.SetActive(false);
	//		_puckMoverPool.Add(pm);
	//	}
	//}

	// TODO: replace
	//void DestroyPuckMoverPool() {
	//	for (int i = 0; i < PUCK_MOVER_POOL_SIZE; i++)
	//		Destroy(_puckMoverPool[i]);
	//	_puckMoverPool.Clear();
	//}

	Vector3 GetLerpedPosition(PuckNode p) => Vector3.Lerp(
		_puckSimulator.PointToPosition(p.PreviousGridPoint, PuckSize),
		_puckSimulator.PointToPosition(p.GridPoint, PuckSize),
		_timeSinceLastStep / StepUpdateDelay
	);

	/// <summary>
	/// Creates a 2d array representing the current level, where each Puck is represented purely by its movement direction.
	/// </summary>
	// TODO: replace
	//EPuckMovementDirection[,] LevelTo2dArrayDirectionOnly() {
	//	EPuckMovementDirection[,] grid = new EPuckMovementDirection[HeightCount, WidthCount];
	//	foreach (var (pos, puck) in _puckSimulator.GetStationaryPucks()) {
	//		grid[pos.x, pos.y] = puck.MovementDirection;
	//	}
	//	foreach (var puck in _puckSimulator.GetMovingPucks()) {
	//		switch (grid[puck.GridPoint.x, puck.GridPoint.y]) {
	//			case EPuckMovementDirection.Up:
	//			case EPuckMovementDirection.Down:
	//				grid[puck.GridPoint.x, puck.GridPoint.y] = EPuckMovementDirection.SplitVertical;
	//				break;
	//			case EPuckMovementDirection.Left:
	//			case EPuckMovementDirection.Right:
	//				grid[puck.GridPoint.x, puck.GridPoint.y] = EPuckMovementDirection.SplitHorizontal;
	//				break;
	//			case EPuckMovementDirection.None:
	//				grid[puck.GridPoint.x, puck.GridPoint.y] = puck.MovementDirection;
	//				break;
	//			default:
	//				break;
	//		}
	//	}
	//	return grid;
	//}

	/// <summary>
	/// Creates a 2d array representing the current generated level.
	/// </summary>
	// TODO: replace
	//EPuckMovementDirection[,] GetGeneratedLevelAs2dArray() {
	//	EPuckMovementDirection[,] grid = new EPuckMovementDirection[HeightCount, WidthCount];
	//	foreach (var (pos, puck) in _puckSimulator.GetStationaryPucks()) {
	//		grid[pos.x, pos.y] = puck.MovementDirection;
	//	}
	//	foreach (var puck in _puckSimulator.GetMovingPucks()) {
	//		switch (grid[puck.GridPoint.x, puck.GridPoint.y]) {
	//			case EPuckMovementDirection.Up:
	//			case EPuckMovementDirection.Down:
	//				grid[puck.GridPoint.x, puck.GridPoint.y] = EPuckMovementDirection.SplitVertical;
	//				break;
	//			case EPuckMovementDirection.Left:
	//			case EPuckMovementDirection.Right:
	//				grid[puck.GridPoint.x, puck.GridPoint.y] = EPuckMovementDirection.SplitHorizontal;
	//				break;
	//			case EPuckMovementDirection.None:
	//				grid[puck.GridPoint.x, puck.GridPoint.y] = puck.MovementDirection;
	//				break;
	//			default:
	//				break;
	//		}
	//	}
	//	return grid;
	//}

	void D_DrawLevelGridOutline(float duration=0f) {
		float3 box = new(WidthCount * PuckSize, HeightCount * PuckSize, PuckSize);
		Util.D_DrawBox(box + new float3(_positionOffset) - box / 2f, box, Color.black, duration, false);
	}

}
