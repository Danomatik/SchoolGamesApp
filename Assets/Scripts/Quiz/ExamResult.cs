using System;

[Serializable]
public class ExamResult
{
    public string timestamp;           // Wann die Prüfung gemacht wurde
    public QuizLang language;
    public LearnLevel level;
    public int totalQuestions;
    public int correctAnswers;
    public float percentageScore;      // 0-100
    public float timeUsedSeconds;      // Wie lange gebraucht
    public bool passed;                // z.B. >= 60%

    public ExamResult()
    {
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public int GetWrongAnswers()
    {
        return totalQuestions - correctAnswers;
    }

    public string GetFormattedTime()
    {
        int minutes = (int)(timeUsedSeconds / 60);
        int seconds = (int)(timeUsedSeconds % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }
}

