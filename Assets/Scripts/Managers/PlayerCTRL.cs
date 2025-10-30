using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // make sure DOTween is installed and imported

public class PlayerCTRL : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerMovement playerMovement;
    public int PlayerID;
    public Route route;
    public int currentPos = 0;

    [Header("Movement Settings")]
    public float moveDurationPerTile = 0.6f;      // time to move one tile
    public float rotationDuration = 0.25f;        // rotation smoothing time
    public float tileRadius = 0.4f;               // spacing when multiple players share a tile
    public Ease moveEase = Ease.InOutSine;        // easing type for DOTween motion

    private static Dictionary<int, List<PlayerCTRL>> playersOnTile = new();
    private bool isMoving = false;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>(); // optional, for walk animations
    }

    public void StartMove(int stepsToTake)
    {
        if (!isMoving)
            StartCoroutine(MoveStepByStep(stepsToTake));
    }

    private IEnumerator MoveStepByStep(int stepsToTake)
    {
        isMoving = true;

        if (anim) anim.SetBool("IsWalking", true);

        while (stepsToTake > 0)
        {
            // Leave old tile
            if (playersOnTile.ContainsKey(currentPos))
                playersOnTile[currentPos].Remove(this);

            // Advance to next tile
            currentPos = (currentPos + 1) % route.childNodeList.Count;

            // Register to new tile
            if (!playersOnTile.ContainsKey(currentPos))
                playersOnTile[currentPos] = new List<PlayerCTRL>();
            playersOnTile[currentPos].Add(this);

            // Target position and direction
            Vector3 endPos = GetTargetPositionForTile(currentPos);
            Vector3 dir = (endPos - transform.position).normalized;
            dir.y = 0;

            // Rotate smoothly before moving
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.DORotateQuaternion(Quaternion.LookRotation(dir), rotationDuration)
                         .SetEase(Ease.OutQuad);
            }

            // Move smoothly using DOTween
            Tween moveTween = transform.DOMove(endPos, moveDurationPerTile)
                .SetEase(moveEase)
                .SetSpeedBased(false);

            yield return moveTween.WaitForCompletion();

            stepsToTake--;

            // Small pause per step for readability
            yield return new WaitForSeconds(0.05f);
        }

        // Face tile center when done
        FaceTileCenter();

        if (anim) anim.SetBool("IsWalking", false);

        isMoving = false;
        playerMovement.PlayerFinishedMoving(currentPos);
    }

    private Vector3 GetTargetPositionForTile(int tileIndex)
    {
        Vector3 center = route.childNodeList[tileIndex].position;

        if (!playersOnTile.ContainsKey(tileIndex))
            return center;

        List<PlayerCTRL> players = playersOnTile[tileIndex];
        int count = players.Count;

        if (count == 1)
            return center;

        int index = players.IndexOf(this);
        if (index < 0) index = 0;

        // Evenly distribute players around tile
        float angle = (index / (float)count) * Mathf.PI * 2f;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * tileRadius;
        return center + offset;
    }

    private void FaceTileCenter()
    {
        Vector3 center = route.childNodeList[currentPos].position;
        Vector3 dir = (center - transform.position).normalized;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.DORotateQuaternion(Quaternion.LookRotation(dir), 0.3f)
                     .SetEase(Ease.OutSine);
        }
    }
}
