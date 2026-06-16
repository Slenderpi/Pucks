using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PuckMoverPool : MonoBehaviour {

	[Header("Configuration")]

	[Min(1)]
	[Tooltip("On Awake(), this number of PuckMovers will get spawned in the internal pool.")]
	[SerializeField]
	int _startingCapacity = 100;

	[Min(1)]
	[Tooltip("When the internal Pool is full and another PuckMover is requested, the Pool will grow by this rate.\nWhen ShrinkPool() is called, the Pool is shrunk to Count + _capacityGrowth.")]
	[SerializeField]
	int _capacityGrowth = 10;

	[Header("PuckMover prefabs")]

	public PuckMover PuckMoverPrefab;

	/// <summary>
	/// Internal Pool capacity.
	/// </summary>
	public int Capacity => Pool.Count;

	/// <summary>
	/// Internal pool of pre-spawned PuckMovers.
	/// </summary>
	protected readonly List<PuckMover> Pool = new();
	/// <summary>
	/// Pointer within the Pool indicating the next available PuckMover that can be used.
	/// </summary>
	protected int PoolPointer = 0;



	private void Awake() {
		InstantiatePuckMovers(_startingCapacity);
	}

	private void OnDestroy() {
		Destroy();
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
		if (PoolPointer >= Capacity)
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

	///// <summary>
	///// Spawns a PuckMover and binds it to the given PuckNode.<br/>
	///// <br/>
	///// If no 
	///// </summary>
	///// <param name="p"></param>
	//public void BindPuckToMover(PuckNode p, Vector3 position, Quaternion rotation) {
	//	if (PoolPointer >= Count) {
	//		SpawnPooledPuckMovers(_capacityGrowth);
	//	}
	//	PuckMover pm = Pool[PoolPointer++];
	//	pm.transform.SetPositionAndRotation(position, rotation);
	//	pm.gameObject.SetActive(true);
	//	//pm.OnSpawned(puckManDists.BinarySearch(pos.x + pos.y));
	//	pm.OnSpawned(0);
	//	BoundPuckMovers.Add(p, pm);
	//}

	///// <summary>
	///// Destroys unused PuckMovers until the Pool size is at minimum Count + _capacityGrowth.
	///// </summary>
	//public void ShrinkPool() {
	//	int shrunkenSize = Count + _capacityGrowth;
	//	if (Capacity > shrunkenSize)
	//		DestroyPuckMovers(Capacity - shrunkenSize);
	//	else
	//		Debug.LogWarning("[PuckMoverPool]: ShrinkPool() was called but the Pool is already small enough.");
	//}

	///// <summary>
	///// Clears all currently bound PuckMovers.
	///// </summary>
	//public void ClearBoundPuckMovers() {
	//	PoolPointer = 0;
	//	foreach (var (_, pm) in BoundPuckMovers)
	//		pm.gameObject.SetActive(false);
	//	BoundPuckMovers.Clear();
	//}

	/// <summary>
	/// Completely destroy the current pool of PuckMovers.<br/>
	/// To only return PuckMovers back to the pool, call Clear().
	/// </summary>
	public void Destroy() {
		foreach (PuckMover pm in Pool)
			Destroy(pm);
		Pool.Clear();
		PoolPointer = 0;
	}

	void InstantiatePuckMovers(int num) {
		for (int i = 0; i < num; i++) {
			PuckMover pm = Instantiate(PuckMoverPrefab);
			//PuckMover pm = Instantiate(PuckType switch {
			//	EPuckType.Quad => _quadPuckPrefab,
			//	EPuckType.Hex => _hexPuckPrefab,
			//	_ => throw new InvalidOperationException("[LevelManager]: CreatePuckMoverPool() called with an invalid EPuckType to use!")
			//});
			pm.gameObject.SetActive(false);
			Pool.Add(pm);
		}
	}

	void DestroyPuckMovers(int num) {
		for (int i = 0; i < num; i++) {
			int end = Pool.Count - 1;
			Destroy(Pool[end]);
			Pool.RemoveAt(end);
		}
	}

}
