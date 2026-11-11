using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource PlayerAudio;
    public Vector2 pitchRange = new Vector2(0.98f, 1.1f);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void PlaySound(AudioClip clip) {     
        float randomPitch = Random.Range(pitchRange.x, pitchRange.y);
        PlayerAudio.pitch = randomPitch;
        PlayerAudio.PlayOneShot(clip);
    }
    
}
