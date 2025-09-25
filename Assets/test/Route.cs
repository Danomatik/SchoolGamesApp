using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Route : MonoBehaviour
{
    Transform[] childObjects;
    public List<Transform> childNodeList = new List<Transform>();

    void Start()
    {
        FillNodes();
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
