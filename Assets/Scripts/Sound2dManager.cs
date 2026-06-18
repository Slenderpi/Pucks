using FMODUnity;
using Pucks;
using UnityEngine;

public class Sound2dManager : MonoBehaviour {

	[SerializeField]
	EventReference _sfxPuckHit;



	private void Awake() {
		LevelManager.A_OnPuckSimulatorChanged += BindToPuckSimulator;

		RuntimeManager.LoadBank("Master");
		RuntimeManager.LoadBank("SoundEffects");
		RuntimeManager.StudioSystem.getBank("bank:/SoundEffects", out FMOD.Studio.Bank bank);
		bank.loadSampleData();
	}

	private void OnDestroy() {
		LevelManager.A_OnPuckSimulatorChanged -= BindToPuckSimulator;
	}

	void BindToPuckSimulator(EPuckType puckType) {
		LevelManager.Singleton.PuckSimulator.A_OnLevelStepped += OnLevelStepped;
	}

	void OnLevelStepped(int numCollisions) {
		if (numCollisions == 0)
			return;
		RuntimeManager.PlayOneShot(_sfxPuckHit);
	}

}
