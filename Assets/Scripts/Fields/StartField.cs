using System;
using UnityEngine;

public class StartField : MonoBehaviour
{
    [SerializeField]
    private GameManager GameManager;
    void OnTriggerEnter (Collider other)
    {
        GameManager.AddMoney(500);
        Debug.Log ("Player has Entered Start Field!");
    }
}
