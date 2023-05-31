using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JsonHelper {

    public static T[] getJsonArray<T>(string json) where T : new() {
        string jsonToProcess = json.Substring(1, json.Length - 2).Replace("},{", "}&{");
        List<string> jsonObjects = jsonToProcess.Split('&').ToList();
        List<T> objects = new List<T>();
        jsonObjects.ForEach(j => {
            T obj = new T();
            obj = JsonUtility.FromJson<T>(j);
            objects.Add(obj);
        });
        return objects.ToArray();
    }

}