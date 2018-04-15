using UnityEngine;

public class DetectiveAudioController : MonoBehaviour
{
    public GameController GameController;
    private Animator Animator;

    private void Start()
    {
        if (Animator == null)
        {
            Animator = GameController.GetCurrentDetectiveAnimator();
        }
    }

    private void Update()
    {
        if (Animator == null)
        {
            Animator = GameController.GetCurrentDetectiveAnimator();
        }
    }

    public void PlaySoundEffect(AudioClip clip)
    {
        var audioSource = GameController.GetDetectiveSoundEffectAudioSource();
        audioSource.clip = clip;
        audioSource.Play();
    }
}