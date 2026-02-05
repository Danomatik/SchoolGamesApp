using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // make sure DOTween is installed and imported

public class CameraManager : MonoBehaviour
{

    [Header("Camera")]

    public GameManager gm;
    public CinemachineCamera cam;

    public CinemachineBrain camBrain;
    public float defaultLens = 5f;

    [SerializeField] private CinemachineCamera topCam;
    [SerializeField] private List<Transform> cameraPositions;
    [SerializeField] private float moveSpeed = 3f;

    private int currentIndex = 0;
    private bool isMoving = false;

    public GameObject leftBtn;
    public GameObject rightBtn;
    public GameObject moveBtn;
    
    private GameObject zurueckBtn;
    private GameObject vorBtn;
    
    private void Start()
    {
        // Finde Buttons anhand ihrer Namen
        FindButtonsByName();
        
        // Prüfe initial, ob topCam aktiv ist und verstecke moveBtn entsprechend
        UpdateMoveButtonVisibility();
    }
    
    /// <summary>
    /// Findet die Buttons anhand ihrer Namen im Unity Editor
    /// </summary>
    private void FindButtonsByName()
    {
        // Suche nach ZurueckBtn
        if (leftBtn != null && leftBtn.name.Contains("Zurueck"))
        {
            zurueckBtn = leftBtn;
        }
        else if (rightBtn != null && rightBtn.name.Contains("Zurueck"))
        {
            zurueckBtn = rightBtn;
        }
        else
        {
            // Fallback: Suche in der Szene
            GameObject found = GameObject.Find("ZurueckBtn");
            if (found != null) zurueckBtn = found;
        }
        
        // Suche nach VorBtn
        if (leftBtn != null && leftBtn.name.Contains("Vor"))
        {
            vorBtn = leftBtn;
        }
        else if (rightBtn != null && rightBtn.name.Contains("Vor"))
        {
            vorBtn = rightBtn;
        }
        else
        {
            // Fallback: Suche in der Szene
            GameObject found = GameObject.Find("VorBtn");
            if (found != null) vorBtn = found;
        }
        
        // Fallback: Wenn nicht gefunden, verwende leftBtn/rightBtn
        if (zurueckBtn == null) zurueckBtn = leftBtn;
        if (vorBtn == null) vorBtn = rightBtn;
    }
    
    /// <summary>
    /// Prüft ob topCam aktiv ist und versteckt/zeigt moveBtn entsprechend
    /// </summary>
    private void UpdateMoveButtonVisibility()
    {
        if (moveBtn == null) return;
        
        bool isTopCamActive = IsTopCameraActive();
        if (moveBtn.activeSelf != !isTopCamActive)
        {
            moveBtn.SetActive(!isTopCamActive);
        }
    }
    
    /// <summary>
    /// Prüft ob die Top-Kamera aktiv ist
    /// </summary>
    private bool IsTopCameraActive()
    {
        if (camBrain == null || topCam == null) return false;
        return (UnityEngine.Object)camBrain.ActiveVirtualCamera == (UnityEngine.Object)topCam;
    }
    
    
    public void NextSide()
    {
        if (isMoving) return;
        currentIndex = (currentIndex + 1) % cameraPositions.Count;
        StartCoroutine(MoveToPosition(cameraPositions[currentIndex]));
        // ✅ Stelle sicher, dass moveBtn versteckt bleibt im Top-View
        UpdateMoveButtonVisibility();
    }

    public void PrevSide()
    {
        if (isMoving) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = cameraPositions.Count - 1;
        StartCoroutine(MoveToPosition(cameraPositions[currentIndex]));
        // ✅ Stelle sicher, dass moveBtn versteckt bleibt im Top-View
        UpdateMoveButtonVisibility();
    }

    private IEnumerator MoveToPosition(Transform target)
    {
        isMoving = true;

        Vector3 startPos = topCam.transform.position;
        Quaternion startRot = topCam.transform.rotation;

        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            topCam.transform.position = Vector3.Lerp(startPos, endPos, t);
            topCam.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        isMoving = false;
    }

    public void SetTopView()
    {
        if ((UnityEngine.Object)camBrain.ActiveVirtualCamera == (UnityEngine.Object)cam)
        {
            // Wechsle zu Top-View
            topCam.Priority = 20;
            cam.Priority = 10;
            if (zurueckBtn != null) zurueckBtn.SetActive(true);
            if (vorBtn != null) vorBtn.SetActive(true);
            moveBtn.SetActive(false); // ✅ Verstecke Würfel-Button im Top-View
        }
        else
        {
            // Wechsle zurück zu normaler Kamera
            topCam.Priority = 10;
            cam.Priority = 20;
            if (zurueckBtn != null) zurueckBtn.SetActive(false);
            if (vorBtn != null) vorBtn.SetActive(false);
            // moveBtn wird automatisch von UpdateMoveButtonVisibility() wieder angezeigt
        }
    }
}