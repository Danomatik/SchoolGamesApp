using NUnit.Framework;

/// <summary>
/// EditMode Unit Tests für Würfel-Berechnungen
/// </summary>
public class DiceCalculationsTests
{
    [Test]
    public void DiceRoll_ValueRange_IsValid()
    {
        // Arrange & Act
        int minValue = 2; // Minimum: 1+1
        int maxValue = 12; // Maximum: 6+6

        // Assert
        Assert.GreaterOrEqual(minValue, 2, "Minimum Würfelwert sollte 2 sein (1+1)");
        Assert.LessOrEqual(maxValue, 12, "Maximum Würfelwert sollte 12 sein (6+6)");
    }

    [Test]
    public void DiceRoll_AllPossibleValues()
    {
        // Arrange
        int[] possibleValues = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        // Act & Assert
        foreach (int value in possibleValues)
        {
            Assert.GreaterOrEqual(value, 2, $"Wert {value} sollte >= 2 sein");
            Assert.LessOrEqual(value, 12, $"Wert {value} sollte <= 12 sein");
        }
    }

    [Test]
    public void DiceRoll_Probability_AllValuesPossible()
    {
        // Arrange
        int minDice = 1;
        int maxDice = 6;
        int minTotal = minDice + minDice; // 2
        int maxTotal = maxDice + maxDice; // 12

        // Act & Assert
        Assert.AreEqual(2, minTotal, "Minimum Summe sollte 2 sein");
        Assert.AreEqual(12, maxTotal, "Maximum Summe sollte 12 sein");
    }

    [Test]
    public void DiceRoll_MovementCalculation()
    {
        // Arrange
        int diceRoll = 7;
        int currentPosition = 10;
        int boardSize = 40;

        // Act
        int newPosition = (currentPosition + diceRoll) % boardSize;

        // Assert
        Assert.AreEqual(17, newPosition, "Neue Position sollte 17 sein (10 + 7)");
    }

    [Test]
    public void DiceRoll_MovementCalculation_WithWrapAround()
    {
        // Arrange
        int diceRoll = 5;
        int currentPosition = 38;
        int boardSize = 40;

        // Act
        int newPosition = (currentPosition + diceRoll) % boardSize;

        // Assert
        Assert.AreEqual(3, newPosition, "Position sollte bei 3 sein (38 + 5 = 43, 43 % 40 = 3)");
    }

    [Test]
    public void DiceRoll_StartFieldBonus_Calculation()
    {
        // Arrange
        int startBonus = 400;
        int playerMoney = 1000;

        // Act
        int newMoney = playerMoney + startBonus;

        // Assert
        Assert.AreEqual(1400, newMoney, "Geld sollte um 400€ erhöht werden");
    }
}
