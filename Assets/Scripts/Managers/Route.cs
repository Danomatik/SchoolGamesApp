using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Route : MonoBehaviour
{
    Transform[] childObjects;
    public List<Transform> childNodeList = new List<Transform>();
    
    // ✅ Empty method to prevent build errors with _g_CodegenRegistration
    void Start()
    {
        // Initialize if needed
    }
}
