using System.Collections.Generic;
using UnityEngine;

public static class LearnProgressStore
{
    // Key: learn_solved_{lang}_{level}
    private static string Key(QuizLang lang, LearnLevel level) => $"learn_solved_{lang}_{level}";

    private static HashSet<string> LoadSet(QuizLang lang, LearnLevel level)
    {
        string key = Key(lang, level);
        string raw = PlayerPrefs.GetString(key, "");
        var set = new HashSet<string>();
        if (!string.IsNullOrEmpty(raw))
        {
            var parts = raw.Split('|');
            foreach (var p in parts)
            {
                if (!string.IsNullOrEmpty(p)) set.Add(p);
            }
        }
        return set;
    }

    private static void SaveSet(QuizLang lang, LearnLevel level, HashSet<string> set)
    {
        string key = Key(lang, level);
        string raw = string.Join("|", set);
        PlayerPrefs.SetString(key, raw);
        PlayerPrefs.Save();
    }

    public static bool IsSolved(QuizLang lang, LearnLevel level, string storageKey)
    {
        var set = LoadSet(lang, level);
        return set.Contains(storageKey);
    }

    public static void MarkSolved(QuizLang lang, LearnLevel level, string storageKey)
    {
        var set = LoadSet(lang, level);
        if (set.Add(storageKey))
        {
            SaveSet(lang, level, set);
        }
    }

    public static void Reset(QuizLang lang, LearnLevel level)
    {
        PlayerPrefs.DeleteKey(Key(lang, level));
        PlayerPrefs.Save();
    }

    public static int CountSolved(QuizLang lang, LearnLevel level, IEnumerable<Question> pool)
    {
        var set = LoadSet(lang, level);
        int count = 0;
        foreach (var q in pool)
        {
            if (q != null && !string.IsNullOrEmpty(q.storageKey) && set.Contains(q.storageKey))
                count++;
        }
        return count;
    }
}
