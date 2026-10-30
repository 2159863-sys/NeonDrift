using UnityEngine;

// Lives on PlayerShip (next to PlayerHealth and PlayerScore).
// Menu "Neon Drift > Set Up Audio" adds it and fills the clip slots from Assets/Audio.
[DisallowMultipleComponent]
public class NeonDriftAudio : MonoBehaviour
{
    [Header("Sound clips (Assets/Audio)")]
    public AudioClip backgroundMusic;
    public AudioClip asteroidCrash;
    public AudioClip energyPickup;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float effectsVolume = 0.75f;

    private AudioSource musicSource;
    private AudioSource effectsSource;
    private PlayerHealth health;
    private PlayerScore score;

    void Awake()
    {
        // Two separate audio channels so effects play over the music without stopping it.
        musicSource = CreateSource(true);
        effectsSource = CreateSource(false);

        health = GetComponent<PlayerHealth>();
        score = GetComponent<PlayerScore>();
    }

    AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D: same volume anywhere in the tunnel
        return source;
    }

    void OnEnable()
    {
        // Events fire on the exact hit / pickup, so a crash is still heard
        // when a full energy bar instantly restores the lost life.
        if (health != null) health.LifeLost += PlayCrash;
        if (score != null) score.EnergyCollected += PlayPickup;
    }

    void OnDisable()
    {
        if (health != null) health.LifeLost -= PlayCrash;
        if (score != null) score.EnergyCollected -= PlayPickup;
    }

    void Start()
    {
        if (health == null) Debug.LogWarning("NEON DRIFT: NeonDriftAudio needs PlayerHealth on the same object.");
        if (score == null) Debug.LogWarning("NEON DRIFT: NeonDriftAudio needs PlayerScore on the same object.");

        ApplyVolumes();
        musicSource.clip = backgroundMusic;
        if (backgroundMusic != null) musicSource.Play();
        else Debug.LogWarning("NEON DRIFT: Please assign Background Music.");
    }

    void Update()
    {
        // Lets the Inspector sliders be tweaked live during Play mode.
        ApplyVolumes();
    }

    void ApplyVolumes()
    {
        musicSource.volume = musicVolume;
        effectsSource.volume = effectsVolume;
    }

    void PlayCrash()
    {
        if (asteroidCrash != null) effectsSource.PlayOneShot(asteroidCrash);
        else Debug.LogWarning("NEON DRIFT: Please assign Asteroid Crash.");
    }

    void PlayPickup()
    {
        if (energyPickup != null) effectsSource.PlayOneShot(energyPickup);
        else Debug.LogWarning("NEON DRIFT: Please assign Energy Pickup.");
    }

#if UNITY_EDITOR
    // Fills empty slots automatically when the component is added in the editor.
    void Reset()
    {
        backgroundMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/NeonDriftMusic.wav");
        asteroidCrash = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AsteroidCrash.wav");
        energyPickup = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/EnergyPickup.wav");
    }
#endif
}
