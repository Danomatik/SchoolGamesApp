# Unity Test Framework - Anleitung

## Übersicht

Dieses Projekt verwendet **Unity Test Framework** (basierend auf NUnit) für Unit Tests und Integration Tests.

## Test-Struktur

```
Assets/Tests/
├── EditMode/          # Unit Tests (laufen im Editor ohne Play Mode)
│   ├── MoneyManagerTests.cs
│   └── GameManagerBankruptcyTests.cs
└── PlayMode/          # Integration Tests (laufen im Play Mode)
    └── BankruptcyPlayModeTests.cs
```

## Tests ausführen

### Methode 1: Unity Test Runner Window
1. **Window → General → Test Runner** öffnen
2. Tab **EditMode** oder **PlayMode** wählen
3. **Run All** klicken oder einzelne Tests ausführen

### Methode 2: Im Code
- Tests haben `[Test]` Attribute für Unit Tests
- Tests haben `[UnityTest]` Attribute für PlayMode Tests

## Test-Typen

### EditMode Tests (Unit Tests)
- Laufen schnell im Editor
- Testen einzelne Funktionen/Methoden
- Keine Unity-Komponenten die Start/Update benötigen

### PlayMode Tests (Integration Tests)
- Laufen im Play Mode
- Testen vollständige Gameplay-Flows
- Können Coroutines verwenden

## Beispiel-Tests

### MoneyManagerTests
- `CalculateTotalAssets_OnlyMoney_ReturnsMoneyAmount()` - Testet Gesamtwert-Berechnung
- `CanAffordPayment_EnoughMoney_ReturnsTrue()` - Testet Zahlungsfähigkeit
- `TryPayAmount_EnoughMoney_ReturnsTrue()` - Testet Zahlungsversuch

### GameManagerBankruptcyTests
- `GetAuctionableCompanies_PlayerHasCompanies_ReturnsList()` - Testet Versteigerungs-Liste
- `HandleBankruptcy_PlayerHasNoCompanies_EndsTurn()` - Testet Insolvenz-Handling

## Neue Tests hinzufügen

1. Erstelle neue Test-Datei in `Assets/Tests/EditMode/` oder `Assets/Tests/PlayMode/`
2. Verwende `[Test]` für Unit Tests oder `[UnityTest]` für PlayMode Tests
3. Struktur:
   ```csharp
   [Test]
   public void TestName_Scenario_ExpectedResult()
   {
       // Arrange - Setup
       // Act - Execute
       // Assert - Verify
   }
   ```

## Best Practices

- **Arrange-Act-Assert Pattern**: Klare Struktur in jedem Test
- **Isolierte Tests**: Jeder Test sollte unabhängig sein
- **Aussagekräftige Namen**: Test-Namen sollten klar sein was getestet wird
- **SetUp/TearDown**: Verwende `[SetUp]` und `[TearDown]` für gemeinsame Initialisierung

## Weitere Ressourcen

- [Unity Test Framework Dokumentation](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit Dokumentation](https://docs.nunit.org/)

