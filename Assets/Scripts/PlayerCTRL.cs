using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.AI;

public class PlayerCTRL : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;  
    public int PlayerID;
    public NavMeshAgent agent;
    public Route route;
    public int currentPos = 0;

    [Header("Movement Settings")]
    public float stoppingDistance = 0.1f;
    
    // This is now the ONLY way to move the player from the outside
    public void StartMove(int stepsToTake) // Parameter name changed for clarity
    {
        StartCoroutine(MoveStepByStep(stepsToTake));
    }

    private IEnumerator MoveStepByStep(int stepsToTake)
    {
        while (stepsToTake > 0)
        {
            currentPos = (currentPos + 1) % route.childNodeList.Count;
            agent.SetDestination(route.childNodeList[currentPos].position);
 
            // Wait until the agent reaches the next node
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance < stoppingDistance);
            
            stepsToTake--;
            
            // Optional short pause between steps for better visual feedback
            yield return new WaitForSeconds(0.2f); 
        }

        Debug.Log($"Player {PlayerID} finished moving and is now at position {currentPos}.");

        // BUG FIX #1: Tell the GameManager the move is finished!
        gameManager.PlayerFinishedMoving(currentPos);
    }
}