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
    /// Laut Spielregeln: Vermögenswerte = Gründungskosten + Investitionskosten + AG-Kosten + Bargeld
    /// </summary>
    public int CalculateTotalAssets(PlayerData player)
    {
        if (player == null) return 0;

        int totalValue = player.Money;

        // Addiere den Wert aller Unternehmen (tatsächlich investierter Betrag)
        var allFields = gm.gameInitiator.GetCompanyFields();
        foreach (var fieldIndex in player.companies)
        {
            var field = allFields.FirstOrDefault(f => f.fieldIndex == fieldIndex);
            if (field != null)
            {
                var company = gm.gameInitiator.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field.companyID);
                if (company != null)
                {
                    // Vermögenswert = tatsächlich investierter Betrag
                    int companyValue = 0;
                    
                    // Gründungskosten (immer wenn besessen)
                    if (field.level >= CompanyLevel.Founded)
                    {
                        companyValue += company.costFound;
                    }
                    
                    // Investitionskosten (wenn investiert oder AG)
                    if (field.level >= CompanyLevel.Invested)
                    {
                        companyValue += company.costInvest;
                    }
                    
                    // AG-Kosten (wenn AG)
                    if (field.level == CompanyLevel.AG)
                    {
                        companyValue += company.costAG;
                    }
                    
                    totalValue += companyValue;
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
            // Prüfe Insolvenz
            if (totalAssets < amount)
            {
                Debug.LogError($"💀 Spieler {payer.PlayerID} ({payer.PlayerName}) ist zahlungsunfähig! Benötigt {amount}€, hat aber nur {totalAssets}€ (Bargeld: {payer.Money}€)");
                
                // ✅ ADD THIS DEBUG LINE
                Debug.LogError($"[DEBUG] About to call EliminatePlayer for {payer.PlayerName}");
                
                // ✅ SPIELER ELIMINIEREN
                EliminatePlayer(payer, $"Konnte {amount}€ nicht bezahlen ({reason})");
                
                // ✅ ADD THIS DEBUG LINE
                Debug.LogError($"[DEBUG] After calling EliminatePlayer. Player.isEliminated = {payer.isEliminated}");
                
                return false;
            }

            Debug.LogError($"💀 Spieler {payer.PlayerID} ({payer.PlayerName}) ist zahlungsunfähig! Benötigt {amount}€, hat aber nur {totalAssets}€ (Bargeld: {payer.Money}€)");
            
            // ✅ SPIELER ELIMINIEREN
            EliminatePlayer(payer, $"Konnte {amount}€ nicht bezahlen ({reason})");
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
            Debug.LogError($"💀 Spieler {payer.PlayerID} ({payer.PlayerName}) ist zahlungsunfähig! Benötigt {amount}€, hat aber nur {totalAssets}€ (Bargeld: {payer.Money}€)");
            
            // ✅ SPIELER ELIMINIEREN
            EliminatePlayer(payer, $"Konnte {amount}€ Miete an {recipient.PlayerName} nicht bezahlen");
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

    // ============================================================
    // 💀 SPIELER ELIMINIERUNG
    // ============================================================

    /// <summary>
    /// Eliminiert einen Spieler aus dem Spiel
    /// </summary>
/// <summary>
/// Eliminiert einen Spieler aus dem Spiel
/// </summary>
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
    
    // ✅ WICHTIG: Markiere ZUERST als eliminiert
    player.isEliminated = true;
    Debug.Log($"   isEliminated gesetzt auf: {player.isEliminated}");

    // Entferne alle Unternehmen des Spielers
    var allFields = gm.gameInitiator.GetCompanyFields();
    foreach (var fieldIndex in player.companies.ToList()) // ToList() wichtig!
    {
        var field = allFields.FirstOrDefault(f => f.fieldIndex == fieldIndex);
        if (field != null)
        {
            field.ownerID = -1;
            field.level = CompanyLevel.None;
            
            // Update visuals
            if (gm.boardVisuals != null)
            {
                gm.boardVisuals.UpdateFieldVisual(field);
            }
            
            Debug.Log($"   Feld {field.fieldIndex} freigegeben");
        }
    }
    player.companies.Clear();
    Debug.Log($"   Unternehmen nach Clearing: {player.companies.Count}");

    // Deaktiviere PlayerCTRL GameObject
    var playerCTRL = gm.players.Find(p => p.PlayerID == player.PlayerID);
    if (playerCTRL != null)
    {
        playerCTRL.gameObject.SetActive(false);
        Debug.Log($"   PlayerCTRL für {playerName} deaktiviert");
    }

    // Entferne Spieler aus der aktiven Spielerliste
    int playerCountBefore = gm.gameInitiator.CurrentGame.AllPlayers.Count;
    gm.gameInitiator.CurrentGame.AllPlayers.Remove(player);
    int playerCountAfter = gm.gameInitiator.CurrentGame.AllPlayers.Count;
    
    Debug.Log($"   Spieler aus AllPlayers entfernt");
    Debug.Log($"   Spieleranzahl: {playerCountBefore} → {playerCountAfter}");
    Debug.Log($"════════════════════════════════════════");

    // Prüfe ob Spiel zu Ende ist
    CheckGameOver();

    // Beende den Zug (wechselt automatisch zum nächsten Spieler)
    gm.EndTurn();
}


    /// <summary>
    /// Prüft ob das Spiel zu Ende ist (nur noch 1 Spieler übrig)
    /// </summary>
    private void CheckGameOver()
    {
        int remainingPlayers = gm.gameInitiator.CurrentGame.AllPlayers.Count;

        Debug.Log($"[MoneyManager] Verbleibende Spieler: {remainingPlayers}");

        if (remainingPlayers <= 1)
        {
            Debug.Log("🏁 SPIEL ZU ENDE! Nur noch 1 Spieler übrig (oder keine).");
            
            // Zeige Game Over Screen
            if (gm.gameTimerManager != null)
            {
                gm.gameTimerManager.TriggerGameOver();
            }
            else
            {
                Debug.LogWarning("[MoneyManager] GameTimerManager nicht gefunden! Kann GameOver nicht auslösen.");
            }
        }
    }
}