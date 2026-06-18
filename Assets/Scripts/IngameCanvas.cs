using Pucks;
using TMPro;
using UnityEngine;

public class IngameCanvas : MonoBehaviour {

    [SerializeField]
    TMP_Text _difficultyLabel;
	[SerializeField]
	TMP_Text _failedWarningLabel;
	[SerializeField]
	GameObject _wonPanel;
	[SerializeField]
	GameObject _lostPanel;

	int _consecutiveFailures = 0;



	private void Awake() {
		LevelManager.A_OnPuckSimulatorChanged += BindToPuckSimulator;
		_failedWarningLabel.gameObject.SetActive(false);
		_wonPanel.SetActive(false);
		_lostPanel.SetActive(false);
	}

	private void OnDestroy() {
		LevelManager.A_OnPuckSimulatorChanged -= BindToPuckSimulator;
	}

	void BindToPuckSimulator(EPuckType puckType) {
		LevelManager.Singleton.PuckSimulator.A_OnLevelSpawned += OnLevelSpawned;
		LevelManager.Singleton.PuckSimulator.A_OnLevelGenFailed += OnLevelGenerationFailed;
		LevelManager.Singleton.PuckSimulator.A_OnLevelWon += OnLevelWon;
		LevelManager.Singleton.PuckSimulator.A_OnLevelLost += OnLevelLost;
	}

	void OnLevelSpawned() {
		SetDifficultyLabelText(LevelManager.Singleton.PuckSimulator.CurrentDifficulty);

		_consecutiveFailures = 0;
		_failedWarningLabel.gameObject.SetActive(false);
		_wonPanel.SetActive(false);
		_lostPanel.SetActive(false);
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

	void OnLevelWon() {
		_wonPanel.SetActive(true);
	}

	void OnLevelLost() {
		_lostPanel.SetActive(true);
	}

	void SetDifficultyLabelText(int difficulty) {
		_difficultyLabel.SetText("LEVEL: " + (difficulty < 0 ? "--" : difficulty.ToString()));
	}

}
