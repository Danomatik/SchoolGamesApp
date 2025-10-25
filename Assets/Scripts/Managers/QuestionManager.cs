using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Collections;

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
    public Button[] optionButtons; // NEU: Array für die Antwort-Buttons
    public TextMeshProUGUI[] optionButtonTexts; // NEU: Array für die Texte AUF den Buttons

    private int currentCorrectIndex = -1; // NEU: Merkt sich den korrekten Index

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

    // In QuestionManager.cs
    public void ShowQuestionInUI()
    {
        QuestionData question = GetRandomQuestion();
        if (question == null) return;

        currentCorrectIndex = question.correctIndex; // Korrekten Index speichern

        if (quizPanel != null) quizPanel.SetActive(true);
        if (questionText != null) questionText.text = question.text;
        if (questionID != null) questionID.text = question.id.ToString();

        if (optionButtons != null && optionButtonTexts != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < question.options.Length)
                {
                    optionButtonTexts[i].text = question.options[i]; // Text auf dem Button setzen
                    optionButtons[i].gameObject.SetActive(true);

                    // --- Button-Logik hinzufügen ---
                    int buttonIndex = i; // Wichtig: Index in lokaler Variable speichern für den Listener
                    optionButtons[i].onClick.RemoveAllListeners(); // Alte Listener entfernen (wichtig!)
                    optionButtons[i].onClick.AddListener(() => HandleAnswer(buttonIndex)); // Neuen Listener hinzufügen

                    // Button-Farbe zurücksetzen (falls vorher falsch/richtig)
                    optionButtons[i].GetComponent<Image>().color = Color.white; // Oder Ihre Standardfarbe
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    // In QuestionManager.cs
    public void HandleAnswer(int selectedIndex)
    {
        Debug.Log($"Antwort {selectedIndex + 1} ausgewählt.");

        // Alle Buttons vorübergehend deaktivieren, um Mehrfachklicks zu verhindern
        foreach (Button btn in optionButtons)
        {
            btn.interactable = false;
        }

        // Prüfen, ob die Antwort korrekt ist
        if (selectedIndex == currentCorrectIndex)
        {
            Debug.Log("RICHTIG!");
            // Visuelles Feedback für richtige Antwort (z.B. Button grün färben)
            if (optionButtons[selectedIndex] != null)
                optionButtons[selectedIndex].GetComponent<Image>().color = Color.green;

            // Hier Logik einfügen, was bei richtiger Antwort passieren soll
            // z.B. GameManager.Instance.AwardBonus(); 
        }
        else
        {
            Debug.Log("FALSCH!");
            // Visuelles Feedback für falsche Antwort (z.B. geklickten Button rot färben)
            if (optionButtons[selectedIndex] != null)
                optionButtons[selectedIndex].GetComponent<Image>().color = Color.red;

            // Optional: Den richtigen Button grün hervorheben
            if (currentCorrectIndex >= 0 && currentCorrectIndex < optionButtons.Length && optionButtons[currentCorrectIndex] != null)
                optionButtons[currentCorrectIndex].GetComponent<Image>().color = Color.green;

            // Hier Logik einfügen, was bei falscher Antwort passieren soll
            // z.B. GameManager.Instance.ApplyPenalty();
        }

        // Nach kurzer Pause das Panel schließen und Buttons reaktivieren
        StartCoroutine(ClosePanelAfterDelay(2.0f)); // Schließt nach 2 Sekunden
    }
    
    // In QuestionManager.cs (braucht oben: using System.Collections;)
    private IEnumerator ClosePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wartezeit

        if (quizPanel != null) quizPanel.SetActive(false); // Panel ausblenden

        // Buttons wieder aktivieren für die nächste Frage
        foreach (Button btn in optionButtons)
        {
            btn.interactable = true; 
        }

        // Optional: Hier den GameManager informieren, dass die Quiz-Interaktion beendet ist,
        // damit der Zug fortgesetzt/beendet werden kann.
        // GameManager.Instance.QuizCompleted(); 
    }
}
