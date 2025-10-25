using UnityEngine;
using Unity.Cinemachine;

public class SmoothOrthoZoom : MonoBehaviour
{
    public CinemachineCamera vcam;
    public float smoothSpeed = 5f;
    public float targetOrthoSize = 5f;

    private float currentOrthoSize;

    void Start()
    {
        if (vcam == null)
            vcam = GetComponent<CinemachineCamera>();

        currentOrthoSize = vcam.Lens.OrthographicSize;
    }

    void Update()
    {
        currentOrthoSize = Mathf.Lerp(currentOrthoSize, targetOrthoSize, Time.deltaTime * smoothSpeed);
        vcam.Lens.OrthographicSize = currentOrthoSize;
    }

    // Optional helper to trigger smooth zoom
    public void SetTargetSize(float newSize)
    {
        targetOrthoSize = newSize;
    }
}
