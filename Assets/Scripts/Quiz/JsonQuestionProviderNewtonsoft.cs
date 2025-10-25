using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class JsonQuestionProviderNewtonsoft : MonoBehaviour
{
    [Header("Quelle (Resources)")]
    [Tooltip("Pfad in Resources ohne .json, z.B. 'questions/business_de'")]
    public string resourcePath = "questions/business_de";

    [Header("Filter")]
    [Tooltip("Level-Key im JSON, z.B. 'junior-de' oder 'senior-de'")]
    public string levelKey = "senior-de";

    [Tooltip("Kategorie-Key im JSON, z.B. 'gruendung'")]
    public string categoryKey = "gruendung";

    public List<Question> LoadQuestions()
    {
        var ta = Resources.Load<TextAsset>(resourcePath);
        if (ta == null)
        {
            Debug.LogError($"[QuestionProvider] Resource '{resourcePath}.json' nicht gefunden.");
            return new List<Question>();
        }

        var root = JObject.Parse(ta.text);

        if (!root.TryGetValue(levelKey, out JToken levelToken) || levelToken.Type != JTokenType.Object)
        {
            Debug.LogError($"[QuestionProvider] Level '{levelKey}' nicht gefunden.");
            return new List<Question>();
        }

        var levelObj = (JObject)levelToken;

        if (!levelObj.TryGetValue(categoryKey, out JToken catToken) || catToken.Type != JTokenType.Array)
        {
            Debug.LogError($"[QuestionProvider] Kategorie '{categoryKey}' in Level '{levelKey}' nicht gefunden.");
            return new List<Question>();
        }

        var arr = (JArray)catToken;
        var result = new List<Question>(arr.Count);

        foreach (var item in arr)
        {
            var q = new Question
            {
                id = item.Value<int>("id"),
                text = item.Value<string>("text"),
                options = item["options"]?.ToObject<List<string>>() ?? new List<string>(),
                correctIndex = item.Value<int>("correctIndex")
            };

            // Basic-Checks
            if (q.options == null || q.options.Count != 3)
            {
                Debug.LogWarning($"[QuestionProvider] Frage {q.id} hat nicht exakt 3 Optionen.");
                continue;
            }
            if (q.correctIndex < 0 || q.correctIndex > 2)
            {
                Debug.LogWarning($"[QuestionProvider] Frage {q.id} hat ungültigen correctIndex: {q.correctIndex}");
                continue;
            }

            result.Add(q);
        }

        return result;
    }
}
