using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class SimpleJsonQuestionProvider : MonoBehaviour
{
    [Header("Resources-Pfad (unter Assets/Resources)")]
    public string resourcesFolder = "questions";

    [Tooltip("Dateiname OHNE .json. Platzhalter: {LEVEL}=Junior/Senior, {LANG}=DE/EN")]
    public string filePattern = "Schoolgames_Fragen_{LEVEL}_{LANG}";

    public List<Question> LoadQuestionsFlat(QuizLang lang, LearnLevel level)
    {
        // Dateiname bauen (z.B. questions/Schoolgames_Fragen_Junior_DE)
        string lvl = level == LearnLevel.Junior ? "Junior" : "Senior";
        string lng = lang == QuizLang.DE ? "DE" : "EN";
        string name = (filePattern ?? string.Empty).Replace("{LEVEL}", lvl).Replace("{LANG}", lng);
        string path = string.IsNullOrEmpty(resourcesFolder) ? name : $"{resourcesFolder.TrimEnd('/')}/{name}";

        var ta = Resources.Load<TextAsset>(path);
        if (ta == null)
        {
            Debug.LogError($"[Provider] Keine Datei unter 'Assets/Resources/{path}.json' gefunden.");
            return new List<Question>();
        }

        if (string.IsNullOrWhiteSpace(ta.text))
        {
            Debug.LogError($"[Provider] Datei leer: {path}.json");
            return new List<Question>();
        }

        try
        {
            // 1) Top-Level-Keys mit Bindestrich zu Unterstrich konvertieren, damit JsonUtility sie mappen kann
            //    z.B.  "junior-de" -> "junior_de"
            string fixedJson = FixFirstLevelMinusKeys(ta.text);

            // 2) In Wrapper parsen
            var root = JsonUtility.FromJson<QuestionsRootBlock>(fixedJson);
            if (root == null)
            {
                Debug.LogError("[Provider] JSON konnte nicht gelesen werden (root == null).");
                return new List<Question>();
            }

            // 3) Passenden Block wählen
            LevelBlock block = null;
            if (lang == QuizLang.DE && level == LearnLevel.Junior)  block = root.junior_de;
            if (lang == QuizLang.EN && level == LearnLevel.Junior)  block = root.junior_en;
            if (lang == QuizLang.DE && level == LearnLevel.Senior)  block = root.senior_de;
            if (lang == QuizLang.EN && level == LearnLevel.Senior)  block = root.senior_en;

            if (block == null)
            {
                Debug.LogError($"[Provider] Im JSON fehlt der Block für {level}-{lang}.");
                return new List<Question>();
            }

            // 4) Kategorien flatten
            var list = new List<Question>();
            void AddCat(string cat, List<Question> src)
            {
                if (src == null) return;
                foreach (var q in src)
                {
                    if (q == null) continue;
                    q.category   = cat;
                    if (string.IsNullOrEmpty(q.storageKey))
                        q.storageKey = $"{lang}_{level}_{cat}_{q.id}";
                    list.Add(q);
                }
            }

            AddCat("gruendung",   block.gruendung);
            AddCat("investition", block.investition);
            AddCat("ag",          block.ag);

            if (list.Count == 0)
                Debug.LogError($"[Provider] 0 Fragen gefunden in '{ta.name}' für {level}-{lang}. Prüfe Kategorienamen und Inhalt.");

            return list;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Provider] Parsefehler: {ex.Message}");
            return new List<Question>();
        }
    }

    // ---- Modelle exakt zu deinem JSON ----
    [Serializable] public class LevelBlock
    {
        public List<Question> gruendung;
        public List<Question> investition;
        public List<Question> ag;
    }

    [Serializable] public class QuestionsRootBlock
    {
        public LevelBlock junior_de;
        public LevelBlock junior_en;
        public LevelBlock senior_de;
        public LevelBlock senior_en;
    }

    // Ersetzt NUR die 1. Ebene der Keys "junior-de"/"senior-de"/"junior-en"/"senior-en" -> mit Unterstrich
    static string FixFirstLevelMinusKeys(string json)
    {
        // Wir ersetzen nur, wenn der Key direkt hinter einer { oder einem , beginnt (Top-Level-Property)
        // Muster:  {"junior-de": ...}  oder  ,"senior-de": ...
        var pattern = "(?<=(\\{|,))\\s*\"(junior|senior)-(de|en)\"\\s*:";
        return Regex.Replace(json, pattern, m =>
        {
            var full = m.Value; // inklusive evtl. Whitespaces und :
            // extrahierte Gruppen via erneutem Regex?
            // einfacher: wir wissen, dass m.Value z.B.  "junior-de":
            // also ersetzen nur den Bindestrich-Teil
            return full.Replace("junior-de", "junior_de")
                       .Replace("junior-en", "junior_en")
                       .Replace("senior-de", "senior_de")
                       .Replace("senior-en", "senior_en");
        }, RegexOptions.IgnoreCase);
    }
}
