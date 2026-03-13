using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    private static GameAudioController instance;

    [Header("Volumes")]
    [SerializeField] private float musicVolume = 0.24f;
    [SerializeField] private float shootVolume = 0.6f;
    [SerializeField] private float waveStartVolume = 0.72f;
    [SerializeField] private float enemyAttackVolume = 0.55f;
    [SerializeField] private float resultVolume = 0.8f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource enemySource;
    private AudioSource uiSource;

    private AudioClip musicClip;
    private AudioClip shootClip;
    private AudioClip waveStartClip;
    private AudioClip enemyAttackClip;
    private AudioClip victoryClip;
    private AudioClip defeatClip;

    private bool initialized;

    public static GameAudioController Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<GameAudioController>();
            if (instance != null)
            {
                return instance;
            }

            var go = new GameObject("GameAudioController");
            instance = go.AddComponent<GameAudioController>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeIfNeeded();
    }

    public void PlayShoot()
    {
        InitializeIfNeeded();
        if (sfxSource == null || shootClip == null)
        {
            return;
        }

        sfxSource.pitch = Random.Range(0.95f, 1.08f);
        sfxSource.PlayOneShot(shootClip, shootVolume);
    }

    public void PlayWaveStart(int waveNumber)
    {
        InitializeIfNeeded();
        if (uiSource == null || waveStartClip == null)
        {
            return;
        }

        var pitch = 0.95f + Mathf.Clamp(waveNumber, 1, 5) * 0.08f;
        uiSource.pitch = pitch;
        uiSource.PlayOneShot(waveStartClip, waveStartVolume);
    }

    public void PlayEnemyAttack()
    {
        InitializeIfNeeded();
        if (enemySource == null || enemyAttackClip == null)
        {
            return;
        }

        enemySource.pitch = Random.Range(0.9f, 1.12f);
        enemySource.PlayOneShot(enemyAttackClip, enemyAttackVolume);
    }

    public void PlayVictory()
    {
        InitializeIfNeeded();
        if (uiSource == null || victoryClip == null)
        {
            return;
        }

        if (musicSource != null)
        {
            musicSource.volume = musicVolume * 0.45f;
        }

        uiSource.pitch = 1f;
        uiSource.PlayOneShot(victoryClip, resultVolume);
    }

    public void PlayDefeat()
    {
        InitializeIfNeeded();
        if (uiSource == null || defeatClip == null)
        {
            return;
        }

        if (musicSource != null)
        {
            musicSource.volume = musicVolume * 0.35f;
        }

        uiSource.pitch = 1f;
        uiSource.PlayOneShot(defeatClip, resultVolume);
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        SetupAudioSources();
        CreateGeneratedClips();
        StartBackgroundMusic();
    }

    private void SetupAudioSources()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 1f;

        enemySource = gameObject.AddComponent<AudioSource>();
        enemySource.playOnAwake = false;
        enemySource.loop = false;
        enemySource.spatialBlend = 0f;
        enemySource.volume = 1f;

        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.loop = false;
        uiSource.spatialBlend = 0f;
        uiSource.volume = 1f;
    }

    private void CreateGeneratedClips()
    {
        shootClip = CreateShootClip();
        waveStartClip = CreateWaveStartClip();
        enemyAttackClip = CreateEnemyAttackClip();
        musicClip = CreateBackgroundMusicClip();
        victoryClip = CreateVictoryClip();
        defeatClip = CreateDefeatClip();
    }

    private void StartBackgroundMusic()
    {
        if (musicSource == null || musicClip == null)
        {
            return;
        }

        if (musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = musicClip;
        musicSource.Play();
    }

    private static AudioClip CreateShootClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.16f;
        var sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[sampleCount];

        var phase = 0f;
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleCount;
            var frequency = Mathf.Lerp(1450f, 280f, t);
            phase += 2f * Mathf.PI * frequency / sampleRate;
            var envelope = (1f - t) * (1f - t);
            var noise = (Mathf.PerlinNoise(i * 0.21f, 0.19f) * 2f - 1f) * 0.2f;
            data[i] = (Mathf.Sin(phase) * 0.75f + noise) * envelope * 0.35f;
        }

        return CreateClip("SFX_Shoot_Procedural", data, sampleRate);
    }

    private static AudioClip CreateWaveStartClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.25f;
        var sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[sampleCount];

        // Epic wave-start stab: rising low tone + bright layer + short kick pulse.
        var lowPhase = 0f;
        var highPhase = 0f;
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleCount;
            var lowFreq = Mathf.Lerp(80f, 140f, t);
            var highFreq = Mathf.Lerp(320f, 520f, t);
            lowPhase += 2f * Mathf.PI * lowFreq / sampleRate;
            highPhase += 2f * Mathf.PI * highFreq / sampleRate;

            var swell = Mathf.Sin(t * Mathf.PI);
            var low = Mathf.Sin(lowPhase) * 0.45f;
            var high = Mathf.Sin(highPhase) * 0.25f;

            var kickEnv = Mathf.Exp(-26f * t);
            var kick = Mathf.Sin(2f * Mathf.PI * 55f * t) * kickEnv * 0.55f;

            var noise = (Mathf.PerlinNoise(i * 0.018f, 0.6f) * 2f - 1f) * 0.08f * kickEnv;
            data[i] = (low + high) * swell * 0.34f + kick + noise;
        }

        return CreateClip("SFX_WaveStart_Procedural", data, sampleRate);
    }

    private static AudioClip CreateEnemyAttackClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.18f;
        var sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[sampleCount];

        var phase = 0f;
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleCount;
            var frequency = Mathf.Lerp(520f, 180f, t);
            phase += 2f * Mathf.PI * frequency / sampleRate;
            var envelope = Mathf.Exp(-10f * t);
            var buzz = Mathf.Sign(Mathf.Sin(phase * 1.8f)) * 0.35f;
            data[i] = (Mathf.Sin(phase) * 0.6f + buzz) * envelope * 0.3f;
        }

        return CreateClip("SFX_EnemyAttack_Procedural", data, sampleRate);
    }

    private static AudioClip CreateBackgroundMusicClip()
    {
        const int sampleRate = 44100;
        const float duration = 16f;
        var sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[sampleCount];

        var chordRoots = new[] { 130.8128f, 195.9977f, 220.0f, 174.6141f }; // C3 G3 A3 F3
        var chordThirds = new[] { 164.8138f, 246.9417f, 261.6256f, 220.0f }; // E3 B3 C4 A3
        var chordFifths = new[] { 195.9977f, 293.6648f, 329.6276f, 261.6256f }; // G3 D4 E4 C4

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var norm = i / (float)sampleCount;

            var bar = Mathf.FloorToInt(t / 4f);
            var chordIndex = bar % chordRoots.Length;

            var root = chordRoots[chordIndex];
            var third = chordThirds[chordIndex];
            var fifth = chordFifths[chordIndex];

            var pad =
                Mathf.Sin(2f * Mathf.PI * root * t) * 0.23f
                + Mathf.Sin(2f * Mathf.PI * third * t + 0.2f) * 0.17f
                + Mathf.Sin(2f * Mathf.PI * fifth * t + 0.41f) * 0.13f;

            // 8th-note pluck pattern for a more "normal game loop" feel.
            var beat8 = Mathf.FloorToInt(t * 2f);
            var stepInBar = beat8 % 8;
            float noteFreq;
            switch (stepInBar)
            {
                case 0:
                    noteFreq = root * 2f;
                    break;
                case 1:
                    noteFreq = third * 2f;
                    break;
                case 2:
                    noteFreq = fifth * 2f;
                    break;
                case 3:
                    noteFreq = third * 2f;
                    break;
                case 4:
                    noteFreq = root * 2f;
                    break;
                case 5:
                    noteFreq = fifth * 2f;
                    break;
                case 6:
                    noteFreq = third * 2f;
                    break;
                default:
                    noteFreq = fifth * 2f;
                    break;
            }

            var localStep = (t * 2f) - Mathf.Floor(t * 2f);
            var pluckEnv = Mathf.Exp(-7f * localStep);
            var pluck = Mathf.Sin(2f * Mathf.PI * noteFreq * t) * pluckEnv * 0.12f;

            var shimmer = Mathf.Sin(2f * Mathf.PI * (noteFreq * 1.5f) * t + 0.5f) * pluckEnv * 0.04f;
            var air = (Mathf.PerlinNoise(t * 0.12f, 0.33f) * 2f - 1f) * 0.016f;
            var slowLfo = 0.78f + 0.22f * Mathf.Sin(2f * Mathf.PI * 0.08f * t);
            var edgeFade = Mathf.Clamp01(norm / 0.08f) * Mathf.Clamp01((1f - norm) / 0.08f);

            data[i] = (pad + pluck + shimmer + air) * slowLfo * edgeFade * 0.26f;
        }

        return CreateClip("BGM_Procedural_SpringLoop", data, sampleRate);
    }

    private static AudioClip CreateVictoryClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.2f;
        var sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[sampleCount];
        var notes = new[] { 261.6256f, 329.6276f, 391.9954f, 523.2511f }; // C E G C

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var idx = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / 0.27f));
            var local = t - idx * 0.27f;
            var env = Mathf.Exp(-4.2f * local);
            var tone = Mathf.Sin(2f * Mathf.PI * notes[idx] * t);
            var bell = Mathf.Sin(2f * Mathf.PI * notes[idx] * 2f * t + 0.2f) * 0.35f;
            data[i] = (tone + bell) * env * 0.3f;
        }

        return CreateClip("SFX_Victory_Procedural", data, sampleRate);
    }

    private static AudioClip CreateDefeatClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.1f;
        var sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var freq = Mathf.Lerp(280f, 90f, Mathf.Clamp01(t / duration));
            var tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            var sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t + 0.1f) * 0.4f;
            var noise = (Mathf.PerlinNoise(i * 0.015f, 0.8f) * 2f - 1f) * 0.12f;
            var env = Mathf.Exp(-2.8f * t);
            data[i] = (tone + sub + noise) * env * 0.3f;
        }

        return CreateClip("SFX_Defeat_Procedural", data, sampleRate);
    }

    private static AudioClip CreateClip(string clipName, float[] data, int sampleRate)
    {
        var clip = AudioClip.Create(clipName, data.Length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
