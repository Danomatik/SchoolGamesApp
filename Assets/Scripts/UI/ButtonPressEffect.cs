using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float animationSpeed = 10f;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isPointerDown = false;
    
    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }
    
    void Update()
    {
        // Smoothly animate to target scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        targetScale = originalScale * pressedScale;
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        targetScale = originalScale;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPointerDown)
        {
            targetScale = originalScale;
        }
    }
}
