using UnityEngine;

public class FieldUpgradeView : MonoBehaviour
{
    public Transform upgradeAnchor;
    public GameObject foundedPrefab;   // Stufe 1
    public GameObject investedPrefab;  // Stufe 2
    public GameObject agPrefab;        // Stufe 3

    private GameObject current;

    public void SetLevel(CompanyLevel level)
    {
        if (current) { Destroy(current); current = null; }

        GameObject prefab = null;
        switch (level)
        {
            case CompanyLevel.Founded:  prefab = foundedPrefab;  break;
            case CompanyLevel.Invested: prefab = investedPrefab; break;
            case CompanyLevel.AG:       prefab = agPrefab;       break;
        }
        if (!prefab) return;

        var anchor = upgradeAnchor ? upgradeAnchor : transform;
        current = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
    }
}
