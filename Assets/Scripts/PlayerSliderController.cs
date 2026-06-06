using Pucks;
using Slenderpi.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSliderControl : MonoBehaviour {

	[SerializeField]
	[Tooltip("The mouse must be dragged a distance at least this value to be considered as selecting and moving a Slider.")]
	float _dragDeadzone = 0.33f;

	bool _isDragging;
	PuckNode _selectedPuck;
	Vector3 _mouseSelectStart;



	private void Start() {
		GameManager.PlayerActions.SelectSlider.started += OnSelectSliderStarted;
		GameManager.PlayerActions.SelectSlider.canceled += OnSelectSliderCanceled;
		GameManager.PlayerActions.Enable();
	}

	// The below code provides helpful visuals during mouse dragging
	// To see these visuals in-game, make sure the "Gizmos" button in the top right of the Game window is toggled on
	private void Update() {
		if (_isDragging) {
			Vector3 mpos = GetMouseWorldPosition();
			Vector3 dragVector = mpos - _mouseSelectStart;
			if (SuccessfullySelectedPuck()) {
				Vector3 puckPos = LevelManager.Singleton.PointToPosition(_selectedPuck.GridPoint);
				if (IsDragDistBigEnough(dragVector)) {
					Util.D_DrawBox(puckPos, new(LevelManager.Singleton.PuckSize + 0.01f), Color.cyan); // slider
					Util.D_DrawArrowFromTo(_mouseSelectStart, mpos, Color.green); // dragVector
					Util.D_DrawArrowFromTo(
						puckPos,
						(Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.y) ? new Vector3(Mathf.Sign(dragVector.x), 0, 0) : new Vector3(0, Mathf.Sign(dragVector.y), 0)) * 2f + puckPos,
						Color.cyan
					); // moveDir
				} else {
					Util.D_DrawBox(puckPos, new(LevelManager.Singleton.PuckSize + 0.01f), Color.yellow); // slider
					Util.D_DrawArrowFromTo(_mouseSelectStart, mpos, Color.yellow); // dragVector
				}
			} else {
				Debug.DrawRay( // dragVector
					_mouseSelectStart,
					dragVector,
					Color.red,
					duration: 0,
					depthTest: false
				);
			}
		} else {
			PuckNode p = LevelManager.GetPuckAt(GetMouseWorldPosition());
			if (p == null)
				return;
			Util.D_DrawBox(
				LevelManager.Singleton.PointToPosition(p.GridPoint),
				LevelManager.Singleton.PuckSize + 0.01f, Color.red, 0, false
			);
		}
	}

	private void OnSelectSliderStarted(InputAction.CallbackContext _) {
		_isDragging = true;
		_mouseSelectStart = GetMouseWorldPosition();
		_selectedPuck = LevelManager.GetPuckAt(_mouseSelectStart);
	}

	private void OnSelectSliderCanceled(InputAction.CallbackContext _) {
		_isDragging = false;
		Vector3 dragVector = GetMouseWorldPosition() - _mouseSelectStart;
		if (!SuccessfullySelectedPuck() || !IsDragDistBigEnough(dragVector))
			return;
		LevelManager.StartLevelWithChoice(_selectedPuck.GridPoint, GetMovementDirection(dragVector));
		_selectedPuck = null;
	}

	Vector3 GetMouseWorldPosition() {
		Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
		return new Plane(Vector3.back, Vector3.zero).Raycast(ray, out float distance)
			? ray.GetPoint(distance)
			: Vector3.zero;
	}

	bool SuccessfullySelectedPuck() => _selectedPuck != null;

	bool IsDragDistBigEnough(Vector3 dragVector) => Vector3.SqrMagnitude(dragVector) >= Util.pow2(_dragDeadzone);

	EPuckMovementDirection GetMovementDirection(Vector3 dragVector) =>
		Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.y)
		? (dragVector.x > 0 ? EPuckMovementDirection.Right : EPuckMovementDirection.Left)
		: (dragVector.y > 0 ? EPuckMovementDirection.Up : EPuckMovementDirection.Down);

}