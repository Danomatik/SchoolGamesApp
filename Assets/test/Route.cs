using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Route : MonoBehaviour
{
    Transform[] childObjects;
    public List<Transform> childNodeList = new List<Transform>();

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        FillNodes();

        for (int i = 0; i < childNodeList.Count; i++)
        {
            Vector3 currentPos = childNodeList[i].position;
            if (i > 0)
            {
                Vector3 prevPos = childNodeList[i - 1].position;
                Gizmos.DrawLine(prevPos, currentPos);
            }
        }
    }

    void FillNodes()
    {
        childObjects = GetComponentsInChildren<Transform>();
        childNodeList.Clear();

        foreach (Transform child in childObjects)
        {
            if (child != this.transform)
            {
                childNodeList.Add(child);
            }
        }

       
    }
}
