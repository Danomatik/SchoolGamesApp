using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using DG.Tweening;
using System.Runtime.InteropServices; // make sure DOTween is installed and imported
using System.Linq; // Für .First() benötigt

public class PlayerMovement : MonoBehaviour
{
    private GameManager gameManager;
    private DiceManager diceManager;
    private CameraManager cameraManager;
    private BankCardManager bankCardManager;
    private UIManager uiManager;
    private GameInitiator gameInitiator;

    [SerializeField]
    private GameObject moveButton;
    
    private bool isTurnInProgress = false;



    public void awake()
    {
        uiManager = GetComponent<UIManager>();
        gameManager = GetComponent<GameManager>();
        diceManager = GetComponent<DiceManager>();
        cameraManager = GetComponent<CameraManager>();
        gameInitiator = GetComponent<GameInitiator>();
        bankCardManager = GetComponent<BankCardManager>();
        // questionManager = GetComponent<QuestionManager>();
        // boardVisuals = GetComponent<BoardVisualsManager>();
    }
    

    public void TakeTurn()
    {
        if (isTurnInProgress) return;
        isTurnInProgress = true;

        int diceRoll = diceManager.GetAddedValue();
        Debug.Log($"Player {gameManager.GetCurrentPlayer().PlayerID} rolled a {diceRoll}!");

        PlayerCTRL activePlayer = gameManager.players.Find(p => p.PlayerID == gameManager.GetCurrentPlayer().PlayerID);
        if (activePlayer != null)
        {
            Transform playerChild = activePlayer.transform.childCount > 0
                ? activePlayer.transform.GetChild(0)
                : activePlayer.transform;

            cameraManager.cam.Lens.OrthographicSize = cameraManager.defaultLens; 
            cameraManager.cam.Follow = playerChild;
            if (cameraManager.camBrain.IsBlending && cameraManager.camBrain.ActiveBlend != null)
            {
                moveButton.SetActive(false);
                // moneyDisplay.SetActive(false);
            }
            else
            {
                uiManager.UpdateMoneyDisplay();
                moveButton.SetActive(true);
                // moneyDisplay.SetActive(true);
            }

            activePlayer.StartMove(diceRoll);
        }
    }

    public void PlayerFinishedMoving(int finalPosition)
    {
        // Check field type from board layout
        if (finalPosition < gameInitiator.boardLayout.Length)
        {
            FieldType fieldType = gameInitiator.boardLayout[finalPosition];

            switch (fieldType)
            {
                case FieldType.Start:
                    Debug.Log("Player landed on Start field!");
                    break;

                case FieldType.Company:
                    {
                        Debug.Log($"Player landed on Company field! Field {finalPosition}");
                        // Debug.Log($"BoardLayout[{finalPosition}] = {boardLayout[finalPosition]}");
                        // Debug.Log($"Total companyFields: {companyFields.Count}");
                        var field = gameInitiator.GetCompanyFields().FirstOrDefault(f => f.fieldIndex == finalPosition);
                        if (field == null)
                        {
                            Debug.LogError($"Kein CompanyField für Position {finalPosition} gefunden.");
                            Debug.Log($"Company fields available: {string.Join(", ", gameInitiator.GetCompanyFields().Select(f => f.fieldIndex))}");
                            gameManager.EndTurn();
                            return;
                        }
                        gameManager.HandleCompanyField(field);
                        return; // wichtig: kein EndTurn() hier; Flow entscheidet
                    }


                case FieldType.Bank:
                    {
                        Debug.Log("Player landed on Bank field!");
                        var currentPlayer = gameManager.GetCurrentPlayer();
                        if (currentPlayer != null && bankCardManager != null)
                        {
                            bankCardManager.ShowRandomBankCard();

                            // WICHTIG: Wir warten jetzt auf den Klick im Popup (BankCardManager beendet/fortsetzt den Turn)
                            return;
                        }
                        else
                        {
                            Debug.LogError("Bank field: Current player or BankCardManager is null!");
                        }
                        break;
                    }

            }
        }

        gameManager.EndTurn();
        isTurnInProgress = false;
    }

    public bool setIsTurnInProgress(bool condition)
    {
        return isTurnInProgress = condition;
    }
    
    public GameObject getMoveButton()
    {
        return moveButton;
    }
}
