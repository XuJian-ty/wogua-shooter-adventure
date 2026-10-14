using UnityEngine;

namespace WoguaShooter.Core
{
    /// <summary>
    /// 游戏的基础音频管理器。没有配置音频资源时也可以正常运行。
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        private const string MusicVolumeKey = "WoguaShooter.MusicVolume";
        private const string SfxVolumeKey = "WoguaShooter.SfxVolume";

        public static AudioManager Instance { get; private set; }

        [Header("音频源")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip defaultMusic;
        [SerializeField] private AudioClip peaShotSfx;
        [SerializeField] private AudioClip cornShotSfx;
        [SerializeField] private AudioClip enemyHitSfx;
        [SerializeField] private AudioClip cornExplosionSfx;

        [Header("音量")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeInstance()
        {
            if (Instance != null)
                return;

            GameObject root = new GameObject("AudioManager");
            root.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            LoadRuntimeResources();
            LoadVolumes();

            if (defaultMusic != null)
                PlayMusic(defaultMusic);
        }

        private void LoadRuntimeResources()
        {
            // Keep the manager usable in a clean scene while allowing generated
            // audio to ship without editing every scene or prefab reference.
            if (defaultMusic == null)
                defaultMusic = Resources.Load<AudioClip>("Audio/WoguaAdventure_BGM");
            if (peaShotSfx == null)
                peaShotSfx = Resources.Load<AudioClip>("Audio/SFX_PeaShot");
            if (cornShotSfx == null)
                cornShotSfx = Resources.Load<AudioClip>("Audio/SFX_CornShot");
            if (enemyHitSfx == null)
                enemyHitSfx = Resources.Load<AudioClip>("Audio/SFX_EnemyHit");
            if (cornExplosionSfx == null)
                cornExplosionSfx = Resources.Load<AudioClip>("Audio/SFX_CornExplosion");
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void LoadVolumes()
        {
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null)
                musicSource.volume = musicVolume;
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        /// <summary>设置背景音乐音量并保存到本机。</summary>
        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        /// <summary>设置游戏音效音量并保存到本机。</summary>
        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        // BGM 是常见缩写，为设置面板保留这个易读的别名。
        public void SetBgmVolume(float value)
        {
            SetMusicVolume(value);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null)
                return;

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource?.Stop();
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (sfxSource == null || clip == null || sfxVolume <= 0f)
                return;

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void PlayWeaponShot(bool cornCannon)
        {
            PlaySfx(cornCannon ? cornShotSfx : peaShotSfx, cornCannon ? 0.8f : 0.65f);
        }

        public void PlayEnemyHit()
        {
            PlaySfx(enemyHitSfx, 0.85f);
        }

        public void PlayCornExplosion()
        {
            PlaySfx(cornExplosionSfx, 0.9f);
        }
    }
}
