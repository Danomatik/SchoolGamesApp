using UnityEngine;

public class DiceFocus : MonoBehaviour
{
    public Transform dice1;
    public Transform dice2;

    // Update is called once per frame
    void Update()
    {
        transform.position = (dice1.position + dice2.position) / 2f;
    }
}
