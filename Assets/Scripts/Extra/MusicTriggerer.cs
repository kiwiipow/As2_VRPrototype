using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;

public class MusicTriggerer : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip defaultMusic;
    [SerializeField] private AudioClip playingMusic;

    [Header("Trigger collider")]
    [SerializeField] private BoxCollider triggerCollider;

    private bool musicChanged = false;

    void Start()
    {
        audioSource.clip = defaultMusic;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!musicChanged)
        {
            audioSource.Stop();
            audioSource.clip = playingMusic;
            audioSource.loop = true;
            audioSource.Play();
            musicChanged = true;
        }
    }
}
