using Slenderpi.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSliderControl : MonoBehaviour {

	[SerializeField]
	[Tooltip("The mouse must be dragged a distance at least this value to be considered as selecting and moving a Slider.")]
	float _dragDeadzone = 0.15f;

	bool _isDragging;
	//Entity _selectedSlider;
	PuckMover _selectedPuck;
	Vector3 _mouseSelectStart;



	private void Start() {
		GameManager.PlayerActions.SelectSlider.started += OnSelectSliderStarted;
		GameManager.PlayerActions.SelectSlider.canceled += OnSelectSliderCanceled;
		GameManager.PlayerActions.Enable();
	}

	// The below code provides helpful visuals during mouse dragging
	// To see these visuals in-game, make sure the "Gizmos" button in the top right of the Game window is toggled on
	private void Update() {
		if (!_isDragging)
			return;
		Vector3 mpos = GetMouseWorldPosition();
		Vector3 dragVector = mpos - _mouseSelectStart;
		if (SuccessfullySelectedPuck()) {
			Vector3 puckPos = _selectedPuck.transform.position;
			if (IsDragDistBigEnough(dragVector)) {
				Util.D_DrawBox(puckPos, new(LevelManager.Singleton.PuckSize + 0.01f), Color.cyan); // slider
				Util.D_DrawArrowFromTo(_mouseSelectStart, mpos, Color.green); // dragVector
				Util.D_DrawArrowFromTo(puckPos, GetMovementDirection(dragVector) * 2f + puckPos, Color.cyan); // moveDir
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
	}

	private void OnSelectSliderStarted(InputAction.CallbackContext _) {
		_isDragging = true;
		_mouseSelectStart = GetMouseWorldPosition();
		//_selectedPuck = LevelManager.GetPuckAt(_mouseSelectStart);
		//if ()
		//	Debug.LogWarning("nothing");
	}

	private void OnSelectSliderCanceled(InputAction.CallbackContext _) {
		_isDragging = false;
		Vector3 dragVector = GetMouseWorldPosition() - _mouseSelectStart;
		if (!SuccessfullySelectedPuck() || !IsDragDistBigEnough(dragVector)) {
			//_selectedSlider = Entity.Null;
			return;
		}

		// TODO_Onboarding: Make the selected Slider begin sliding.
		// The desired movementDirection is calculated for you.
		// A copy of the SliderComponent is grabbed for you.
		// The selected Slider is stored for you in the variable _selectedSlider
		//EntityManager em = GetEm();
		//SliderComponent sliderComp = em.GetComponentData<SliderComponent>(_selectedSlider);
		//Vector3 movementDirection = GetMovementDirection(dragVector);
		//sliderComp.SliderVelocity = sliderComp.MovementSpeed * movementDirection;
		//GetEm().SetComponentData(_selectedSlider, sliderComp);
		_selectedPuck = null;
	}

	Vector3 GetMouseWorldPosition() {
		Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
		return new Plane(Vector3.back, Vector3.zero).Raycast(ray, out float distance)
			? ray.GetPoint(distance)
			: Vector3.zero;
	}

	bool SuccessfullySelectedPuck() => _selectedPuck != null; // _selectedSlider != Entity.Null;

	bool IsDragDistBigEnough(Vector3 dragVector) => Vector3.SqrMagnitude(dragVector) >= Util.pow2(_dragDeadzone);

	Vector3 GetMovementDirection(Vector3 dragVector) => Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.z) ? new(Mathf.Sign(dragVector.x), 0, 0) : new(0, 0, Mathf.Sign(dragVector.z));

	//EntityManager GetEm() => World.DefaultGameObjectInjectionWorld.EntityManager;

}