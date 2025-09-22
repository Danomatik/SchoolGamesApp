using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCTRL : MonoBehaviour
{
    public Route currentRoute;
    int routePosition;

    public int steps;

    bool isMoving;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isMoving)
        {
            steps = Random.Range(1, 7);
            Debug.Log("Dice Rolled: " + steps);

            StartCoroutine(Move());
        }
    }

    IEnumerator Move()
    {
        if (isMoving)
        {
            yield break;
        }
        isMoving = true;

        while (steps > 0)
        {
            routePosition++;
            routePosition %= currentRoute.childNodeList.Count;

            Vector3 nextPos = currentRoute.childNodeList[routePosition].position;
            while (MoveToNextNode(nextPos))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
            steps--;
        }

        isMoving = false;
    }

    bool MoveToNextNode(Vector3 goal)
    {
        return goal != (transform.position = Vector3.MoveTowards(transform.position, goal, 2f * Time.deltaTime));
    }
}
