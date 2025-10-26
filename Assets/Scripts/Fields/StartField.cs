using System;
using UnityEngine;

public class StartField : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;

    void OnTriggerEnter(Collider other)
    {
        PlayerCTRL triggeredPiece = other.GetComponentInParent<PlayerCTRL>();
        if (triggeredPiece == null) return;

        gameManager.AddMoney(triggeredPiece.PlayerID, 400); 
    }
}