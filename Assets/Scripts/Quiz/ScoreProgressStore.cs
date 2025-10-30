using UnityEngine;

public static class ScoreProgressStore
{
    // Key: score_highscore_{lang}_{level}
    private static string Key(QuizLang lang, LearnLevel level) => $"score_highscore_{lang}_{level}";

    /// <summary>
    /// Holt den aktuellen Highscore für das gegebene Level und Sprache
    /// </summary>
    public static int GetHighscore(QuizLang lang, LearnLevel level)
    {
        string key = Key(lang, level);
        return PlayerPrefs.GetInt(key, 0);
    }

    /// <summary>
    /// Speichert einen neuen Highscore, wenn dieser höher ist als der aktuelle
    /// </summary>
    public static void SaveHighscore(QuizLang lang, LearnLevel level, int score)
    {
        int currentHighscore = GetHighscore(lang, level);
        if (score > currentHighscore)
        {
            string key = Key(lang, level);
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Prüft, ob der Score ein neuer Highscore ist
    /// </summary>
    public static bool IsNewHighscore(QuizLang lang, LearnLevel level, int score)
    {
        return score > GetHighscore(lang, level);
    }

    /// <summary>
    /// Setzt den Highscore für ein Level zurück
    /// </summary>
    public static void ResetHighscore(QuizLang lang, LearnLevel level)
    {
        PlayerPrefs.DeleteKey(Key(lang, level));
        PlayerPrefs.Save();
    }
}

