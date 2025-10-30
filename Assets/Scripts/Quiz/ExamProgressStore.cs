using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ExamProgressStore
{
    private const int MaxStoredResults = 10; // Maximale Anzahl gespeicherter Ergebnisse pro Level

    // Key: exam_results_{lang}_{level}
    private static string Key(QuizLang lang, LearnLevel level) => $"exam_results_{lang}_{level}";

    /// <summary>
    /// Speichert ein neues Prüfungsergebnis
    /// </summary>
    public static void SaveResult(ExamResult result)
    {
        var results = GetAllResults(result.language, result.level);
        results.Add(result);

        // Nur die letzten MaxStoredResults behalten
        if (results.Count > MaxStoredResults)
        {
            results = results.OrderByDescending(r => r.timestamp).Take(MaxStoredResults).ToList();
        }

        // Als JSON speichern
        string json = JsonUtility.ToJson(new ExamResultList { results = results }, true);
        PlayerPrefs.SetString(Key(result.language, result.level), json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Holt alle gespeicherten Ergebnisse für ein Level
    /// </summary>
    public static List<ExamResult> GetAllResults(QuizLang lang, LearnLevel level)
    {
        string key = Key(lang, level);
        string json = PlayerPrefs.GetString(key, "");

        if (string.IsNullOrEmpty(json))
            return new List<ExamResult>();

        try
        {
            var resultList = JsonUtility.FromJson<ExamResultList>(json);
            return resultList?.results ?? new List<ExamResult>();
        }
        catch
        {
            return new List<ExamResult>();
        }
    }

    /// <summary>
    /// Holt das beste Ergebnis (höchster Prozentsatz)
    /// </summary>
    public static ExamResult GetBestResult(QuizLang lang, LearnLevel level)
    {
        var results = GetAllResults(lang, level);
        return results.OrderByDescending(r => r.percentageScore).FirstOrDefault();
    }

    /// <summary>
    /// Holt das neueste Ergebnis
    /// </summary>
    public static ExamResult GetLatestResult(QuizLang lang, LearnLevel level)
    {
        var results = GetAllResults(lang, level);
        return results.OrderByDescending(r => r.timestamp).FirstOrDefault();
    }

    /// <summary>
    /// Löscht alle Ergebnisse für ein Level
    /// </summary>
    public static void ClearResults(QuizLang lang, LearnLevel level)
    {
        PlayerPrefs.DeleteKey(Key(lang, level));
        PlayerPrefs.Save();
    }

    // Helper-Klasse für JSON-Serialisierung (JsonUtility unterstützt keine Listen direkt)
    [System.Serializable]
    private class ExamResultList
    {
        public List<ExamResult> results = new List<ExamResult>();
    }
}

