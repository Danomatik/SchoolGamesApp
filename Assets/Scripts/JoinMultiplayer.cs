using UnityEngine;
using UnityEngine.SceneManagement;

public class JoinMultiplayer : MonoBehaviour
{
    public void JoinMultiplayerMode()
	{
		SceneManager.LoadScene("MainScene");
	}

	public void LoadMenu()
	{
		SceneManager.LoadScene("MenuScene");
	}
}
