using FMODUnity;
using UnityEngine;

public class Sound2dManager : MonoBehaviour {

	[SerializeField]
	EventReference _sfxPuckHit;



	private void Awake() {
		LevelManager.A_OnLevelStepped += OnLevelStepped;

		RuntimeManager.LoadBank("Master");
		RuntimeManager.LoadBank("SoundEffects");
		RuntimeManager.StudioSystem.getBank("bank:/SoundEffects", out FMOD.Studio.Bank bank);
		bank.loadSampleData();
	}

	private void OnDestroy() {
		LevelManager.A_OnLevelStepped -= OnLevelStepped;
	}

	void OnLevelStepped(int numCollisions) {
		if (numCollisions == 0)
			return;
		RuntimeManager.PlayOneShot(_sfxPuckHit);
	}

}
