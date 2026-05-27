using System.Collections.Generic;
using UnityEngine;

public class BoardVisualsManager : MonoBehaviour
{
    [Header("Namensmuster deiner Felder")]
    public string fieldNamePrefix = "Field_"; // also Field_0, Field_1, ...

    [Header("Spielerfarben (Index = PlayerID-1)")]
    // Gleiche Palette wie MainMenuController / LeaderboardPanelController (6 Spieler)
    public Color[] playerColors = {
        new Color(0.30f, 0.69f, 0.31f),  // Green
        new Color(0.13f, 0.59f, 0.95f),  // Blue
        new Color(0.98f, 0.74f, 0.02f),  // Yellow/Gold
        new Color(0.90f, 0.30f, 0.24f),  // Red
        new Color(0.61f, 0.15f, 0.69f),  // Purple
        new Color(1.00f, 0.60f, 0.00f),  // Orange
    };

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
