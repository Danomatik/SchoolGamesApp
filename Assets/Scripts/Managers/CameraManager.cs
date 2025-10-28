using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using DG.Tweening; // make sure DOTween is installed and imported

public class CameraManager : MonoBehaviour
{

    [Header("Camera")]
    public CinemachineCamera cam;

    public CinemachineBrain camBrain;
    public float defaultLens = 5f;
}