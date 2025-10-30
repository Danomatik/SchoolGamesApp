using System.Collections.Generic;
using UnityEngine;

public class BoardVisualsManager : MonoBehaviour
{
    [Header("Namensmuster deiner Felder")]
    public string fieldNamePrefix = "Field_"; // also Field_0, Field_1, ...

    [Header("Spielerfarben (Index = PlayerID-1)")]
    public Color[] playerColors = { Color.red, Color.blue, Color.green, Color.yellow };

    private readonly Dictionary<int, GameObject> cache = new();

    void Awake()
    {
        cache.Clear();
        // optional: vorcachen, falls Felder existieren
        for (int i = 0; i < 100; i++) // großzügig
        {
            var go = GameObject.Find(fieldNamePrefix + i);
            if (go) cache[i] = go;
        }
    }

    GameObject GetFieldGO(int fieldIndex)
    {
        if (cache.TryGetValue(fieldIndex, out var go) && go) return go;
        go = GameObject.Find(fieldNamePrefix + fieldIndex);
        if (go) cache[fieldIndex] = go;
        return go;
    }

    Color GetPlayerColor(int playerId)
    {
        if (playerId <= 0) return Color.white;
        int idx = playerId - 1;
        if (playerColors != null && idx >= 0 && idx < playerColors.Length) return playerColors[idx];
        return Color.white;
    }

    public void UpdateFieldVisual(CompanyField field)
    {
        if (field == null) return;
        var go = GetFieldGO(field.fieldIndex);
        if (!go) return;

        bool owned = field.ownerID > 0;
        Color c = owned ? GetPlayerColor(field.ownerID) : Color.white;

        // Outline
        go.GetComponent<FieldOutline3D>()?.SetOwned(c, owned);

        // Upgrade
        go.GetComponent<FieldUpgradeView>()?.SetLevel(field.level);
    }

    public void RefreshAll(IList<CompanyField> fields)
    {
        if (fields == null) return;
        foreach (var f in fields) UpdateFieldVisual(f);
    }
}
