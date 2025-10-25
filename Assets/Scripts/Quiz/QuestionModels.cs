using System;
using System.Collections.Generic;

[Serializable]
public class Question
{
    public int id;
    public string text;
    public List<string> options; // exakt 3
    public int correctIndex;     // 0..2
}
