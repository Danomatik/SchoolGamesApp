using System;
using System.Collections.Generic;

[Serializable]
public class Question
{
    public int id;
    public string text;
    public List<string> options;
    public int correctIndex;

    // Zusätzliche Felder, die wir zur Laufzeit füllen:
    public string category;          // "gruendung", "investition", "ag"
    public string storageKey;        // zusammengesetzter Key für Speicherung
}

[Serializable]
public class LevelBlock
{
    public List<Question> gruendung;
    public List<Question> investition;
    public List<Question> ag;
}

// Variante 1: eine Datei enthält ALLE Kombinationen (junior-de, senior-de, …)
[Serializable]
public class QuestionsRoot
{
    public LevelBlock junior_de;
    public LevelBlock junior_en;
    public LevelBlock senior_de;
    public LevelBlock senior_en;

    // Fallbacks ohne Unterstrich, falls du Dateien selbst „fixed“ speicherst
    public LevelBlock juniorde;
    public LevelBlock junioren;
    public LevelBlock seniorde;
    public LevelBlock senioren;
}
