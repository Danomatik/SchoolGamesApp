using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MoneyManager : MonoBehaviour
{
    private GameManager gm;

    public MoneyPopupUI moneyPopupUI;

    private void Awake()
    {
        gm = GetComponent<GameManager>();
        moneyPopupUI = MoneyPopupUI.Instance;
    }

    public void AddMoney(int amount)
    {
        PlayerData currentPlayer = gm.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            gm.uiManager.UpdateMoneyDisplay();

            moneyPopupUI?.ShowGain(
                string.IsNullOrEmpty(currentPlayer.PlayerName) ? $"Spieler {currentPlayer.PlayerID}" : currentPlayer.PlayerName,
                amount
            );

            Debug.Log($"Spieler {currentPlayer.PlayerID} erhält {amount}€. Neuer Stand: {currentPlayer.Money}€");
        }
    }

    public void AddMoney(int playerID, int amount)
    {
        PlayerData currentPlayer = gm.gameInitiator.CurrentGame.AllPlayers.Find(p => p.PlayerID == playerID);
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            gm.uiManager.UpdateMoneyDisplay();

            moneyPopupUI?.ShowGain(
                string.IsNullOrEmpty(currentPlayer.PlayerName) ? $"Spieler {currentPlayer.PlayerID}" : currentPlayer.PlayerName,
                amount
            );

            Debug.Log($"Spieler {currentPlayer.PlayerID} erhält {amount}€. Neuer Stand: {currentPlayer.Money}€");
        }
    }

    public bool RemoveMoney(int amount)
    {
        PlayerData currentPlayer = gm.GetCurrentPlayer();
        if (currentPlayer != null && currentPlayer.Money >= amount)
        {
            currentPlayer.Money -= amount;
            gm.uiManager.UpdateMoneyDisplay();

            moneyPopupUI?.ShowLoss(
                string.IsNullOrEmpty(currentPlayer.PlayerName) ? $"Spieler {currentPlayer.PlayerID}" : currentPlayer.PlayerName,
                amount
            );

            Debug.Log($"Spieler {currentPlayer.PlayerID} bezahlt {amount}€. Neuer Stand: {currentPlayer.Money}€");
            return true;
        }
        Debug.LogWarning($"Spieler {currentPlayer.PlayerID} hat zu wenig Geld, um {amount}€ zu bezahlen!");
        return false;
    }

    public int CalculateTotalAssets(PlayerData player)
    {
        if (player == null) return 0;

        int totalValue = player.Money;

        var allFields = gm.gameInitiator.GetCompanyFields();
        foreach (var fieldIndex in player.companies)
        {
            var field = allFields.FirstOrDefault(f => f.fieldIndex == fieldIndex);
            if (field != null)
            {
                var company = gm.gameInitiator.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field.companyID);
                if (company != null)
                {
                    int companyValue = 0;

                    if (field.level >= CompanyLevel.Founded)  companyValue += company.costFound;
                    if (field.level >= CompanyLevel.Invested) companyValue += company.costInvest;
                    if (field.level == CompanyLevel.AG)       companyValue += company.costAG;

                    totalValue += companyValue;
                }
            }
        }

        return totalValue;
    }
    public bool CanAffordPayment(PlayerData player, int amount)
    {
        if (player == null) return false;
        if (player.Money >= amount) return true;

        int totalAssets = CalculateTotalAssets(player);
        return totalAssets >= amount;
    }

    public bool TryPayAmount(PlayerData payer, int amount, string reason = "")
    {
        if (payer == null) return false;

        if (payer.Money >= amount)
        {
            payer.Money -= amount;
            gm.uiManager.UpdateMoneyDisplay();

            moneyPopupUI?.ShowLoss(
                string.IsNullOrEmpty(payer.PlayerName) ? $"Spieler {payer.PlayerID}" : payer.PlayerName,
                amount
            );

            Debug.Log($"Spieler {payer.PlayerID} bezahlt {amount}€ ({reason}). Neuer Stand: {payer.Money}€");
            return true;
        }

        int totalAssets = CalculateTotalAssets(payer);
        if (totalAssets < amount)
        {
            Debug.LogError($"💀 Spieler {payer.PlayerID} ({payer.PlayerName}) ist zahlungsunfähig! Benötigt {amount}€, hat aber nur {totalAssets}€ (Bargeld: {payer.Money}€)");
            EliminatePlayer(payer, $"Konnte {amount}€ nicht bezahlen ({reason})");
            return false;
        }

        Debug.LogWarning($"Spieler {payer.PlayerID} kann {amount}€ nicht bezahlen. Muss Unternehmen versteigern.");
        gm.HandleBankruptcy(payer, amount, reason, null);
        return false;
    }

    public bool TryPayAmount(PlayerData payer, int amount, PlayerData recipient, string reason = "")
    {
        if (payer == null) return false;

        if (payer.Money >= amount)
        {
            payer.Money -= amount;
            if (recipient != null) recipient.Money += amount;
            gm.uiManager.UpdateMoneyDisplay();

            string payerName     = string.IsNullOrEmpty(payer.PlayerName)     ? $"Spieler {payer.PlayerID}"     : payer.PlayerName;
            string recipientName = recipient == null ? "Bank" : (string.IsNullOrEmpty(recipient.PlayerName) ? $"Spieler {recipient.PlayerID}" : recipient.PlayerName);

            moneyPopupUI?.ShowRent(payerName, recipientName, amount, showForPayer: true);

            Debug.Log($"Spieler {payer.PlayerID} bezahlt {amount}€ an {(recipient != null ? $"Spieler {recipient.PlayerID}" : "Bank")} ({reason}). Neuer Stand: {payer.Money}€");
            return true;
        }

        int totalAssets = CalculateTotalAssets(payer);
        if (totalAssets < amount)
        {
            Debug.LogError($"💀 Spieler {payer.PlayerID} ({payer.PlayerName}) ist zahlungsunfähig! Benötigt {amount}€, hat aber nur {totalAssets}€ (Bargeld: {payer.Money}€)");
            EliminatePlayer(payer, $"Konnte {amount}€ Miete an {recipient?.PlayerName} nicht bezahlen");
            return false;
        }

        Debug.LogWarning($"Spieler {payer.PlayerID} kann {amount}€ nicht bezahlen. Muss Unternehmen versteigern.");
        gm.HandleBankruptcy(payer, amount, reason, recipient);
        return false;
    }

    public void PayRent(PlayerData payer, PlayerData owner, CompanyConfigData company, CompanyField field)
    {
        int rent = 0;
        switch (field.level)
        {
            case CompanyLevel.Founded:  rent = company.revenueFound;  break;
            case CompanyLevel.Invested: rent = company.revenueInvest; break;
            case CompanyLevel.AG:       rent = company.revenueAG;     break;
        }

        if (rent <= 0)
        {
            gm.EndTurn();
            return;
        }

        if (TryPayAmount(payer, rent, owner, $"Miete für {company.companyName}"))
        {
            gm.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {payer.PlayerID} zahlt {rent}€ Miete an Spieler {owner.PlayerID}");
            gm.EndTurn();
        }
    }
    public void EliminatePlayer(PlayerData player, string reason)
    {
        if (player == null) return;

        string playerName = string.IsNullOrEmpty(player.PlayerName)
            ? $"Spieler {player.PlayerID}"
            : player.PlayerName;

        Debug.Log($"════════════════════════════════════════");
        Debug.Log($"💀 {playerName} wurde eliminiert!");
        Debug.Log($"   Grund: {reason}");
        Debug.Log($"   Finales Geld: {player.Money}€");
        Debug.Log($"   Unternehmen vor Eliminierung: {player.companies.Count}");

        player.isEliminated = true;

        moneyPopupUI?.ShowElimination(playerName);

        var allFields = gm.gameInitiator.GetCompanyFields();
        foreach (var fieldIndex in player.companies.ToList())
        {
            var field = allFields.FirstOrDefault(f => f.fieldIndex == fieldIndex);
            if (field != null)
            {
                field.ownerID = -1;
                field.level   = CompanyLevel.None;

                if (gm.boardVisuals != null)
                    gm.boardVisuals.UpdateFieldVisual(field);

                Debug.Log($"   Feld {field.fieldIndex} freigegeben");
            }
        }
        player.companies.Clear();

        var playerCTRL = gm.players.Find(p => p.PlayerID == player.PlayerID);
        if (playerCTRL != null)
        {
            playerCTRL.gameObject.SetActive(false);
            Debug.Log($"   PlayerCTRL für {playerName} deaktiviert");
        }

        int playerCountBefore = gm.gameInitiator.CurrentGame.AllPlayers.Count;
        gm.gameInitiator.CurrentGame.AllPlayers.Remove(player);
        int playerCountAfter = gm.gameInitiator.CurrentGame.AllPlayers.Count;

        Debug.Log($"   Spieler aus AllPlayers entfernt");
        Debug.Log($"   Spieleranzahl: {playerCountBefore} → {playerCountAfter}");
        Debug.Log($"════════════════════════════════════════");

        CheckGameOver();
        gm.EndTurn();
    }

    private void CheckGameOver()
    {
        int remainingPlayers = gm.gameInitiator.CurrentGame.AllPlayers.Count;
        Debug.Log($"[MoneyManager] Verbleibende Spieler: {remainingPlayers}");

        if (remainingPlayers <= 1)
        {
            Debug.Log("🏁 SPIEL ZU ENDE! Nur noch 1 Spieler übrig.");

            if (gm.gameTimerManager != null)
                gm.gameTimerManager.TriggerGameOver();
            else
                Debug.LogWarning("[MoneyManager] GameTimerManager nicht gefunden!");
        }
    }
}