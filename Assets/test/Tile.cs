using UnityEngine;
using DG.Tweening;
public class Tile : MonoBehaviour
{
   void OnMouseDown() {
    Player.playerInstance.transform.DOMove(transform.position, 0.5f).SetEase(Ease.InOutQuad);
   }
}
