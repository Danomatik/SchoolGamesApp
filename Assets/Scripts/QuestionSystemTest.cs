using UnityEngine;

public class QuestionSystemTest : MonoBehaviour
{
    [SerializeField]
    private QuestionManager questionManager;

    void Update()
    {
        // Press Q to test random question loading
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (questionManager != null)
            {
                Debug.Log("=== TESTING QUESTION SYSTEM ===");
                questionManager.PrintRandomQuestion();
            }
            else
            {
                Debug.LogWarning("QuestionManager not assigned to QuestionSystemTest!");
            }
        }
    }
}
