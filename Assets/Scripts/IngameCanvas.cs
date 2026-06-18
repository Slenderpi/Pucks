using Pucks;
using TMPro;
using UnityEngine;

public class IngameCanvas : MonoBehaviour {

    [SerializeField]
    TMP_Text _difficultyLabel;
	[SerializeField]
	TMP_Text _failedWarningLabel;

	int _consecutiveFailures = 0;



	private void Awake() {
		LevelManager.A_OnPuckSimulatorChanged += BindToPuckSimulator;
	}

	private void OnDestroy() {
		LevelManager.A_OnPuckSimulatorChanged -= BindToPuckSimulator;
	}

	void BindToPuckSimulator(EPuckType puckType) {
		LevelManager.Singleton.PuckSimulator.A_OnLevelSpawned += OnLevelSpawned;
		LevelManager.Singleton.PuckSimulator.A_OnLevelGenFailed += OnLevelGenerationFailed;
	}

	void OnLevelSpawned() {
		SetDifficultyLabelText(LevelManager.Singleton.PuckSimulator.CurrentDifficulty);

		_consecutiveFailures = 0;
		_failedWarningLabel.gameObject.SetActive(false);
	}

	void OnLevelGenerationFailed(int difficulty, int numGenProcessFails, int numUnsovlableFails) {
		SetDifficultyLabelText(difficulty);
		_consecutiveFailures++;
		string beginning = "LEVEL GENERATION FAILED!";
		if (_consecutiveFailures > 1)
			beginning += " x" + _consecutiveFailures;
		_failedWarningLabel.SetText($"{beginning}\nPress G to regenerate.\nGeneration fails: {numGenProcessFails} | Solvability fails: {numUnsovlableFails}");
		_failedWarningLabel.gameObject.SetActive(true);
	}

	void SetDifficultyLabelText(int difficulty) {
		_difficultyLabel.SetText("LEVEL: " + (difficulty < 0 ? "--" : difficulty.ToString()));
	}

}
