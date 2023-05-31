using Mirror;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StructureSystem))]
[RequireComponent(typeof(MeshSystem))]
public class ResourceNodeEntity : RealmEntity {

    private void Start() {
        GetComponent<StructureSystem>().StructureDestroyedEvent += () => GetComponent<ShaderSystem>().SwitchShader("");
        GetComponent<StructureSystem>().StructureRepairedEvent += () => GetComponent<ShaderSystem>().SwitchShader("");
    }

}
