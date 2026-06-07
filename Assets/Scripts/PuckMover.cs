using Slenderpi.Utilities;
using System.Collections;
using UnityEngine;

public class PuckMover : MonoBehaviour {

    Coroutine _spawnAnimation;



	private void Awake() {

	}

	public void OnSpawned(int order) {
        if (_spawnAnimation != null)
            StopCoroutine(_spawnAnimation);
        _spawnAnimation = StartCoroutine(SpawnAnimation(order * 0.01f));
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
    
}
