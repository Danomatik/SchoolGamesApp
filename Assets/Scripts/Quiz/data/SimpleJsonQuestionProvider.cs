using System.Collections.Generic;
using UnityEngine;

public class SimpleJsonQuestionProvider : MonoBehaviour
{
    public enum FileKey { Junior_DE, Junior_EN, Senior_DE, Senior_EN }

    [Header("Quelle")]
    [Tooltip("Wähle eines der 4 Fragefiles aus Resources/questions/*.json")]
    public FileKey file = FileKey.Junior_DE;

    [Tooltip("Optional: überschreibt den automatisch gebauten Pfad (ohne .json), z.B. 'questions/Schoolgames_Fragen_Junior_DE'")]
    public string customResourcePath = "";

    [Header("Optionen")]
    public bool shuffleQuestions = true;

    public List<Question> LoadQuestions()
    {
        var path = string.IsNullOrEmpty(customResourcePath) ? BuildPath(file) : customResourcePath;
        var ta = Resources.Load<TextAsset>(path);
        if (ta == null)
        {
            Debug.LogError($"[QuestionProvider] Resource '{path}.json' nicht gefunden. Liegt die Datei unter Assets/Resources/?");
            return new List<Question>();
        }

        // 1) Versuch: Top-Level ist ein Array: [ {..}, .. ]
        List<Question> list = TryParseTopLevelArray(ta.text);

        // 2) Versuch: { "questions": [ ... ] }
        if (list == null || list.Count == 0)
            list = TryParseQuestionsObject(ta.text);

        // 3) Versuch: Dein Format mit "junior-de"/"senior-en" und Levels (gruendung/investition/ag)
        if (list == null || list.Count == 0)
            list = TryParsePackFormat(ta.text, file);

        if (list == null) list = new List<Question>();

        // Validierung (2 oder 3 Antwortoptionen zulassen) + optional mischen
        list.RemoveAll(q =>
            q == null || q.options == null || q.options.Count < 2 ||
            q.correctIndex < 0 || q.correctIndex >= q.options.Count);

        if (shuffleQuestions) Shuffle(list);

        return list;
    }

    // ---------- Parser ----------

    private static List<Question> TryParseTopLevelArray(string json)
    {
        try
        {
            var arr = JsonHelper.FromJson<Question>(json);
            if (arr != null && arr.Length > 0) return new List<Question>(arr);
        }
        catch { }
        return null;
    }

    private static List<Question> TryParseQuestionsObject(string json)
    {
        try
        {
            var set = JsonUtility.FromJson<QuestionSet>(json);
            if (set != null && set.questions != null && set.questions.Count > 0)
                return set.questions;
        }
        catch { }
        return null;
    }

    private static List<Question> TryParsePackFormat(string json, FileKey key)
    {
        try
        {
            // JsonUtility kann keine Felder mit Bindestrich parsen → Schlüssel normalisieren
            string norm = NormalizeHyphenKeys(json);

            var root = JsonUtility.FromJson<PackRoot>(norm);
            if (root == null) return null;

            Pack pack = key switch
            {
                FileKey.Junior_DE => root.junior_de,
                FileKey.Junior_EN => root.junior_en,
                FileKey.Senior_DE => root.senior_de,
                FileKey.Senior_EN => root.senior_en,
                _ => null
            };

            if (pack == null) 
            {
                // Fallback: Nimm den ersten nicht-null Pack (falls Datei nur einen enthält)
                pack = root.junior_de ?? root.junior_en ?? root.senior_de ?? root.senior_en;
            }

            if (pack == null) return null;

            var merged = new List<Question>(512);
            if (pack.gruendung != null)   merged.AddRange(pack.gruendung);
            if (pack.investition != null) merged.AddRange(pack.investition);
            if (pack.ag != null)          merged.AddRange(pack.ag);
            return merged;
        }
        catch { }
        return null;
    }

    private static string NormalizeHyphenKeys(string s)
    {
        // Ersetzt nur die Root-Schlüssel mit Bindestrich
        return s
            .Replace("\"junior-de\"", "\"junior_de\"")
            .Replace("\"junior-en\"", "\"junior_en\"")
            .Replace("\"senior-de\"", "\"senior_de\"")
            .Replace("\"senior-en\"", "\"senior_en\"");
    }

    private static string BuildPath(FileKey key)
    {
        // ohne .json, Resources.Load erwartet "questions/DateinameOhneEndung"
        switch (key)
        {
            case FileKey.Junior_DE: return "questions/Schoolgames_Fragen_Junior_DE";
            case FileKey.Junior_EN: return "questions/Schoolgames_Fragen_Junior_EN";
            case FileKey.Senior_DE: return "questions/Schoolgames_Fragen_Senior_DE";
            case FileKey.Senior_EN: return "questions/Schoolgames_Fragen_Senior_EN";
            default: return "questions/Schoolgames_Fragen_Junior_DE";
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        var rng = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    // ---------- Hilfs-Container für JsonUtility ----------

    [System.Serializable] private class QuestionSet { public List<Question> questions; }

    [System.Serializable] private class PackRoot
    {
        public Pack junior_de;
        public Pack junior_en;
        public Pack senior_de;
        public Pack senior_en;
    }

    [System.Serializable]
    private class Pack
    {
        public List<Question> gruendung;
        public List<Question> investition;
        public List<Question> ag;
    }
    
    // ↓ NEU einfügen
public string categoryKey
{
    get
    {
        return file switch
        {
            FileKey.Junior_DE => "junior-de",
            FileKey.Junior_EN => "junior-en",
            FileKey.Senior_DE => "senior-en",
            FileKey.Senior_EN => "senior-en",
            _ => ""
        };
    }
}

}
