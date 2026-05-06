using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace ChaosDraftParty.Core.Audio
{
    /// <summary>
    /// BGM の global シングルトン。crossfade ループ + stinger one-shot 対応。
    ///
    /// 使い方:
    /// 1. Boot.unity に空 GameObject + 本 component を attach
    /// 2. AudioMixerGroup に MainMixer/BGM を assign
    /// 3. lobbyBGM / matchBGM に Pixabay (CC0 商用 OK) の loop 用 mp3 を assign
    /// 4. scene 切替で `BGMManager.Instance.PlayLobby()` / `PlayMatch()` を呼ぶ
    ///
    /// 設計:
    /// - 2 つの AudioSource を交互に使って crossfade (ぶつ切り防止)
    /// - DontDestroyOnLoad で scene 跨ぎ生存
    /// - Stinger は active source に PlayOneShot (BGM の上に重ねる)
    /// </summary>
    public class BGMManager : MonoBehaviour
    {
        public static BGMManager Instance { get; private set; }

        [Header("Output")]
        [Tooltip("AudioMixerGroup (MainMixer/BGM) を assign。null でも動くが volume 制御不可。")]
        [SerializeField] AudioMixerGroup bgmMixerGroup;

        [Header("BGM Clips")]
        [Tooltip("ロビー画面用 (Pixabay CC0 推奨)。loop=true で再生。")]
        [SerializeField] AudioClip lobbyBGM;

        [Tooltip("試合中 BGM。Pixabay funky / energetic 系推奨。")]
        [SerializeField] AudioClip matchBGM;

        [Header("Settings")]
        [Tooltip("曲切替時の crossfade 時間 (秒)。0.5 推奨。")]
        [SerializeField] float crossFadeSec = 0.5f;

        [Tooltip("デフォルト volume (0..1)。Mixer 側でも調整可。")]
        [Range(0f, 1f)]
        [SerializeField] float defaultVolume = 0.7f;

        AudioSource sourceA;
        AudioSource sourceB;
        AudioSource active;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            sourceA = CreateSource("BGM_A");
            sourceB = CreateSource("BGM_B");
            active = sourceA;
        }

        AudioSource CreateSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.volume = 0f;
            if (bgmMixerGroup != null) s.outputAudioMixerGroup = bgmMixerGroup;
            return s;
        }

        public void PlayLobby() => PlayLoop(lobbyBGM);
        public void PlayMatch() => PlayLoop(matchBGM);

        public void PlayLoop(AudioClip clip)
        {
            if (clip == null) return;
            if (active != null && active.clip == clip && active.isPlaying) return;
            var next = (active == sourceA) ? sourceB : sourceA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            StartCoroutine(CrossFadeRoutine(active, next, crossFadeSec));
            active = next;
        }

        public void Stop(float fadeSec = 0.5f)
        {
            if (active != null && active.isPlaying) StartCoroutine(FadeOutRoutine(active, fadeSec));
        }

        public void PlayStinger(AudioClip clip)
        {
            if (clip == null || active == null) return;
            active.PlayOneShot(clip);
        }

        IEnumerator CrossFadeRoutine(AudioSource from, AudioSource to, float dur)
        {
            float t = 0f;
            float fromStart = (from != null) ? from.volume : 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                if (from != null) from.volume = fromStart * (1f - k);
                if (to != null) to.volume = defaultVolume * k;
                yield return null;
            }
            if (from != null && from != to) from.Stop();
        }

        IEnumerator FadeOutRoutine(AudioSource src, float dur)
        {
            float t = 0f;
            float start = src.volume;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                src.volume = start * (1f - Mathf.Clamp01(t / dur));
                yield return null;
            }
            src.Stop();
        }
    }
}
