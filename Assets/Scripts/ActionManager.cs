using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ActionManager : MonoBehaviour
{
    private GameManager gameManager;

    [HideInInspector] public bool lastCardWasRollAgain = false;

    public void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }


    public bool ShouldRollAgain()
    {
        return lastCardWasRollAgain;
    }

    public void MovePlayer(int steps)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        PlayerCTRL activePlayer = gameManager.players.Find(p => p.PlayerID == currentPlayer.PlayerID);
        
        if (activePlayer != null)
        {
            Debug.Log($"Bank Card Action: Moving player {currentPlayer.PlayerID} {steps} steps forward");
            activePlayer.StartMove(steps);
        }
        else
        {
            Debug.LogError($"Could not find PlayerCTRL for player {currentPlayer.PlayerID}");
        }
    }

    public void MovePlayerToField(int fieldPosition)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        PlayerCTRL activePlayer = gameManager.players.Find(p => p.PlayerID == currentPlayer.PlayerID);
        
        if (activePlayer != null)
        {
            int currentPos = activePlayer.currentPos;
            int stepsNeeded = (fieldPosition - currentPos + 40) % 40;
            
            Debug.Log($"Bank Card Action: Moving player {currentPlayer.PlayerID} to field {fieldPosition} ({stepsNeeded} steps)");
            activePlayer.StartMove(stepsNeeded);
        }
    }

    public void SkipTurn()
    {
        PlayerData current = gameManager.GetCurrentPlayer();
        if (current == null)
        {
            Debug.LogError("SkipTurn: no current player!");
            gameManager.EndTurn();
            return;
        }

        current.hasToSkip = true;
        Debug.Log($"Bank Card Action: Player {current.PlayerID} will skip their next turn.");
        gameManager.EndTurn();
    }

    public void RollAgain()
    {
        Debug.Log($"Bank Card Action: Player {gameManager.GetCurrentPlayer().PlayerID} gets to roll again!");
        gameManager.playerMovement.setIsTurnInProgress(false);
        GameObject moveButton = gameManager.playerMovement.getMoveButton();
        moveButton.SetActive(true);
        gameManager.uiManager.UpdateMoneyDisplay();
        Debug.Log("Player can now roll again!");
    }

    public void AddMoney(int amount)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            gameManager.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Action Card: Player {currentPlayer.PlayerID} receives {amount}€");
        }
        else
        {
            Debug.LogError("AddMoneyFromActionCard: Current player is null!");
        }
    }

    public void AddMoneyAndMove(int amount)
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            gameManager.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Bank Card Action: Player {currentPlayer.PlayerID} receives {amount}€");
        }
        else
        {
            Debug.LogError("AddMoneyFromBankCard: Current player is null!");
            gameManager.EndTurn();
        }
    }


    public void MoveToNextCompanyField()
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("MoveToNextCompanyField: Current player is null!");
            gameManager.EndTurn();
            return;
        }

        
        List<int> companyFields = currentPlayer.companies; 
        if (companyFields == null || companyFields.Count == 0)
        {
            Debug.LogError("MoveToNextCompanyField: No company fields owned by Player!");
            gameManager.EndTurn();
            return;
        }

        int nearestCompany = -1;
        int shortestDistance = 40;

        foreach (int companyField in companyFields)
        {
            int distance = (companyField - currentPlayer.BoardPosition + 40) % 40;
            if (distance > 0 && distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestCompany = companyField;
            }
        }

        if (nearestCompany == -1)
        {
            Debug.LogWarning("MoveToNextCompanyField: No next company found.");
            gameManager.EndTurn();
            return;
        }

        Debug.Log($"Moving player {currentPlayer.PlayerID} to next company field {nearestCompany} ({shortestDistance} steps ahead)");
        
        MovePlayerToField(nearestCompany);
    }

    public void MoveToChosenCompanyField()
    {   
        StartCoroutine(MoveToChosenCompanyFieldCoroutine());
    }

    private IEnumerator MoveToChosenCompanyFieldCoroutine()
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        gameManager.cameraManager.SetTopView();
        FieldSelector selector = gameManager.fieldSelector;

        if (selector == null)
        {
            Debug.LogError("MoveToChosenCompanyField: FieldSelector not found!");
            yield break;
        }

        List<int> allowedCompanies = new List<int>();
        allowedCompanies.AddRange(currentPlayer.companies);
        allowedCompanies.AddRange(gameManager.GetUnownedFieldIndices());

        selector.SetAllowedFields(allowedCompanies);
        selector.EnableSelection();
        
        Debug.Log("Selecting company field to move to...");

        // Wait until the player confirms their selection
        yield return new WaitUntil(() => selector.HasConfirmedSelection());
        
        Debug.Log("Company field selected.");
        int selectedFieldId = selector.GetSelectedFieldId();
        selector.DisableSelection();
        gameManager.cameraManager.SetTopView();
        MovePlayerToField(selectedFieldId);
    }

    public void MoveToChosenField()
    {
        StartCoroutine(MoveToChosenFieldCoroutine());
    }

    private IEnumerator MoveToChosenFieldCoroutine()
    {
        PlayerData currentPlayer = gameManager.GetCurrentPlayer();
        gameManager.cameraManager.SetTopView();
        FieldSelector selector = gameManager.fieldSelector;

        if (selector == null)
        {
            Debug.LogError("MoveToChosenField: FieldSelector not found!");
            yield break;
        }

        List<int> allowedCompanies = new List<int>();
        allowedCompanies.AddRange(currentPlayer.companies);
        allowedCompanies.AddRange(gameManager.GetBankAndActionFieldIndices());
        allowedCompanies.AddRange(gameManager.GetUnownedFieldIndices());

        selector.SetAllowedFields(allowedCompanies);
        selector.EnableSelection();

        // Wait until the player confirms their selection
        yield return new WaitUntil(() => selector.HasConfirmedSelection());

        int selectedFieldId = selector.GetSelectedFieldId();
        selector.DisableSelection();
        gameManager.cameraManager.SetTopView();
        MovePlayerToField(selectedFieldId);
    }
}
