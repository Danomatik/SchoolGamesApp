using System;

public static class JsonHelper
{
    /// <summary>
    /// Ersetzt problematische Keys mit Bindestrich in Varianten,
    /// die JsonUtility verarbeiten kann (Unterstrich).
    /// </summary>
    public static string FixMinusKeysForJsonUtility(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        return json
            .Replace("\"junior-de\"", "\"junior_de\"")
            .Replace("\"junior-en\"", "\"junior_en\"")
            .Replace("\"senior-de\"", "\"senior_de\"")
            .Replace("\"senior-en\"", "\"senior_en\"");
    }

    /// <summary>
    /// Hilfsfunktion: sichere Null-Listen.
    /// </summary>
    public static System.Collections.Generic.List<T> OrEmpty<T>(this System.Collections.Generic.List<T> list)
        => list ?? new System.Collections.Generic.List<T>();
}
