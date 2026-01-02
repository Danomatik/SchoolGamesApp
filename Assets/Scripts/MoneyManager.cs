using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MoneyManager : MonoBehaviour
{
    private GameManager gm;

    private void Awake()
    {
        gm = GetComponent<GameManager>();
    }
    public void AddMoney(int amount)
    {
        PlayerData currentPlayer = gm.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            gm.uiManager.UpdateMoneyDisplay();
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
            Debug.Log($"Spieler {currentPlayer.PlayerID} bezahlt {amount}€. Neuer Stand: {currentPlayer.Money}€");
            return true;
        }
        Debug.LogWarning($"Spieler {currentPlayer.PlayerID} hat zu wenig Geld, um {amount}€ zu bezahlen!");
        return false;
    }

    /// <summary>
    /// Berechnet den Gesamtwert eines Spielers (Bargeld + Unternehmen)
    /// </summary>
    public int CalculateTotalAssets(PlayerData player)
    {
        if (player == null) return 0;

        int totalValue = player.Money;

        // Addiere den Wert aller Unternehmen (50% der Gründungskosten)
        var allFields = gm.gameInitiator.GetCompanyFields();
        foreach (var fieldIndex in player.companies)
        {
            var field = allFields.FirstOrDefault(f => f.fieldIndex == fieldIndex);
            if (field != null)
            {
                var company = gm.gameInitiator.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field.companyID);
                if (company != null)
                {
                    // Versteigerungspreis = 50% der Gründungskosten
                    int auctionPrice = company.costFound / 2;
                    totalValue += auctionPrice;
                }
            }
        }

        return totalValue;
    }

    /// <summary>
    /// Prüft ob ein Spieler eine Zahlung leisten kann (mit Insolvenzprüfung)
    /// </summary>
    public bool CanAffordPayment(PlayerData player, int amount)
    {
        if (player == null) return false;
        
        // Wenn genug Bargeld vorhanden, kann bezahlt werden
        if (player.Money >= amount) return true;

        // Prüfe ob Gesamtwert ausreicht
        int totalAssets = CalculateTotalAssets(player);
        return totalAssets >= amount;
    }

    /// <summary>
    /// Versucht eine Zahlung zu leisten. Wenn nicht genug Bargeld, wird Insolvenz ausgelöst.
    /// </summary>
    public bool TryPayAmount(PlayerData payer, int amount, string reason = "")
    {
        if (payer == null) return false;

        // Genug Bargeld vorhanden
        if (payer.Money >= amount)
        {
            payer.Money -= amount;
            gm.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {payer.PlayerID} bezahlt {amount}€ ({reason}). Neuer Stand: {payer.Money}€");
            return true;
        }

        // Prüfe Insolvenz
        int totalAssets = CalculateTotalAssets(payer);
        if (totalAssets < amount)
        {
            Debug.LogError($"Spieler {payer.PlayerID} ist zahlungsunfähig! Benötigt {amount}€, hat aber nur {totalAssets}€ (Bargeld: {payer.Money}€)");
            return false;
        }

        // Insolvenz: Muss Unternehmen versteigern
        Debug.LogWarning($"Spieler {payer.PlayerID} kann {amount}€ nicht bezahlen. Muss Unternehmen versteigern. (Benötigt: {amount}€, Bargeld: {payer.Money}€, Gesamtwert: {totalAssets}€)");
        gm.HandleBankruptcy(payer, amount, reason, null);
        return false; // Zahlung wird durch Versteigerung abgewickelt
    }

    /// <summary>
    /// Versucht eine Zahlung zu leisten mit Empfänger (z.B. Miete)
    /// </summary>
    public bool TryPayAmount(PlayerData payer, int amount, PlayerData recipient, string reason = "")
    {
        if (payer == null) return false;

        // Genug Bargeld vorhanden
        if (payer.Money >= amount)
        {
            payer.Money -= amount;
            if (recipient != null)
            {
                recipient.Money += amount;
            }
            gm.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {payer.PlayerID} bezahlt {amount}€ an {(recipient != null ? $"Spieler {recipient.PlayerID}" : "Bank")} ({reason}). Neuer Stand: {payer.Money}€");
            return true;
        }

        // Prüfe Insolvenz
        int totalAssets = CalculateTotalAssets(payer);
        if (totalAssets < amount)
        {
            Debug.LogError($"Spieler {payer.PlayerID} ist zahlungsunfähig! Benötigt {amount}€, hat aber nur {totalAssets}€ (Bargeld: {payer.Money}€)");
            return false;
        }

        // Insolvenz: Muss Unternehmen versteigern
        Debug.LogWarning($"Spieler {payer.PlayerID} kann {amount}€ nicht bezahlen. Muss Unternehmen versteigern. (Benötigt: {amount}€, Bargeld: {payer.Money}€, Gesamtwert: {totalAssets}€)");
        gm.HandleBankruptcy(payer, amount, reason, recipient);
        return false; // Zahlung wird durch Versteigerung abgewickelt
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

        // Versuche Miete zu zahlen (mit Insolvenzprüfung)
        if (TryPayAmount(payer, rent, owner, $"Miete für {company.companyName}"))
        {
            gm.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {payer.PlayerID} zahlt {rent}€ Miete an Spieler {owner.PlayerID}");
            gm.EndTurn();
        }
        else
        {
            // Insolvenz wird in TryPayAmount/HandleBankruptcy behandelt
            // Wenn Versteigerung erfolgreich, wird die Miete danach bezahlt und EndTurn() aufgerufen
            // Wenn Insolvenz nicht aufgelöst werden kann, wird EndTurn() trotzdem aufgerufen
        }
    }
}