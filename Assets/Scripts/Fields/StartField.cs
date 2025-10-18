using System;
using UnityEngine;

public class StartField : MonoBehaviour
{
    [SerializeField]
    private GameManager GameManager;
    void OnTriggerEnter (Collider other)
    {
        PlayerCTRL player = other.GetComponent<PlayerCTRL>();

        if(player != null && player.PlayerID == GameManager.GetCurrentPlayer().PlayerID)
        {
            GameManager.AddMoney(500);
            Debug.Log($"Aktiver Spieler (ID: {player.PlayerID}) ist auf dem Startfeld gelandet!");

        }

    }
}
