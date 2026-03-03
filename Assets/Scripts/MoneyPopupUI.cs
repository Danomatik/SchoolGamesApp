using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MoneyPopupUI : MonoBehaviour
{
    public static MoneyPopupUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI iconText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Image panelBackground;

    [Header("Colors")]
    [SerializeField] private Color gainColor  = new Color(0.18f, 0.80f, 0.44f); // green
    [SerializeField] private Color lossColor  = new Color(0.91f, 0.30f, 0.24f); // red
    [SerializeField] private Color neutralColor = new Color(0.20f, 0.60f, 1.00f); // blue

    [Header("Timing")]
    [SerializeField] private float displayDuration = 1.8f;
    [SerializeField] private float fadeInDuration  = 0.20f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Animation")]
    [SerializeField] private float punchScale = 1.18f; // overshoot on entry

    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Coroutine activeRoutine;

    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        canvasGroup = popupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = popupPanel.AddComponent<CanvasGroup>();

        panelRect = popupPanel.GetComponent<RectTransform>();

        popupPanel.SetActive(false);
    }

    // ================================================================
    // PUBLIC API
    // ================================================================

    /// <summary>Simple gain: one player receives money (no sender).</summary>
    /// <param name="receiverName">Name of the player who gains money.</param>
    /// <param name="amount">Positive amount in €.</param>
    public void ShowGain(string receiverName, int amount)
    {
        ShowPopup(
            icon:     "📈",
            title:    $"{receiverName} erhält",
            amount:   amount,
            subtitle: "",
            isGain:   true
        );
    }

    /// <summary>Simple loss: one player pays money (no recipient shown).</summary>
    public void ShowLoss(string payerName, int amount)
    {
        ShowPopup(
            icon:     "💸",
            title:    $"{payerName} bezahlt",
            amount:   amount,
            subtitle: "an die Bank",
            isGain:   false
        );
    }

    /// <summary>Rent / transfer between two players.</summary>
    /// <param name="payerName">Player paying the rent.</param>
    /// <param name="receiverName">Player receiving the rent.</param>
    /// <param name="amount">Positive amount in €.</param>
    /// <param name="showForPayer">
    ///   true  → show from the payer's perspective (red, "– X€")
    ///   false → show from the receiver's perspective (green, "+ X€")
    /// </param>
    public void ShowRent(string payerName, string receiverName, int amount, bool showForPayer = true)
    {
        if (showForPayer)
        {
            ShowPopup(
                icon:     "🏠",
                title:    $"{payerName} zahlt Miete",
                amount:   amount,
                subtitle: $"an {receiverName}",
                isGain:   false
            );
        }
        else
        {
            ShowPopup(
                icon:     "🏠",
                title:    $"{receiverName} erhält Miete",
                amount:   amount,
                subtitle: $"von {payerName}",
                isGain:   true
            );
        }
    }

    /// <summary>Player eliminated — special red popup.</summary>
    public void ShowElimination(string playerName)
    {
        ShowPopup(
            icon:     "💀",
            title:    $"{playerName}",
            amount:   -1, // suppressed
            subtitle: "ist insolvent!",
            isGain:   false,
            suppressAmount: true
        );
    }

    // ================================================================
    // INTERNAL
    // ================================================================

    private void ShowPopup(
        string icon,
        string title,
        int    amount,
        string subtitle,
        bool   isGain,
        bool   suppressAmount = false)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PopupRoutine(icon, title, amount, subtitle, isGain, suppressAmount));
    }

    private IEnumerator PopupRoutine(
        string icon,
        string title,
        int    amount,
        string subtitle,
        bool   isGain,
        bool   suppressAmount)
    {
        // --- Configure text & color ---
        Color accent = isGain ? gainColor : (suppressAmount ? lossColor : lossColor);
        if (suppressAmount) accent = lossColor;

        if (iconText   != null) iconText.text   = icon;
        if (titleText  != null) titleText.text   = title;
        if (subtitleText != null) subtitleText.text = subtitle;

        if (amountText != null)
        {
            if (suppressAmount)
            {
                amountText.text = "";
            }
            else
            {
                string sign   = isGain ? "+" : "–";
                amountText.text  = $"{sign}{amount:N0}€";
                amountText.color = accent;
            }
        }

        // Tint the panel background
        if (panelBackground != null)
        {
            Color bg = accent;
            bg.a = 0.92f;
            panelBackground.color = bg;
        }

        // --- Show & fade in with punch scale ---
        popupPanel.SetActive(true);
        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * 0.6f;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float p = t / fadeInDuration;
            canvasGroup.alpha = Mathf.Clamp01(p);

            // Overshoot spring: ease-out then small bounce
            float scale = Mathf.Lerp(0.6f, punchScale, EaseOutBack(p));
            panelRect.localScale = Vector3.one * scale;
            yield return null;
        }

        // Settle to 1.0
        t = 0f;
        float settleTime = 0.10f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float p = t / settleTime;
            float scale = Mathf.Lerp(punchScale, 1f, p);
            panelRect.localScale = Vector3.one * scale;
            yield return null;
        }
        panelRect.localScale = Vector3.one;
        canvasGroup.alpha = 1f;

        // --- Hold ---
        yield return new WaitForSeconds(displayDuration);

        // --- Fade out ---
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - t / fadeOutDuration);
            yield return null;
        }

        popupPanel.SetActive(false);
        activeRoutine = null;
    }

    // Smooth overshoot easing
    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}