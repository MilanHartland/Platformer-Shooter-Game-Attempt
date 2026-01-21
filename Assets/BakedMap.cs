using System.Collections.Generic;
using MilanUtils;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class BakedMap : ScriptableObject
{
    public Dictionary<Vector3, List<Vector3>> map => ToDict();
    public List<GraphPoint> serializableMap;

    [System.Serializable]
    public class GraphPoint
    {
        public Vector3 point;
        public List<Vector3> list;
    }

    public Dictionary<Vector3, List<Vector3>> ToDict()
    {
        Dictionary<Vector3, List<Vector3>> newMap = new();

        foreach(var pair in serializableMap)
        {
            newMap.Add(pair.point, pair.list);
        }

        return newMap;
    }

    public void SetFromDict(Dictionary<Vector3, List<Vector3>> dict)
    {
        serializableMap = new();
        foreach(var pair in dict)
        {
            serializableMap.Add(new(){point = pair.Key, list = pair.Value});
        }
    }
}
