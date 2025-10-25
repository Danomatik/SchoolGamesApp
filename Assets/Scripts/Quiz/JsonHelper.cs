using System;
using UnityEngine;

public static class JsonHelper
{
    // Erwartet: JSON ist ein Array auf Root-Ebene: [ {...}, {...} ]
    public static T[] FromJson<T>(string json)
    {
        string newJson = $"{{\"items\":{json}}}";
        var wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.items ?? Array.Empty<T>();
    }

    [Serializable]
    private class Wrapper<T> { public T[] items; }
}
