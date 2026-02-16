using UnityEngine;
using UnityEngine.Audio;

public class OptionsScript : MonoBehaviour
{   

    public AudioMixer audioMixer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("Volume", volume);
    }

}
