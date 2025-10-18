using System;
using UnityEngine;

[Serializable]
public class QuestionData
{
    public int id;
    public string text;
    public string[] options;
    public int correctIndex;
}
