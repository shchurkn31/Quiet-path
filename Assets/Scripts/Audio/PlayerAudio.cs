
using UnityEngine;
using System.Collections;

public class PlayerAudio : MonoBehaviour
{
    [Header("Компоненти")]
    public AudioSource footstepSource;
    public AudioSource bowSource;
    public PlayerController player;

    [Header("Звук ходьби")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.4f;

    [Header("Звук лука")]
    public AudioClip bowShotSound;
    public float bowShotDuration = 1f;

    private float footstepTimer;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (player == null || footstepSource == null)
            return;

        if (player.IsMoving)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
            return;

        AudioClip clip = footstepSounds[
            Random.Range(0, footstepSounds.Length)
        ];

        if (clip != null)
            footstepSource.PlayOneShot(clip);
    }

    public void PlayBowShot()
    {
        if (bowSource == null || bowShotSound == null)
            return;

        StartCoroutine(PlayBowSound());
    }

    private IEnumerator PlayBowSound()
    {
        bowSource.Stop();
        bowSource.clip = bowShotSound;
        bowSource.time = 0f;
        bowSource.loop = false;
        bowSource.Play();

        yield return new WaitForSecondsRealtime(
            Mathf.Clamp(bowShotDuration, 0f, bowShotSound.length)
        );

        bowSource.Stop();
    }
}
