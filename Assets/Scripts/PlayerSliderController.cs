using Pucks;
using Slenderpi.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSliderControl : MonoBehaviour {

	public static PlayerSliderControl Singleton;

	[SerializeField]
	[Tooltip("The mouse must be dragged a distance at least this value to be considered as selecting and moving a Slider.")]
	float _dragDeadzone = 0.33f;

	[SerializeField]
	LineRenderer _dragLine;

	public Vector2 MouseScreenPosition {
		get => _mouseScreenPosition;
		private set {
			_mouseScreenPosition = value;
			if (Camera.main != null) {
				MouseWorldPosition = Camera.main.ScreenToWorldPoint(new(_mouseScreenPosition.x, _mouseScreenPosition.y, Mathf.Abs(Camera.main.transform.position.z)));
				if (LevelManager.Singleton != null && LevelManager.Singleton.PuckSimulator != null)
					MouseGridPoint = LevelManager.Singleton.PositionToPoint(MouseWorldPosition);
			}
		}
	}
	Vector2 _mouseScreenPosition;
	public Vector3 MouseWorldPosition { get; private set; }
	public Vector2Int MouseGridPoint { get; private set; }

	bool _isDragging;
	PuckNode _selectedPuck;
	PuckMover _hoveredPuckMover;
	Vector3 _mouseSelectStart;



	private void Awake() {
		if (!Singleton) {
			Singleton = this;

			_dragLine.widthMultiplier = 0.1f;
			_dragLine.enabled = false;
		} else if (Singleton != this) {
			Destroy(gameObject);
		}
	}

	private void Start() {
		GameManager.PlayerActions.SelectSlider.started += OnSelectSliderStarted;
		GameManager.PlayerActions.SelectSlider.canceled += OnSelectSliderCanceled;
		GameManager.PlayerActions.MoveMouse.performed += (InputAction.CallbackContext context) => {
			MouseScreenPosition = context.ReadValue<Vector2>();
		};
		GameManager.PlayerActions.Enable();
	}

	// The below code provides helpful visuals during mouse dragging
	// To see these visuals in-game, make sure the "Gizmos" button in the top right of the Game window is toggled on
	private void Update() {
		if (LevelManager.Singleton.PuckSimulator.HasLevelStarted)
			return;
		if (_isDragging) {
			Vector3 dragVector = MouseWorldPosition - _mouseSelectStart;
			if (SuccessfullySelectedPuck()) {
				Vector3 puckPos = LevelManager.Singleton.PointToPosition(_selectedPuck.GridPoint);
				Vector2Int dragVectAsDir = LevelManager.Singleton.PuckSimulator.DragVectorToDirection(dragVector);
				Vector3 alignedDragVector = new(dragVectAsDir.y, -dragVectAsDir.x, 0);
				_dragLine.SetPosition(1, alignedDragVector * 5000 + _dragLine.GetPosition(0));
				if (IsDragDistBigEnough(dragVector)) {
					if (!_dragLine.enabled)
						_dragLine.enabled = true;
					Util.D_DrawBox(puckPos, new(LevelManager.Singleton.PuckSize + 0.01f), Color.cyan); // slider
					Util.D_DrawArrowFromTo(_mouseSelectStart, MouseWorldPosition, Color.green); // dragVector
					Util.D_DrawArrowFromTo(
						puckPos,
						alignedDragVector * 2f + puckPos,
						Color.cyan
					); // moveDir
				} else {
					if (_dragLine.enabled)
						_dragLine.enabled = false;
					Util.D_DrawBox(puckPos, new(LevelManager.Singleton.PuckSize + 0.01f), Color.yellow); // slider
					Util.D_DrawArrowFromTo(_mouseSelectStart, MouseWorldPosition, Color.yellow); // dragVector
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
			PuckNode p = LevelManager.Singleton.GetPuckAt(MouseWorldPosition);
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
		_mouseSelectStart = MouseWorldPosition;
		_selectedPuck = LevelManager.Singleton.GetPuckAt(_mouseSelectStart);
		if (SuccessfullySelectedPuck()) {
			_hoveredPuckMover = LevelManager.GetPuckMoverFromPuck(_selectedPuck);
			_hoveredPuckMover.OnSelectBegin();
			_dragLine.SetPosition(0, LevelManager.Singleton.PointToPosition(_selectedPuck.GridPoint));
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
		Vector3 dragVector = MouseWorldPosition - _mouseSelectStart;
		if (!SuccessfullySelectedPuck() || !IsDragDistBigEnough(dragVector))
			return;
		LevelManager.Singleton.PuckSimulator.PushPuck(
			_selectedPuck.GridPoint,
			LevelManager.Singleton.PuckSimulator.DragVectorToDirection(dragVector)
		);
		_selectedPuck = null;
	}

	bool SuccessfullySelectedPuck() => _selectedPuck != null;

	bool IsDragDistBigEnough(Vector3 dragVector) => Vector3.SqrMagnitude(dragVector) >= Util.pow2(_dragDeadzone);

	public static Vector3 Dev_GetMouseWorldPosition() => Singleton.MouseWorldPosition;
	public static Vector2Int Dev_GetMouseGridPoint() => Singleton.MouseGridPoint;

}