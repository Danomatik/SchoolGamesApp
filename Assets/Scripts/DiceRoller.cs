using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class DiceRoller : MonoBehaviour
{
    public Rigidbody dice1;
    public Rigidbody dice2;

    public Transform spawnPos1;
    public Transform spawnPos2;

    public float throwForce = 8f;
    public float torqueForce = 10f;

    private bool rolling = false;

    public GameManager gameManager;

    public Transform diceFocus;

    public CinemachineCamera cam;

    public void RollDice()
    {
        if (rolling) return;
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        rolling = true;

        // Reset Positionen
        ResetDice(dice1, spawnPos1);
        ResetDice(dice2, spawnPos2);

        // Kraft + Drehmoment anwenden
        ThrowDice(dice1);
        ThrowDice(dice2);

        cam.Follow = diceFocus;

        // Warten bis sie still sind
        yield return new WaitUntil(() => dice1.IsSleeping() && dice2.IsSleeping());

        // Ergebnisse
        GetAddedValue();

        Debug.Log(GetAddedValue());

        gameManager.TakeTurn();

        // → hier kannst du MoveDiceToCamera oder Spielfigurbewegung starten

        rolling = false;
    }

    void ResetDice(Rigidbody rb, Transform startPos)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.transform.position = startPos.position;
        rb.transform.rotation = Random.rotation;
    }

    void ThrowDice(Rigidbody rb)
    {
        rb.AddForce(Vector3.down * throwForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
    }

    // 👇 Gemeinsame Funktion für beide Würfel
    int GetDiceValue(Rigidbody dice)
    {
        Vector3[] directions = {
            dice.transform.up,
            -dice.transform.up,
            dice.transform.right,
            -dice.transform.right,
            dice.transform.forward,
            -dice.transform.forward
        };

        // Dein manuell kalibriertes Mapping (gleich für beide Modelle)
        int[] faceValues = { 1, 6, 3, 4, 2, 5 };

        float maxDot = -1f;
        int bestIndex = 0;

        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector3.Dot(Vector3.up, directions[i]);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestIndex = i;
            }
        }

        return faceValues[bestIndex];
    }

    public int GetAddedValue()
    {
        int val1 = GetDiceValue(dice1);
        int val2 = GetDiceValue(dice2);
        return val1 + val2;
    }
}
