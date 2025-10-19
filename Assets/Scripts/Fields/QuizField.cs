using UnityEngine;

public class QuizField : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;
    
    [SerializeField]
    public QuestionManager questionManager;
    
    public int fieldIndex; // Set this in the inspector to match the board position

    // This method is called by GameManager when player lands on this field
    public void TriggerQuestion()
    {
        Debug.Log($"Player landed on Quiz Field {fieldIndex}!");
        
        if (questionManager != null)
        {
            questionManager.PrintRandomQuestion();
        }
        else
        {
            Debug.LogWarning("QuestionManager not assigned to QuizField!");
        }
    }
}
