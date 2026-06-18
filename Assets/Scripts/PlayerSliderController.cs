using Pucks;
using Slenderpi.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSliderControl : MonoBehaviour {

	[SerializeField]
	[Tooltip("The mouse must be dragged a distance at least this value to be considered as selecting and moving a Slider.")]
	float _dragDeadzone = 0.33f;

	[SerializeField]
	LineRenderer _dragLine;

	bool _isDragging;
	PuckNode _selectedPuck;
	PuckMover _hoveredPuckMover;
	Vector3 _mouseSelectStart;



	private void Awake() {
		_dragLine.widthMultiplier = 0.1f;
		_dragLine.enabled = false;
	}

	private void Start() {
		GameManager.PlayerActions.SelectSlider.started += OnSelectSliderStarted;
		GameManager.PlayerActions.SelectSlider.canceled += OnSelectSliderCanceled;
		GameManager.PlayerActions.Enable();
	}

	// The below code provides helpful visuals during mouse dragging
	// To see these visuals in-game, make sure the "Gizmos" button in the top right of the Game window is toggled on
	private void Update() {
		//if (LevelManager.Singleton.PuckSimulator.HasLevelStarted)
		//	return;
		if (_isDragging) {
			// TODO: replace
			Vector3 mpos = GetMouseWorldPosition();
			Vector3 dragVector = mpos - _mouseSelectStart;
			if (SuccessfullySelectedPuck()) {
				Vector3 puckPos = LevelManager.Singleton.PointToPosition(_selectedPuck.GridPoint);
				Vector3 alignedDragVector = (Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.y) ? new Vector3(Mathf.Sign(dragVector.x), 0, 0) : new Vector3(0, Mathf.Sign(dragVector.y), 0));
				_dragLine.SetPosition(1, alignedDragVector * 5000 + _dragLine.GetPosition(0));
				if (IsDragDistBigEnough(dragVector)) {
					if (!_dragLine.enabled)
						_dragLine.enabled = true;
					Util.D_DrawBox(puckPos, new(LevelManager.Singleton.PuckSize + 0.01f), Color.cyan); // slider
					Util.D_DrawArrowFromTo(_mouseSelectStart, mpos, Color.green); // dragVector
					Util.D_DrawArrowFromTo(
						puckPos,
						alignedDragVector * 2f + puckPos,
						Color.cyan
					); // moveDir
				} else {
					if (_dragLine.enabled)
						_dragLine.enabled = false;
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
			// TODO: replace
			PuckNode p = LevelManager.Singleton.GetPuckAt(GetMouseWorldPosition());
			if (p == null) {
				if (_hoveredPuckMover != null) {
					_hoveredPuckMover.OnHoverEnd();
					_hoveredPuckMover = null;
				}
			} else {
				PuckMover pm = LevelManager.GetPuckMoverFromPuck(p);
				if (_hoveredPuckMover == null) {
					_hoveredPuckMover = pm;
					_hoveredPuckMover.OnHoverBegin();
				} else if (pm != _hoveredPuckMover) {
					_hoveredPuckMover.OnHoverEnd();
					_hoveredPuckMover = pm;
					pm.OnHoverBegin();
				}
				Util.D_DrawBox(
					LevelManager.Singleton.PointToPosition(p.GridPoint),
					LevelManager.Singleton.PuckSize + 0.01f, Color.red, 0, false
				);
			}
		}
	}

	private void OnSelectSliderStarted(InputAction.CallbackContext _) {
		if (LevelManager.Singleton.PuckSimulator.HasLevelStarted)
			return;
		_isDragging = true;
		_mouseSelectStart = GetMouseWorldPosition();
		// TODO: replace
		_selectedPuck = LevelManager.Singleton.GetPuckAt(_mouseSelectStart);
		//_selectedPuck = LevelManager.GetPuckAt(_mouseSelectStart);
		if (SuccessfullySelectedPuck()) {
			_hoveredPuckMover = LevelManager.GetPuckMoverFromPuck(_selectedPuck);
			_hoveredPuckMover.OnSelectBegin();
			// TODO: replace
			_dragLine.SetPosition(0, LevelManager.Singleton.PointToPosition(_selectedPuck.GridPoint));
			//_dragLine.SetPosition(0, LevelManager.Singleton.PointToPosition(_selectedPuck.GridPoint));
		}
	}

	private void OnSelectSliderCanceled(InputAction.CallbackContext _) {
		if (LevelManager.Singleton.PuckSimulator.HasLevelStarted)
			return;
		_isDragging = false;
		_dragLine.enabled = false;
		if (_hoveredPuckMover != null) {
			_hoveredPuckMover.OnSelectEnd();
			_hoveredPuckMover = null;
		}
		Vector3 dragVector = GetMouseWorldPosition() - _mouseSelectStart;
		if (!SuccessfullySelectedPuck() || !IsDragDistBigEnough(dragVector))
			return;
		// TODO: replace
		LevelManager.Singleton.PuckSimulator.PushPuck(
			_selectedPuck.GridPoint,
			LevelManager.Singleton.PuckSimulator.DragVectorToDirection(dragVector)
		);
		//LevelManager.StartLevelWithChoice(_selectedPuck.GridPoint, GetMovementDirection(dragVector));
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

}