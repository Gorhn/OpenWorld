using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class RandomizedMeshSystem : MeshSystem {

    [SerializeField]
    private List<Mesh> possibleMeshes;

    [SyncVar(hook = nameof(UpdateMeshHook))]
    private int currentMeshId;

    public override void OnStartServer() {
        currentMeshId = Random.Range(0, possibleMeshes.Count);
    }

    private void UpdateMeshHook(int oldMeshId, int newMeshId) {
        if (newMeshId >= possibleMeshes.Count) {
            Debug.LogError("Attempting to update the mesh with id = " + newMeshId + ", max id being " + (possibleMeshes.Count - 1), this);
            return;
        }
        
        GetComponent<MeshFilter>().mesh = possibleMeshes[newMeshId];
        GetComponent<MeshCollider>().sharedMesh = possibleMeshes[newMeshId];
    }

}
