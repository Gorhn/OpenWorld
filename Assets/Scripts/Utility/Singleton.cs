using Mirror;
using UnityEngine;
public class Singleton<T> : NetworkBehaviour where T : NetworkBehaviour {

    private static bool shuttingDown = false;
    private static object locked = new object();
    private static T instance;

    public static T Instance {
        get {
            if (shuttingDown && Application.isPlaying) {
                Debug.LogWarning("[Singleton] Instance '" + typeof(T) + "' already destroyed. Returning null.");
                return null;
            }

            lock (locked) {
                if (instance == null) {
                    instance = (T)FindObjectOfType(typeof(T));
                    if (instance == null) {
                        var singletonObject = new GameObject();
                        instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return instance;
            }
        }
    }

    private void OnApplicationQuit() {
        shuttingDown = true;
    }

    private void OnDestroy() {
        shuttingDown = true;
    }

}