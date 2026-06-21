using NUnit.Framework;
using Pucks;
using Pucks.Level;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class DevCanvas : MonoBehaviour {

	internal const string STR_TRUE = "<color=green>true</color>";
	internal const string STR_FALSE = "<color=red>false</color>";
	internal const string STR_ON = "<color=green>ON</color>";
	internal const string STR_OFF = "<color=red>OFF</color>";
	internal const string STR_COL_NUMBER = "yellow";
	internal const string STR_COL_COMMON = "#00ffff";
	internal const string STR_COL_CONSOLE_TIMESTAMP = "#cdcdcd";

	public bool IsVisible {
		get => _devCanvas.enabled;
		set {
			_devCanvas.enabled = value;
		}
	}

	[Header("General Dev Canvas")]
	[SerializeField]
	Canvas _devCanvas;

	[Header("Dev Info")]
	public GameObject DevInfoPanel;
	public TMP_Text InfoText;

	[Header("Dev Controls")]
	public GameObject DevControlsPanel;
	public Button ToggleInfoPanelButton;
	TMP_Text ToggleInfoPanelButtonText;
	public Button ToggleConsoleOutputPanelButton;
	TMP_Text ToggleConsoleOutputoPanelButtonText;

	[Header("Dev Console")]
	public GameObject DevConsoleOutputPanel;
	public TMP_Text ConsoleOutputText;

	public readonly StringBuilder InfoStrb = new();

	/// <summary>
	/// numGenProcessFails, numUnsovlableFails
	/// </summary>
	Vector2Int _lastLevelGenFailures = new();
	bool _lastLevelGenFailed = false;
	int _lastStepCollisions = 0;
	Vector2Int _pushedPuckGridpoint = new();
	Vector2Int _pushedPuckDirection = new();



	private void Awake() {
		Assert.IsNotNull(_devCanvas, "[DevCanvas]: _devCanvas reference not set.");
		Assert.IsNotNull(DevInfoPanel, "[DevCanvas]: DevInfoPanel reference not set.");

		LevelManager.A_OnPuckSimulatorChanged += OnPuckSimulatorChanged;

		_devCanvas.enabled = false;
	}

	private void Start() {
		BindDebugActions();
		BindDevControls();
	}

	private void Update() {
		UpdateInfoText();
	}

	private void OnDestroy() {
		LevelManager.A_OnPuckSimulatorChanged -= OnPuckSimulatorChanged;
	}

	void OnPuckSimulatorChanged(EPuckType type) {
		PuckSimulation ps = LevelManager.Singleton.PuckSimulator;
		ps.A_OnLevelGenHadAnyFail += (numGenProcessFails, numUnsovlableFails) => {
			_lastLevelGenFailed = true;
			_lastLevelGenFailures = new(numGenProcessFails, numUnsovlableFails);
		};
		ps.A_OnLevelGenerated += () => {
			if (_lastLevelGenFailed)
				_lastLevelGenFailed = false;
			else
				_lastLevelGenFailures = new();
		};
		ps.A_OnLevelStepped += (numCollisions) => {
			_lastStepCollisions = numCollisions;
		};
		ps.A_OnPuckPushed += (p) => {
			_pushedPuckGridpoint = p.GridPoint;
			_pushedPuckDirection = p.Direction;
		};
	}

	void UpdateInfoText() {
		InfoStrb.Clear();
		bool hasLMS = LevelManager.Singleton != null;
		PuckSimulation ps = hasLMS ? LevelManager.Singleton.PuckSimulator : null;
		bool hasPS = ps != null;

		// PuckType | CurrentDifficulty
		if (!hasLMS)
			InfoStrb.AppendLine("PuckType: ~ | CurrentDifficulty: ~");
		else {
			InfoStrb.Append("PuckType: ")
					.Append(WrapWithColor(LevelManager.Singleton.PuckType.ToString(), STR_COL_COMMON));
			if (!hasPS)
				InfoStrb.AppendLine(" | CurrentDifficulty: ~");
			else {
				AppendInt(InfoStrb.Append(" | CurrentDifficulty: "), ps.CurrentDifficulty);
				InfoStrb.AppendLine();
			}
		}

		// Level gen fails | Unsolvable gens
		if (!hasPS)
			InfoStrb.AppendLine("Level gen fails: ~ | Unsolvable gens: ~");
		else {
			if (_lastLevelGenFailures.x > 0 || _lastLevelGenFailures.y > 0) {
				AppendInt(InfoStrb.Append("Level gen fails: "), _lastLevelGenFailures.x);
				AppendInt(InfoStrb.Append(" | Unsolvable gens: "), _lastLevelGenFailures.y);
				InfoStrb.AppendLine();
			} else
				InfoStrb.AppendLine("Level gen fails: 0 | Unsolvable gens: 0");
		}

		// Spawned | Started | Ended
		if (!hasPS)
			InfoStrb.AppendLine("Spawned: ~ | Started: ~ | Ended: ~");
		else {
			InfoStrb.Append("Spawned: ").Append(BoolAsString(ps.HasLevelSpawned))
					.Append(" | Started: ").Append(BoolAsString(ps.HasLevelStarted))
					.Append(" | Ended: ").Append(BoolAsString(ps.HasLevelEnded));
			InfoStrb.AppendLine();
		}

		// Stationary | Moving | Exited
		if (!hasPS)
			InfoStrb.AppendLine("Stationary: ~ | Moving: ~ | Exited: ~");
		else {
			AppendInt(InfoStrb.Append("Stationary: "), ps.NumStationaryPucks);
			AppendInt(InfoStrb.Append(" | Moving: "), ps.NumMovingPucks);
			AppendInt(InfoStrb.Append(" | Exited: "), ps.NumExitedPuck);
			InfoStrb.AppendLine();
		}

		InfoStrb.AppendLine();

		// Pushed Puck
		if (!hasPS)
			InfoStrb.AppendLine("Pushed Puck: ~");
		else if (!ps.HasLevelStarted)
			InfoStrb.AppendLine("Pushed Puck: not yet pushed");
		else {
			InfoStrb.Append("Pushed Puck: ").Append(WrapWithColor(_pushedPuckGridpoint.ToString(), STR_COL_COMMON));
			InfoStrb.Append(" with direction ").Append(WrapWithColor(_pushedPuckDirection.ToString(), STR_COL_COMMON));
			InfoStrb.AppendLine();
		}

		// Step | Collisions
		if (!hasPS)
			InfoStrb.AppendLine("Step: ~ | Collisions: ~");
		else {
			AppendInt(InfoStrb.Append("Step: "), ps.StepCount);
			AppendInt(InfoStrb.Append(" | Collisions: "), _lastStepCollisions);
			InfoStrb.AppendLine();
		}

		// Manual step mode | Step delay
		if (!hasLMS)
			InfoStrb.AppendLine("Manual stepping: ~ | Step update delay: ~");
		else {
			InfoStrb.Append("Manual stepping: ")
					.Append(LevelManager.Singleton.UpdateManually ? STR_ON : STR_OFF);
			AppendFloat(InfoStrb.Append(" | Step update delay: "), LevelManager.Singleton.StepUpdateDelay);
			InfoStrb.AppendLine();
		}

		InfoStrb.AppendLine();

		// PuckMoverPool Capacity | In use
		if (!hasLMS || LevelManager.Singleton.PuckMoverPool == null) {
			InfoStrb.AppendLine("PuckMoverPool Capacity: ~ | In use: ~");
		} else {
			PuckMoverPool pmp = LevelManager.Singleton.PuckMoverPool;
			AppendInt(InfoStrb.Append("PuckMoverPool Capacity: "), pmp.Capacity);
			AppendInt(InfoStrb.Append(" | In use: "), pmp.Count);
			InfoStrb.AppendLine();
		}

		// SET TMP TEXT
		InfoText.SetText(InfoStrb.ToString());
	}

	internal string BoolAsString(bool b) => b ? STR_TRUE : STR_FALSE;

	/// <summary>
	/// Wraps the provided string with "&lt;color=color&gt;text&lt;/color&gt;".
	/// </summary>
	/// <param name="text">Text to be colored.</param>
	/// <param name="color">Either #xxxxxx or black, blue, green, orange, purple, red, white, or yellow.</param>
	internal StringBuilder WrapWithColor(string text, string color) {
		StringBuilder sb = new(text.Length + 23);
		sb.Append("<color=").Append(color).Append('>').Append(text).Append("</color>");
		return sb;
	}

	/// <summary>
	/// Wraps the provided int with "&lt;color=color&gt;int&lt;/color&gt;".
	/// </summary>
	/// <param name="text">Int to be colored.</param>
	/// <param name="color">Either #xxxxxx or black, blue, green, orange, purple, red, white, or yellow.</param>
	internal StringBuilder WrapWithColor(int x, string color) {
		StringBuilder sb = new(8 + 23);
		sb.Append("<color=").Append(color).Append('>').Append(x).Append("</color>");
		return sb;
	}

	/// <summary>
	/// Wraps the provided float with "&lt;color=color&gt;float&lt;/color&gt;".
	/// </summary>
	/// <param name="text">Float to be colored.</param>
	/// <param name="color">Either #xxxxxx or black, blue, green, orange, purple, red, white, or yellow.</param>
	internal StringBuilder WrapWithColor(float x, string color) {
		StringBuilder sb = new(8 + 23);
		sb.Append("<color=").Append(color).Append('>').Append(x).Append("</color>");
		return sb;
	}

	/// <summary>
	/// Wraps the provided string with "&lt;b&gt;text&lt;/b&gt;".
	/// </summary>
	/// <param name="text">Text to be bolded.</param>
	internal StringBuilder WrapWithBold(string text) {
		return new StringBuilder(text.Length + 7).Append("<b>").Append(text).Append("</b>");
	}

	/// <summary>
	/// Wraps the provided string with "&lt;i&gt;text&lt;/i&gt;".
	/// </summary>
	/// <param name="text">Text to be italicized.</param>
	internal StringBuilder WrapWithItalics(string text) {
		return new StringBuilder(text.Length + 7).Append("<i>").Append(text).Append("</i>");
	}

	/// <summary>
	/// Wraps the provided string with "&lt;u&gt;text&lt;/u&gt;".
	/// </summary>
	/// <param name="text">Text to be underlined.</param>
	internal StringBuilder WrapWithUnderline(string text) {
		return new StringBuilder(text.Length + 7).Append("<u>").Append(text).Append("</u>");
	}

	/// <summary>
	/// Wraps the provided string with "&lt;s&gt;text&lt;/s&gt;".
	/// </summary>
	/// <param name="text">Text to have strikethrough.</param>
	internal StringBuilder WrapWithStrikethrough(string text) {
		return new StringBuilder(text.Length + 7).Append("<s>").Append(text).Append("</s>");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal StringBuilder AppendInt(StringBuilder strb, int x) {
		return strb.Append(WrapWithColor(x, STR_COL_NUMBER));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal StringBuilder AppendFloat(StringBuilder strb, float x) {
		return strb.Append(WrapWithColor(x, STR_COL_NUMBER));
	}

	private void BindDebugActions() {
		var da = GameManager.DebugActions;
		da.ToggleDevUi.started += _ => IsVisible = !IsVisible;
	}

	void BindDevControls() {
		ToggleInfoPanelButtonText = ToggleInfoPanelButton.GetComponentInChildren<TMP_Text>();
		ToggleInfoPanelButton.onClick.AddListener(() => {
			if (DevInfoPanel.activeSelf) {
				DevInfoPanel.SetActive(false);
				ToggleInfoPanelButtonText.SetText("Show INFO");
			} else {
				DevInfoPanel.SetActive(true);
				ToggleInfoPanelButtonText.SetText("Hide INFO");
			}
		});
		ToggleInfoPanelButtonText.SetText(DevInfoPanel.activeSelf ? "Hide INFO" : "Show INFO");

		ToggleConsoleOutputoPanelButtonText = ToggleConsoleOutputPanelButton.GetComponentInChildren<TMP_Text>();
		ToggleConsoleOutputPanelButton.onClick.AddListener(() => {
			if (DevConsoleOutputPanel.activeSelf) {
				DevConsoleOutputPanel.SetActive(false);
				ToggleConsoleOutputoPanelButtonText.SetText("Show CONSOLE");
			} else {
				DevConsoleOutputPanel.SetActive(true);
				ToggleConsoleOutputoPanelButtonText.SetText("Hide CONSOLE");
			}
		});
		ToggleConsoleOutputoPanelButtonText.SetText(DevConsoleOutputPanel.activeSelf ? "Hide CONSOLE" : "Show CONSOLE");
	}

}