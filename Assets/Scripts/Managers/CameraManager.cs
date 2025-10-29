using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
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
    
    public void NextSide()
    {
        if (isMoving) return;
        currentIndex = (currentIndex + 1) % cameraPositions.Count;
        StartCoroutine(MoveToPosition(cameraPositions[currentIndex]));
    }

    public void PrevSide()
    {
        if (isMoving) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = cameraPositions.Count - 1;
        StartCoroutine(MoveToPosition(cameraPositions[currentIndex]));
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
        if (camBrain.ActiveVirtualCamera == cam)
        {
            topCam.Priority = 20;
            cam.Priority = 10;
            leftBtn.SetActive(true);
            rightBtn.SetActive(true);
        }
        else
        {
            topCam.Priority = 10;
            cam.Priority = 20;
            leftBtn.SetActive(false);
            rightBtn.SetActive(false);
        }
    }
}