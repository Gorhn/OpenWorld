using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ShaderSystem : MonoBehaviour {

    public void SwitchShader(string shaderName) {
        Shader shader = Shader.Find(shaderName);

        if (shader.name == "Hidden/InternalErrorShader") {
            Debug.LogError("Attempting to update the shader with name = " + shaderName + ", but it wasn't found.", this);
            return;
        }

        GetComponent<MeshRenderer>().material.shader = shader;
    }

}
