using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapManager))]
public class MapManagerEditor : Editor {

    public override void OnInspectorGUI() {
        MapManager mapManager = (MapManager) target;

        DrawDefaultInspector();

        if (GUILayout.Button("Update Voronoi")) {
            mapManager.InstantiateRegions();
        }
    }

}
