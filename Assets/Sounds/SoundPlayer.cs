using UnityEngine;

public class SoundPlayer : MonoBehaviour
{

    public AudioSource audioSource;

    public AudioClip drawClip;
    public AudioClip confirmClip;
    public AudioClip clickClip;
    public AudioClip selectClip;
    public AudioClip eraseClip;

    public void playDraw()
    {
        if (!audioSource.isPlaying)
            audioSource.PlayOneShot(drawClip);
    }
    public void playConfirm()
    {
        audioSource.PlayOneShot(confirmClip);
    }
    public void playClick()
    {
        audioSource.PlayOneShot(clickClip);
    }

    public void playSelect()
    {

        audioSource.PlayOneShot(selectClip);
    }

    public void playErase()
    {
        audioSource.PlayOneShot(eraseClip);
    }
}
