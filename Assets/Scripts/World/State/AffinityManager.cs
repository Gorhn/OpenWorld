using GameData;
using Mirror;
using System.Collections;
using UnityEngine;

public class AffinityManager : Singleton<AffinityManager> {

    [SerializeField]
    [SyncVar]
    private WorldState state;

    [SerializeField]
    [SyncVar(hook = nameof(ChangeAffinityHook))]
    private Affinity affinity;

    public delegate void AffinityChanged(Affinity oldValue, Affinity newValue);
    public static event AffinityChanged AffinityChangedEvent;
    private Coroutine ChangeAffinityCoroutine;

    public override void OnStartServer() {
        state = WorldState.DEFAULT;
        affinity = GetRandomAffinity();
        ChangeAffinityCoroutine = StartCoroutine(ChangeAffinity());
    }

    [Server]
    private IEnumerator ChangeAffinity() {
        if (state == WorldState.DEFAULT) {
            affinity = GetRandomAffinity();
        }
        yield return new WaitForSecondsRealtime(10);
    }

    private void ChangeAffinityHook(Affinity oldValue, Affinity newValue) {
        AffinityChangedEvent(oldValue, newValue);
    }

    private Affinity GetRandomAffinity() {
        return (Affinity) typeof(Affinity).GetEnumValues().GetValue(Random.Range(0, typeof(Affinity).GetEnumValues().Length));
    }

}
