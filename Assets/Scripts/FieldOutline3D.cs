using UnityEngine;

[RequireComponent(typeof(Transform))]
public class FieldOutline3D : MonoBehaviour
{
    [Header("Outline Shape (local units)")]
    public Vector2 fieldSize = new Vector2(1f, 1f); // Breite (X), Tiefe (Z) des Feldes
    public float lineWidth = 0.03f;
    public float yOffset = 0.02f;      // leicht über dem Boden
    public float padding = 0.02f;      // Rahmen etwas größer als Feld

    [Header("Glow (HDR Emission)")]
    public float glowIntensity = 2.2f; // Bloom!
    public Material lineMaterial;      // optional; wenn leer, wird Unlit erstellt

    private LineRenderer lr;

    void Reset()
    {
        fieldSize = new Vector2(1f, 1f);
        lineWidth = 0.03f;
        yOffset = 0.02f;
        padding = 0.02f;
        glowIntensity = 2.2f;
    }

    void Awake()
    {
        EnsureLR();
        BuildRect();
        SetVisible(false);
    }

    void OnValidate()
    {
        if (Application.isPlaying && lr != null) BuildRect();
    }

    void EnsureLR()
    {
        lr = GetComponent<LineRenderer>();
        if (!lr) lr = gameObject.AddComponent<LineRenderer>();

        lr.loop = true;
        lr.positionCount = 4;
        lr.useWorldSpace = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.widthMultiplier = lineWidth;

        if (lineMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Unlit/Color");
            lineMaterial = new Material(shader) { enableInstancing = true };
        }
        lr.material = lineMaterial;

        SetColor(Color.white);
    }

    void BuildRect()
    {
        float hx = fieldSize.x * 0.5f + padding;
        float hz = fieldSize.y * 0.5f + padding;

        Vector3 p0 = new(-hx, yOffset, -hz);
        Vector3 p1 = new(-hx, yOffset,  hz);
        Vector3 p2 = new( hx, yOffset,  hz);
        Vector3 p3 = new( hx, yOffset, -hz);

        lr.SetPosition(0, p0);
        lr.SetPosition(1, p1);
        lr.SetPosition(2, p2);
        lr.SetPosition(3, p3);
        lr.widthMultiplier = lineWidth;
    }

    void SetColor(Color c)
    {
        Color hdr = c * glowIntensity; // HDR-Farbe -> Bloom
        lr.startColor = hdr;
        lr.endColor = hdr;

        if (lr.material != null)
        {
            if (lr.material.HasProperty("_BaseColor")) lr.material.SetColor("_BaseColor", hdr);
            if (lr.material.HasProperty("_Color"))     lr.material.SetColor("_Color", hdr);
            if (lr.material.HasProperty("_EmissionColor"))
            {
                lr.material.EnableKeyword("_EMISSION");
                lr.material.SetColor("_EmissionColor", hdr);
            }
        }
    }

    void SetVisible(bool v) => lr.enabled = v;

    public void SetOwned(Color ownerColor, bool owned)
    {
        if (!owned) { SetVisible(false); return; }
        SetColor(ownerColor);
        SetVisible(true);
    }
}
