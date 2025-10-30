using UnityEngine;

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

    public void PayRent(PlayerData payer, PlayerData owner, CompanyConfigData company, CompanyField field)
    {
        int rent = 0;
        switch (field.level)
        {
            case CompanyLevel.Founded:  rent = company.revenueFound;  break;
            case CompanyLevel.Invested: rent = company.revenueInvest; break;
            case CompanyLevel.AG:       rent = company.revenueAG;     break;
        }
        if (rent <= 0) return;

        if (payer.Money >= rent)
        {
            payer.Money -= rent;
            owner.Money += rent;
            gm.uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {payer.PlayerID} zahlt {rent}€ an Spieler {owner.PlayerID}");
        }
        else
        {
            Debug.LogWarning($"Spieler {payer.PlayerID} kann Miete {rent}€ nicht zahlen.");
        }
    }
}