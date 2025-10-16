using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerCTRL : MonoBehaviour
{
    public NavMeshAgent agent;
    public Route route;
    public int currentPos = 0;
    public int steps;
    
    [Header("Movement Settings")]
    public float rotationSpeed = 5f;
    public float stoppingDistance = 0.1f;
    
    private bool isMoving = false;
    private bool canRollDice = true;
    
    void Start()
    {
        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
        }
    }
    
    void Update()
    {
        if (isMoving && !agent.pathPending && agent.remainingDistance < stoppingDistance)
        {
            isMoving = false;
            canRollDice = true;
            Debug.Log("Player stopped at position: " + currentPos);
        }
        
        if (Input.GetKeyDown(KeyCode.Space) && canRollDice && !isMoving)
        {
            RollDiceAndMove();
        }
    }
    
    void RollDiceAndMove()
    {
        steps = Random.Range(1, 7);
        Debug.Log("Dice Rolled: " + steps);

        int targetPos = (currentPos + steps) % route.childNodeList.Count;
        
        StartCoroutine(MoveStepByStep(targetPos));
    }
    
    IEnumerator MoveStepByStep(int finalPos)
    {
        canRollDice = false;
        isMoving = true;
        
        int stepsToTake = steps;
        
        while (stepsToTake > 0)
        {
            currentPos = (currentPos + 1) % route.childNodeList.Count;
            agent.SetDestination(route.childNodeList[currentPos].position);
 
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance < stoppingDistance);
            
            stepsToTake--;
            
            yield return new WaitForSeconds(0.2f);
        }
        
        isMoving = false;
        canRollDice = true;
        Debug.Log("Player reached final position: " + currentPos);
    }
    
    public bool CanRollDice()
    {
        return canRollDice && !isMoving;
    }
    
    public bool IsMoving()
    {
        return isMoving;
    }
}