using System;
using System.Collections.Generic;

[Serializable]
public class LearnProgress
{
    // IDs der Fragen, die mindestens einmal richtig beantwortet wurden
    public List<int> masteredIds = new List<int>();

    public bool IsMastered(int id) => masteredIds.Contains(id);

    public void MarkMastered(int id)
    {
        if (!masteredIds.Contains(id))
            masteredIds.Add(id);
    }
}
