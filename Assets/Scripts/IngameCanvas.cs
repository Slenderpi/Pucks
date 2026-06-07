using TMPro;
using UnityEngine;

public class IngameCanvas : MonoBehaviour {

    [SerializeField]
    TMP_Text _difficultyLabel;



	private void Awake() {
		LevelManager.A_OnLevelSpawned += OnLevelSpawned;
	}

	private void OnDestroy() {
		LevelManager.A_OnLevelSpawned -= OnLevelSpawned;
	}

	void OnLevelSpawned(int difficulty) {
		_difficultyLabel.SetText("LEVEL: " + (difficulty < 0 ? "--" : difficulty.ToString()));
	}

}
