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

        gameManager.moneyManager.AddMoney(triggeredPiece.PlayerID, 400); 
        
        Debug.Log($"Spieler {triggeredPiece.PlayerID} überquert das Startfeld und erhält Geld.");
    }
}