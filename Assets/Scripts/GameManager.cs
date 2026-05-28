using UnityEngine;

public class GameManager : MonoBehaviour {

    public static GameManager Singleton;



	private void Awake() {
		if (!Singleton) {
			Singleton = this;
			DontDestroyOnLoad(gameObject);

		} else if (Singleton != this) {
			Destroy(gameObject);
		}
	}

	private void OnDestroy() {
		if (Singleton == this) {
			Singleton = null;
		}
	}

}
