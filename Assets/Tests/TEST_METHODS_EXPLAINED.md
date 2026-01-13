# Test-Methoden Erklärung

Diese Dokumentation erklärt die verschiedenen Test-Methoden und Patterns, die in diesem Projekt verwendet werden.

---

## 📋 Inhaltsverzeichnis

1. [Test-Typen](#test-typen)
2. [Gemeinsame Test-Patterns](#gemeinsame-test-patterns)
3. [EditMode Test-Methoden](#editmode-test-methoden)
4. [PlayMode Test-Methoden](#playmode-test-methoden)
5. [Assert-Methoden](#assert-methoden)
6. [Beispiele](#beispiele)

---

## Test-Typen

### EditMode Tests (Unit Tests)
- **Attribut**: `[Test]`
- **Rückgabetyp**: `void`
- **Laufzeit**: Editor (ohne Play Mode)
- **Geschwindigkeit**: ⚡ Sehr schnell (Millisekunden)
- **Verwendung**: Isolierte Logik, Datenstrukturen, Berechnungen

### PlayMode Tests (Integration Tests)
- **Attribut**: `[UnityTest]`
- **Rückgabetyp**: `IEnumerator`
- **Laufzeit**: Play Mode (Scene wird geladen)
- **Geschwindigkeit**: 🐌 Langsamer (Sekunden)
- **Verwendung**: Vollständige Systeme, Unity-Komponenten, Gameplay-Flows

---

## Gemeinsame Test-Patterns

### 1. Arrange-Act-Assert Pattern

**Das Standard-Pattern für alle Tests:**

```csharp
[Test]
public void TestName_Scenario_ExpectedResult()
{
    // Arrange - Setup der Test-Daten
    int value = 5;
    int expected = 10;
    
    // Act - Führe die Aktion aus
    int result = value * 2;
    
    // Assert - Prüfe das Ergebnis
    Assert.AreEqual(expected, result);
}
```

**Verwendung:**
- Alle Tests folgen diesem Pattern
- Macht Tests lesbar und verständlich
- Klare Trennung zwischen Setup, Ausführung und Prüfung

---

### 2. Setup und TearDown

**Für wiederholbare Test-Umgebungen:**

#### EditMode Setup:
```csharp
[SetUp]
public void SetUp()
{
    // Wird vor JEDEM Test ausgeführt
    // Beispiel: PlayerPrefs löschen, Test-Objekte erstellen
    PlayerPrefs.DeleteAll();
    testObject = new GameObject("TestObject");
}

[TearDown]
public void TearDown()
{
    // Wird nach JEDEM Test ausgeführt
    // Beispiel: Aufräumen, Objekte zerstören
    Object.DestroyImmediate(testObject);
    PlayerPrefs.DeleteAll();
}
```

#### PlayMode Setup:
```csharp
[UnitySetUp]
public IEnumerator SetUp()
{
    // Wird vor JEDEM Test ausgeführt
    // Beispiel: Scene laden, Manager finden
    SceneManager.LoadScene("MainScene");
    yield return new WaitForSeconds(2f);
    
    gm = Object.FindFirstObjectByType<GameManager>();
    Assert.IsNotNull(gm);
}

[UnityTearDown]
public IEnumerator TearDown()
{
    // Wird nach JEDEM Test ausgeführt
    // Beispiel: Save-Dateien löschen
    string savePath = Path.Combine(Application.persistentDataPath, "game_save.json");
    if (File.Exists(savePath)) File.Delete(savePath);
    yield return null;
}
```

**Verwendung:**
- **SetUp**: Initialisiert Test-Umgebung (PlayerPrefs löschen, Scene laden, Manager finden)
- **TearDown**: Räumt auf (Dateien löschen, Objekte zerstören)
- Verhindert, dass Tests sich gegenseitig beeinflussen

---

### 3. Test-Isolation

**Jeder Test sollte unabhängig sein:**

```csharp
[Test]
public void Test1_DoesSomething()
{
    // Arrange: Setze explizit alle benötigten Werte
    player.Money = 1000;
    player.companies.Clear();
    
    // Act & Assert
    // ...
}

[Test]
public void Test2_DoesSomethingElse()
{
    // Arrange: Setze wieder explizit alle Werte
    // (auch wenn Test1 sie schon gesetzt hat)
    player.Money = 500;
    player.companies.Clear();
    
    // Act & Assert
    // ...
}
```

**Wichtig:**
- Jeder Test setzt seine eigenen Daten
- Tests sollten in beliebiger Reihenfolge laufen können
- Keine Abhängigkeiten zwischen Tests

---

## EditMode Test-Methoden

### 1. Datenstruktur-Tests

**Testet Klassen ohne Unity-Komponenten:**

```csharp
[Test]
public void PlayerData_Initialization_DefaultValues()
{
    // Arrange & Act
    PlayerData player = new PlayerData();
    
    // Assert: Prüfe Standardwerte
    Assert.AreEqual(0, player.PlayerID);
    Assert.AreEqual(0, player.Money);
    Assert.IsNotNull(player.companies);
    Assert.AreEqual(0, player.companies.Count);
}
```

**Verwendung:**
- `PlayerDataTests` - Testet PlayerData Klasse
- `GameStateTests` - Testet GameState Klasse
- `GameSaveDataTests` - Testet Save-Datenstrukturen

**Pattern:**
1. Erstelle Objekt
2. Prüfe Standardwerte oder setze Werte
3. Prüfe dass Werte korrekt sind

---

### 2. Berechnungs-Tests

**Testet reine Logik ohne Unity:**

```csharp
[Test]
public void MoneyCalculations_AddMoney_CalculatesCorrectly()
{
    // Arrange
    int money = 1000;
    int amount = 500;
    
    // Act
    int result = money + amount;
    
    // Assert
    Assert.AreEqual(1500, result);
}
```

**Verwendung:**
- `MoneyCalculationsTests` - Geld-Berechnungen
- `DiceCalculationsTests` - Würfel-Berechnungen
- `TimerCalculationsTests` - Zeit-Berechnungen
- `CompanyLevelTests` - Unternehmen-Berechnungen

**Pattern:**
1. Setze Eingabewerte
2. Führe Berechnung aus
3. Prüfe Ergebnis

---

### 3. PlayerPrefs-Tests

**Testet Speichern/Laden von Einstellungen:**

```csharp
[SetUp]
public void SetUp()
{
    PlayerPrefs.DeleteAll(); // Saubere Umgebung
}

[Test]
public void PlayerSetupManager_SetPlayerCount_SavesCorrectly()
{
    // Arrange
    setupManager.SetPlayerCount(4);
    
    // Act
    int savedCount = setupManager.GetPlayerCount();
    
    // Assert
    Assert.AreEqual(4, savedCount);
}
```

**Verwendung:**
- `PlayerSetupManagerTests` - Spieler-Einstellungen speichern/laden

**Pattern:**
1. Setze Wert
2. Lade Wert zurück
3. Prüfe dass Wert korrekt ist

---

## PlayMode Test-Methoden

### 1. Scene-basierte Tests

**Testet Systeme die eine Scene benötigen:**

```csharp
[UnitySetUp]
public IEnumerator SetUp()
{
    // Lade Scene
    SceneManager.LoadScene("MainScene");
    yield return new WaitForSeconds(2f);
    
    // Finde Manager
    gm = Object.FindFirstObjectByType<GameManager>();
    Assert.IsNotNull(gm);
    
    // Warte bis Systeme initialisiert sind
    yield return new WaitUntil(() => !gm.InitiativeInProgress);
}
```

**Verwendung:**
- Alle PlayMode Tests (MoneyManager, Bankruptcy, GameTimer, etc.)

**Pattern:**
1. Lade Scene
2. Warte auf Initialisierung
3. Finde benötigte Manager
4. Führe Test aus

---

### 2. Asynchrone Operationen

**Testet Coroutines und asynchrone Aktionen:**

```csharp
[UnityTest]
public IEnumerator Test_SaveGame_SavesSuccessfully()
{
    // Arrange
    player.Money = 5000;
    
    // Act
    bool success = saveManager.SaveGame(gi);
    yield return new WaitForSeconds(0.5f); // Warte auf Abschluss
    
    // Assert
    Assert.IsTrue(success);
}
```

**Verwendung:**
- Save/Load Tests
- Bewegung-Tests
- UI-Tests

**Pattern:**
1. Führe asynchrone Aktion aus
2. Warte mit `yield return new WaitForSeconds()` oder `yield return new WaitUntil()`
3. Prüfe Ergebnis

---

### 3. State-Verification Tests

**Testet dass System-Zustand korrekt ist:**

```csharp
[UnityTest]
public IEnumerator Test_PlayerElimination_RemovesPlayer()
{
    // Arrange
    int originalCount = gi.CurrentGame.AllPlayers.Count;
    player.Money = 0;
    player.companies.Clear();
    
    // Act
    moneyManager.TryPayAmount(player, 500, "Test");
    yield return new WaitForSeconds(1.5f);
    
    // Assert: Prüfe dass Spieler entfernt wurde
    Assert.AreEqual(originalCount - 1, gi.CurrentGame.AllPlayers.Count);
    Assert.IsTrue(player.isEliminated);
}
```

**Verwendung:**
- Eliminierungs-Tests
- Bankruptcy-Tests
- Turn-Management Tests

**Pattern:**
1. Merke ursprünglichen Zustand
2. Führe Aktion aus
3. Prüfe dass Zustand korrekt geändert wurde

---

### 4. Log-Assertion Tests

**Testet dass bestimmte Logs ausgegeben werden:**

```csharp
[UnityTest]
public IEnumerator Test_PlayerElimination_LogsError()
{
    // Arrange
    player.Money = 0;
    
    // Erwarte Error-Log
    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*ist zahlungsunfähig.*"));
    
    // Act
    moneyManager.TryPayAmount(player, 500, "Test");
    yield return new WaitForSeconds(1.5f);
    
    // Assert: LogAssert prüft automatisch dass Log ausgegeben wurde
}
```

**Verwendung:**
- Tests die Error-Logs erwarten
- Debug-Log Tests

**Pattern:**
1. Erwarte Log mit `LogAssert.Expect()`
2. Führe Aktion aus (die den Log auslöst)
3. LogAssert prüft automatisch

---

### 5. Multi-Step Tests

**Testet komplexe Abläufe mit mehreren Schritten:**

```csharp
[UnityTest]
public IEnumerator Test_SaveLoad_CompleteFlow()
{
    // Schritt 1: Speichere
    player.Money = 5000;
    saveManager.SaveGame(gi);
    yield return new WaitForSeconds(0.5f);
    
    // Schritt 2: Ändere Daten
    player.Money = 1000;
    
    // Schritt 3: Lade
    GameSaveData loaded = saveManager.LoadGame();
    yield return new WaitForSeconds(1f);
    
    // Schritt 4: Prüfe
    Assert.AreEqual(5000, loaded.players[0].Money);
}
```

**Verwendung:**
- Save/Load Tests
- Bankruptcy-Flow Tests
- Turn-Sequence Tests

**Pattern:**
1. Führe mehrere Schritte aus
2. Warte zwischen Schritten
3. Prüfe Ergebnis nach jedem Schritt

---

## Assert-Methoden

### Häufig verwendete Assert-Methoden:

#### Gleichheit prüfen:
```csharp
Assert.AreEqual(expected, actual, "Nachricht");
Assert.AreNotEqual(expected, actual, "Nachricht");
```

#### Boolean prüfen:
```csharp
Assert.IsTrue(condition, "Nachricht");
Assert.IsFalse(condition, "Nachricht");
```

#### Null-Prüfung:
```csharp
Assert.IsNotNull(obj, "Nachricht");
Assert.IsNull(obj, "Nachricht");
```

#### Vergleich:
```csharp
Assert.Greater(value, threshold, "Nachricht");
Assert.Less(value, threshold, "Nachricht");
Assert.GreaterOrEqual(value, threshold, "Nachricht");
Assert.LessOrEqual(value, threshold, "Nachricht");
```

#### Collections:
```csharp
Assert.Contains(item, collection);
Assert.AreEqual(expectedCount, collection.Count);
```

---

## Beispiele

### Beispiel 1: Einfacher EditMode Test

```csharp
[Test]
public void PlayerData_Companies_AddRemove()
{
    // Arrange
    PlayerData player = new PlayerData();
    
    // Act: Add
    player.companies.Add(1);
    player.companies.Add(2);
    
    // Assert
    Assert.AreEqual(2, player.companies.Count);
    Assert.Contains(1, player.companies);
    
    // Act: Remove
    player.companies.Remove(1);
    
    // Assert
    Assert.AreEqual(1, player.companies.Count);
    Assert.IsFalse(player.companies.Contains(1));
}
```

**Was passiert:**
1. Erstellt PlayerData
2. Fügt 2 Unternehmen hinzu → prüft dass 2 vorhanden
3. Entfernt 1 Unternehmen → prüft dass nur noch 1 vorhanden

---

### Beispiel 2: PlayMode Test mit Scene

```csharp
[UnitySetUp]
public IEnumerator SetUp()
{
    SceneManager.LoadScene("MainScene");
    yield return new WaitForSeconds(2f);
    
    gm = Object.FindFirstObjectByType<GameManager>();
    moneyManager = Object.FindFirstObjectByType<MoneyManager>();
    
    yield return new WaitUntil(() => !gm.InitiativeInProgress);
}

[UnityTest]
public IEnumerator Test_AddMoney_IncreasesPlayerMoney()
{
    // Arrange
    var player = gm.GetCurrentPlayer();
    int originalMoney = player.Money;
    
    // Act
    moneyManager.AddMoney(500);
    yield return new WaitForSeconds(0.3f);
    
    // Assert
    Assert.AreEqual(originalMoney + 500, player.Money);
}
```

**Was passiert:**
1. SetUp lädt Scene und findet Manager
2. Test holt aktuellen Spieler
3. Fügt Geld hinzu
4. Prüft dass Geld erhöht wurde

---

### Beispiel 3: Test mit LogAssert

```csharp
[UnityTest]
public IEnumerator Test_PlayerElimination_LogsError()
{
    // Arrange
    var player = gm.GetCurrentPlayer();
    player.Money = 0;
    player.companies.Clear();
    
    // Erwarte Error-Log
    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*ist zahlungsunfähig.*"));
    
    // Act
    moneyManager.TryPayAmount(player, 500, "Test");
    yield return new WaitForSeconds(1.5f);
    
    // Assert: LogAssert prüft automatisch
    Assert.IsTrue(player.isEliminated);
}
```

**Was passiert:**
1. Setzt Spieler auf 0 Geld
2. Erwartet Error-Log
3. Versucht Zahlung (löst Error aus)
4. Prüft dass Spieler eliminiert wurde

---

## Test-Namenskonventionen

**Format:** `TestName_Scenario_ExpectedResult`

**Beispiele:**
- `PlayerData_Initialization_DefaultValues` - Prüft Standardwerte
- `AddMoney_IncreasesPlayerMoney` - Prüft dass Geld erhöht wird
- `PlayerElimination_RemovesPlayer` - Prüft dass Spieler entfernt wird
- `CanAffordPayment_WithEnoughCash_ReturnsTrue` - Prüft Zahlungsfähigkeit

**Vorteile:**
- Klar was getestet wird
- Einfach zu finden wenn Test fehlschlägt
- Selbst-dokumentierend

---

## Wichtige Hinweise

### 1. Wartezeiten in PlayMode Tests
```csharp
yield return new WaitForSeconds(0.5f);  // Feste Wartezeit
yield return new WaitUntil(() => condition);  // Warte bis Bedingung erfüllt
yield return null;  // Warte einen Frame
```

### 2. Test-Isolation
- Jeder Test sollte unabhängig sein
- SetUp/TearDown für saubere Umgebung
- Keine Abhängigkeiten zwischen Tests

### 3. Assert-Nachrichten
```csharp
Assert.AreEqual(expected, actual, "Klare Fehlermeldung");
// Hilft beim Debuggen wenn Test fehlschlägt
```

### 4. Debug-Logs in Tests
```csharp
Debug.Log($"   Player money: {player.Money}€");
// Hilft beim Verstehen was im Test passiert
```

---

## Zusammenfassung

**EditMode Tests:**
- Schnell, isoliert, testen reine Logik
- Verwenden `[Test]` und `Assert.*`
- Keine Unity Runtime nötig

**PlayMode Tests:**
- Langsamer, testen vollständige Systeme
- Verwenden `[UnityTest]` und `IEnumerator`
- Benötigen Scene und Unity Runtime

**Gemeinsam:**
- Beide verwenden Arrange-Act-Assert Pattern
- Beide verwenden Assert-Methoden
- Beide sollten isoliert und wiederholbar sein
