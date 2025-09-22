using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player playerInstance;
    void Awake() {
        playerInstance = this;
    }
}
