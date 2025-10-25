using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestionManager : MonoBehaviour
{
    [System.Serializable]
    public class QuestionCategory
    {
        public List<QuestionData> gruendung;
        public List<QuestionData> investition;
        public List<QuestionData> ag;
    }

    [System.Serializable]
    public class QuestionDatabase
    {
        public QuestionCategory junior_de;
    }

    private QuestionDatabase questionDatabase;
    private List<QuestionData> allQuestions = new List<QuestionData>();
    private QuizField[] quizFields;

    [Header("UI Elements")] // NEUER ABSCHNITT
    public GameObject quizPanel; // Ihr UI-Panel
    public TextMeshProUGUI questionText; // Das TMP-Feld für die Frage
    public TextMeshProUGUI questionID; // Array von TMP-Feldern für die Antworten (z.B. 4 Stück)
    // Optional: Buttons für die Antworten
    public TextMeshProUGUI[] optionTexts; // Array von TMP-Feldern für die Antworten (z.B. 4 Stück)
    // Optional: Buttons für die Antworten

    void Start()
    {
        LoadQuestions();
        FindQuizFields();
    }

    private void FindQuizFields()
    {
        quizFields = FindObjectsByType<QuizField>(FindObjectsSortMode.None);
    }

    private void LoadQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Schoolgames_Fragen_Junior_DE");
        
        if (jsonFile == null)
        {
            Debug.LogError("Could not load Schoolgames_Fragen_Junior_DE.json from Resources/Data/");
            return;
        }

        try
        {
            questionDatabase = JsonUtility.FromJson<QuestionDatabase>(jsonFile.text);
            
            if (questionDatabase == null)
            {
                Debug.LogError("QuestionManager: questionDatabase is null after parsing!");
                return;
            }
            
            if (questionDatabase.junior_de == null)
            {
                Debug.LogError("QuestionManager: junior_de is null!");
                return;
            }
            
            CompileAllQuestions();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
        }
    }

    private void CompileAllQuestions()
    {
        allQuestions.Clear();

        if (questionDatabase?.junior_de != null)
        {
            if (questionDatabase.junior_de.gruendung != null)
            {
                allQuestions.AddRange(questionDatabase.junior_de.gruendung);
            }
            
            if (questionDatabase.junior_de.investition != null)
            {
                allQuestions.AddRange(questionDatabase.junior_de.investition);
            }
            
            if (questionDatabase.junior_de.ag != null)
            {
                allQuestions.AddRange(questionDatabase.junior_de.ag);
            }
        }
    }

    public QuestionData GetRandomQuestion()
    {
        if (allQuestions.Count == 0)
        {
            Debug.LogWarning("No questions available!");
            return null;
        }

        int randomIndex = Random.Range(0, allQuestions.Count);
        return allQuestions[randomIndex];
    }

    public void PrintRandomQuestion()
    {
        QuestionData question = GetRandomQuestion();
        if (question != null)
        {
            string optionsText = "";
            for (int i = 0; i < question.options.Length; i++)
            {
                string marker = (i == question.correctIndex) ? "✓" : " ";
                optionsText += $"\n  {marker} {i + 1}. {question.options[i]}";
            }

            Debug.Log($"Question #{question.id}: {question.text}{optionsText}\nCorrect Answer: {question.correctIndex + 1}");

        }
    }

    // Called by GameManager when player lands on a field
    public void CheckForQuizField(int fieldPosition)
    {
        if (quizFields == null)
        {
            FindQuizFields();
        }

        if (quizFields == null || quizFields.Length == 0)
        {
            return;
        }

        foreach (QuizField quizField in quizFields)
        {
            if (quizField.fieldIndex == fieldPosition)
            {
                quizField.TriggerQuestion();
                return;
            }
        }
    }
    
    // In QuestionManager.cs

public void ShowQuestionInUI()
{
    QuestionData question = GetRandomQuestion(); // Holt eine zufällige Frage
    if (question == null) return; // Wenn keine Frage da ist, hör auf

    // 1. Panel aktivieren
    if (quizPanel != null) quizPanel.SetActive(true);

        // 2. Fragetext setzen
        if (questionText != null) questionText.text = question.text;

        if (questionID != null) questionID.text = question.id.ToString();

    // 3. Antwortoptionen setzen
    if (optionTexts != null)
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (i < question.options.Length) // Nur so viele Optionen anzeigen, wie die Frage hat
            {
                optionTexts[i].text = $"{i + 1}. {question.options[i]}";
                optionTexts[i].gameObject.SetActive(true); // Sicherstellen, dass das Textfeld sichtbar ist
            }
            else
            {
                optionTexts[i].gameObject.SetActive(false); // Übrige Textfelder ausblenden
            }
        }
    }

    // Hier könnten Sie noch die Logik für Antwort-Buttons hinzufügen
}
}
