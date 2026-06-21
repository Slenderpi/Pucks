using Pucks;
using Slenderpi.Utilities.CircularArray;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PuckMoverPool : MonoBehaviour {

	[Header("Configuration")]

	[Min(1)]
	[Tooltip("On Awake(), this number of PuckMovers will get spawned in the internal pool.\nNOTE: this value ALSO determines the minimum capacity.")]
	[SerializeField]
	int _startingCapacity = 32;

	[Min(0.1f)]
	[SerializeField]
	[Tooltip("If the average pool size becomes less than this percent of the current capacity, the capacity will be shrunken.")]
	float _shrinkAtPercent = 0.6f;

	[Min(1)]
	[Tooltip("When the internal Pool is full and another PuckMover is requested, the Pool will grow by this rate.\nWhen ShrinkPool() is called, the Pool is shrunk to Count + _capacityGrowth.")]
	[SerializeField]
	int _capacityGrowth = 10;

	[Min(0)]
	[SerializeField]
	[Tooltip("The amount of time (in seconds, realtime) between each shrink check. The Count is recorded to determine if the Pool can be shrunken to a smaller size.")]
	float _shrinkCheckDelay = 1f;
	WaitForSecondsRealtime _poolShrinkerWaiter;

	[Min(1)]
	[SerializeField]
	[Tooltip("The number of records of last recorded counts that will be maintained. This should be considered in context with _countRecordDelay")]
	int _shrinkCheckMemorySize = 64;

	[Header("PuckMover prefabs")]

	public PuckMover QuadPuck;
	public PuckMover OctaPuck;

	/// <summary>
	/// Internal Pool capacity.
	/// </summary>
	public int Capacity => Pool.Count;

	/// <summary>
	/// The number of PuckMovers currently active.
	/// </summary>
	public int Count => PoolPointer;

	/// <summary>
	/// Internal pool of pre-spawned PuckMovers.
	/// </summary>
	protected readonly List<PuckMover> Pool = new();
	/// <summary>
	/// Pointer within the Pool indicating the next available PuckMover that can be used.<br/>
	/// Also indicivative of the number of currently active PuckMovers.
	/// </summary>
	protected int PoolPointer = 0;

	/// <summary>
	/// A record of the last PoolPointer sizes.
	/// </summary>
	protected CircularArray<int> RecordedCounts;
	/// <summary>
	/// A running sum of the last PoolPointer sizes.
	/// </summary>
	protected int RunSum;
	/// <summary>
	/// Determines if the Capacity increased before the next ShrinkCheck.
	/// </summary>
	protected bool CapcityIncreasedThisShrinkCheck = false;



	private void Awake() {
		Assert.IsTrue(_shrinkAtPercent < 1, "$[PuckMoverPool]: The ShrinkAtPercent value of {_shrinkAtPercent} cannot be more than 1.");
		RecordedCounts = new(_shrinkCheckMemorySize);
		_poolShrinkerWaiter = new(_shrinkCheckDelay);
		LevelManager.A_OnPuckSimulatorChanged += OnPuckSimulatorChanged;
	}

	private void Start() {
		StartCoroutine(PoolShrinker());
	}

	private void OnDestroy() {
		LevelManager.A_OnPuckSimulatorChanged -= OnPuckSimulatorChanged;
		Destroy();
	}

	IEnumerator PoolShrinker() {
		while (true) {
			yield return _poolShrinkerWaiter;
			if (CapcityIncreasedThisShrinkCheck) {
				CapcityIncreasedThisShrinkCheck = false;
				RecordedCounts.SetAll(Capacity);
				RunSum = Capacity * _shrinkCheckMemorySize;
			} else {
				RunSum = RunSum - RecordedCounts.Peek() + PoolPointer;
				float avg = (float)RunSum / _shrinkCheckMemorySize;
				if (avg / Capacity < _shrinkAtPercent) {
					ShrinkPool();
				}
				RecordedCounts.Add(PoolPointer);
			}
		}
	}

	/// <summary>
	/// Sets the number of currently-spawned PuckMovers within the pool.<br/>
	/// Setting to a value higher than the current capacity will spawn extra PuckMovers.<br/>
	/// Setting to a value lower than the current capacity will destroy extra PuckMovers, unless this interferes with bound PuckMovers.
	/// </summary>
	/// <param name="capacity"></param>
	public void SetCapacity(int capacity=0) {
		Assert.IsTrue(capacity > 0, "[PuckMoverPool]: SetCapacity() must be given a positive value.");
		int diff = capacity - Capacity;
		if (diff > 0)
			InstantiatePuckMovers(diff);
		else if (diff < 0) {
			DestroyPuckMovers(-diff);
		}
	}

	/// <summary>
	/// Gives a PuckMover from the internal pool.<br/>
	/// The internal pool will be expanded as necessary.
	/// </summary>
	/// <returns></returns>
	public PuckMover SpawnPuckMover() {
		if (PoolPointer + _capacityGrowth >= Capacity)
			InstantiatePuckMovers(_capacityGrowth);
		return Pool[PoolPointer++];
	}

	/// <summary>
	/// Returns all PuckMovers back to the pool. It is up to you to handle removal of references to PuckMovers.
	/// </summary>
	public void Clear() {
		for (; PoolPointer > 0;)
			Pool[--PoolPointer].gameObject.SetActive(false);
	}

	/// <summary>
	/// Completely destroy the current pool of PuckMovers.<br/>
	/// To only return PuckMovers back to the pool, call Clear().
	/// </summary>
	public void Destroy() {
		foreach (PuckMover pm in Pool)
			if (pm != null && pm.gameObject != null)
				Destroy(pm.gameObject);
		Pool.Clear();
		PoolPointer = 0;
	}

	/// <summary>
	/// Shrinks Capacity to PoolPointer + _capacityGrowth (with a minimum Capacity size of _startingCapacity).
	/// </summary>
	public void ShrinkPool() {
		if (Capacity <= _startingCapacity)
			return;
		DestroyPuckMovers(Capacity - Mathf.Max(PoolPointer + _capacityGrowth, _startingCapacity));
	}

	void InstantiatePuckMovers(int num) {
		CapcityIncreasedThisShrinkCheck = true;
		for (int i = 0; i < num; i++) {
			PuckMover pm = Instantiate(LevelManager.Singleton.PuckType switch {
				EPuckType.Quad => QuadPuck,
				EPuckType.Octa => OctaPuck,
				_ => throw new Exception($"[PuckMoverPool]: The puck type {LevelManager.Singleton.PuckType} does not have a prefab set up for it. You must link it in the code here.")
			});
			pm.gameObject.SetActive(false);
			Pool.Add(pm);
		}
	}

	void DestroyPuckMovers(int num) {
		for (int i = 0; i < num; i++) {
			int end = Pool.Count - 1;
			Destroy(Pool[end].gameObject);
			Pool.RemoveAt(end);
		}
	}

	void OnPuckSimulatorChanged(EPuckType type) {
		Destroy();
		InstantiatePuckMovers(_startingCapacity);
	}

}
