using System.Collections.Generic;
using UnityEngine;

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
}
