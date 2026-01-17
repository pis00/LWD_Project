using UnityEngine;

public class PlaySoundOnTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;
    [SerializeField] private bool playOnce = false;

    private bool _hasPlayed;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnce && _hasPlayed) return;
        if (!other.CompareTag(playerTag)) return;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) return;

        if (clip != null)
            audioSource.PlayOneShot(clip);
        else
            audioSource.Play();

        _hasPlayed = true;
    }
}