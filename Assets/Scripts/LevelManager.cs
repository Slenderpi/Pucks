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

	/// <summary>
	/// Broadcast when the current PuckSimulator changes.<br/>
	/// EPuckType: the EPuckType enum specified for the change.
	/// </summary>
	public static Action<EPuckType> A_OnPuckSimulatorChanged;

	public static LevelManager Singleton;

	public EPuckType PuckType = EPuckType.Quad;

	[Tooltip("Reference to a PuckMoverPool.")]
	public PuckMoverPool PuckMoverPool;

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

	[Header("Debug")]
	[SerializeField]
	bool _updateManually = false;
	[SerializeField]
	int _startingDifficulty = 0;

	/// <summary>
	/// Access the current PuckSimulator.
	/// </summary>
	public PuckSimulation PuckSimulator => _puckSimulator;
	PuckSimulation _puckSimulator;

	/// <summary>
	/// Hashmap of currently active PuckMovers.
	/// </summary>
	readonly Dictionary<PuckNode, PuckMover> _activePuckMovers = new();

	float _timeSinceLastStep = 0;
	Vector3 _positionOffset = new();



	private void Awake() {
		Assert.IsNotNull(PuckMoverPool, "[LevelManager]: Missing reference to PuckMoverPool.");
		if (!Singleton) {
			Singleton = this;

			ChangePuckSimulator(EPuckType.Quad);
			_positionOffset = new(WidthCount * PuckSize / -2f, HeightCount * PuckSize / -2f, 0);
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

	/// <summary>
	/// Get the PuckNode at a world position.
	/// </summary>
	/// <param name="position"></param>
	/// <returns>null if there is no Puck there.</returns>
	public PuckNode GetPuckAt(Vector3 position) => _puckSimulator.GetPuckAt(position - _positionOffset, PuckSize);

	/// <summary>
	/// Converts from GridPoint to world position.
	/// </summary>
	/// <param name="point"></param>
	/// <returns></returns>
	public Vector3 PointToPosition(Vector2Int point) => _puckSimulator.PointToPosition(point, PuckSize) + _positionOffset;

	/// <summary>
	/// Converts from world position to GridPoint.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Vector2Int PositionToPoint(Vector3 position) => _puckSimulator.PositionToPoint(position - _positionOffset, PuckSize);

	/// <summary>
	/// Get the PuckMover bound to a specific PuckNode.
	/// </summary>
	/// <param name="puckNode"></param>
	/// <returns></returns>
	public static PuckMover GetPuckMoverFromPuck(PuckNode puckNode) => Singleton._activePuckMovers[puckNode];

	void OnLevelSpawned() {
		_timeSinceLastStep = 0f;
		BindPuckMoversToSpawnedLevel();
	}

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
		_puckSimulator.RespawnLevel();
	}

	private void OnStepPucksActionStarted(InputAction.CallbackContext context) {
		if (!_puckSimulator.HasLevelStarted) {
			if (_puckSimulator.HasLevelSpawned)
				_puckSimulator.PushSolutionPuck();
			else
				Debug.LogWarning("[LevelManager]: Cannot push solution Puck because no level is currently spawned.");
		} else {
			_puckSimulator.Step();
		}
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

	Vector3 GetLerpedPosition(PuckNode p) => Vector3.Lerp(
		_puckSimulator.PointToPosition(p.PreviousGridPoint, PuckSize),
		_puckSimulator.PointToPosition(p.GridPoint, PuckSize),
		_timeSinceLastStep / StepUpdateDelay
	);

	void D_DrawLevelGridOutline(float duration=0f) {
		float3 box = new(WidthCount * PuckSize, HeightCount * PuckSize, PuckSize);
		Util.D_DrawBox(box + new float3(_positionOffset) - box / 2f, box, Color.black, duration, false);
	}

}
