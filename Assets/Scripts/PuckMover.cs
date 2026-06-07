using Slenderpi.Utilities;
using System.Collections;
using UnityEngine;

public class PuckMover : MonoBehaviour {

    enum EAnimState {
        None,
        Spawned,
        HoverBegin,
        HoverEnd,
        SelectBegin,
        SelectEnd
    }

    Coroutine _currentAnimation;
    EAnimState _currState = EAnimState.None;



	private void Awake() {

	}

    /// <summary>
    /// Hover means when the mouse is hovered over this Puck.
    /// </summary>
    public void OnHoverBegin() {
        StopCurrentAnimation();
		_currentAnimation = StartCoroutine(HoverOrSelectAnimation(transform.localScale.x, 1.15f));
        _currState = EAnimState.HoverBegin;
	}

	/// <summary>
	/// Hover means when the mouse is hovered over this Puck.
	/// </summary>
	public void OnHoverEnd() {
        StopCurrentAnimation();
		_currentAnimation = StartCoroutine(HoverOrSelectAnimation(transform.localScale.x, 1));
        _currState = EAnimState.HoverEnd;
	}

    public void OnSelectBegin() {
		StopCurrentAnimation();
		_currentAnimation = StartCoroutine(HoverOrSelectAnimation(transform.localScale.x, 0.9f));
		_currState = EAnimState.SelectBegin;
	}

    public void OnSelectEnd() {
		StopCurrentAnimation();
		_currentAnimation = StartCoroutine(HoverOrSelectAnimation(transform.localScale.x, 1));
		_currState = EAnimState.SelectEnd;
	}

	public void OnSpawned(int order) {
        StopCurrentAnimation();
        _currentAnimation = StartCoroutine(SpawnAnimation(order * 0.01f));
		_currState = EAnimState.Spawned;
	}

    void StopCurrentAnimation() {
		if (_currentAnimation != null)
			StopCoroutine(_currentAnimation);
        if (_currState == EAnimState.Spawned) {
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
	}

	IEnumerator SpawnAnimation(float startDelay) {
        float duration = 0.3333f;
        float c = 3; // 1.70158f;
		float startScale = 0f;
        float endScale = 1f;
        Quaternion startQuat = Quaternion.Euler(0, 0, 30f);
        Quaternion endQuat = Quaternion.identity;

        transform.rotation = startQuat;
        transform.localScale = new Vector3(startScale, startScale, startScale);
        yield return new WaitForSeconds(startDelay);

        float t = 0;
        while (t < 1) {
            float lerp = Util.pow2(t - 1) * ((c + 1) * (t - 1) + c) + 1;
			transform.rotation = Quaternion.SlerpUnclamped(startQuat, endQuat, lerp);
            float lerpedScale = Mathf.LerpUnclamped(startScale, endScale, lerp);
            transform.localScale = new(lerpedScale, lerpedScale, lerpedScale);
            yield return new WaitForEndOfFrame();
            t += Time.deltaTime / duration;
        }
        transform.rotation = endQuat;
        transform.localScale = new Vector3(endScale, endScale, endScale);
    }

    IEnumerator HoverOrSelectAnimation(float startScale, float endScale) {
        float duration = 0.2f;
		float c = 5; // 1.70158f;

		float t = 0;
        while (t < 1) {
			float lerp = Util.pow2(t - 1) * ((c + 1) * (t - 1) + c) + 1;
			float lerpedScale = Mathf.LerpUnclamped(startScale, endScale, lerp);
			transform.localScale = new(lerpedScale, lerpedScale, lerpedScale);
			yield return new WaitForEndOfFrame();
            t += Time.deltaTime / duration;
        }
        transform.localScale = new Vector3(endScale, endScale, endScale);
    }

}
