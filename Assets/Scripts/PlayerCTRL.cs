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
    
    public void StartMove(int stepsToTake) 
    {
        StartCoroutine(MoveStepByStep(stepsToTake));
    }

    private IEnumerator MoveStepByStep(int stepsToTake)
    {
        float stepTimeout = 1.4f;

        while (stepsToTake > 0)
        {
            currentPos = (currentPos + 1) % route.childNodeList.Count;
            agent.SetDestination(route.childNodeList[currentPos].position);

            float timer = 0.0f;
            // Warte, bis das Ziel erreicht ist ODER der Timer abläuft, weil er manchmal stuck bleibt
            while (!(!agent.pathPending && agent.remainingDistance < stoppingDistance))
            {
                timer += Time.deltaTime;
                if (timer > stepTimeout)
                {
                    Debug.LogWarning($"Spieler {PlayerID} HÄNGT! Breche Bewegung ab.");
                    agent.Warp(route.childNodeList[currentPos].position); 
                    break; 
                }
                yield return null; 
            }
 
            stepsToTake--;
            
            yield return new WaitForSeconds(0.0f); 
        }

        Debug.Log($"Player {PlayerID} finished moving and is now at position {currentPos}.");
        gameManager.PlayerFinishedMoving(currentPos);
    }
}