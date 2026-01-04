using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerData
{
    public int PlayerID;
    public int Money;
    public int BoardPosition;
    public string PlayerName;
    public bool hasToSkip;
    public List<int> companies;
    public bool isEliminated = false;
}