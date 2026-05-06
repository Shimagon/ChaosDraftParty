using UnityEngine;
using UnityEngine.Audio;

namespace ChaosDraftParty.Core.Audio
{
    /// <summary>
    /// UI 用 SE の global シングルトン (Splatoon / Fall Guys defacto = hover/select/confirm/cancel/error 5 種)。
    ///
    /// 使い方:
    /// 1. Boot.unity に空 GameObject + 本 component を attach
    /// 2. AudioMixerGroup に MainMixer/UI を assign
    /// 3. 5 つの clip slot に Kenney UI Audio (CC0) を割当
    /// 4. UI ボタン OnClick から `UIAudioPlayer.Instance.PlayConfirm()` を呼ぶ
    ///
    /// 特徴:
    /// - DontDestroyOnLoad で scene 跨ぎ生存
    /// - pitchVariance で連打感 (人間味)
    /// - spatialBlend=0 (2D) で常に一定音量
    /// </summary>
    public class UIAudioPlayer : MonoBehaviour
    {
        public static UIAudioPlayer Instance { get; private set; }

        [Header("Output")]
        [Tooltip("AudioMixerGroup (MainMixer/UI) を assign。null でも動くが volume 制御不可。")]
        [SerializeField] AudioMixerGroup uiMixerGroup;

        [Header("UI SE Clips (Kenney UI Audio CC0 推奨)")]
        [SerializeField] AudioClip hoverClip;
        [SerializeField] AudioClip selectClip;
        [SerializeField] AudioClip confirmClip;
        [SerializeField] AudioClip cancelClip;
        [SerializeField] AudioClip errorClip;

        [Header("Settings")]
        [Tooltip("pitch のランダム ±幅 (0.05 = ±5%)。連打感の人間味用。")]
        [Range(0f, 0.2f)]
        [SerializeField] float pitchVariance = 0.05f;

        [Tooltip("デフォルト volume (0..1)。Mixer 側でも調整可。")]
        [Range(0f, 1f)]
        [SerializeField] float defaultVolume = 1f;

        AudioSource source;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = defaultVolume;
            if (uiMixerGroup != null) source.outputAudioMixerGroup = uiMixerGroup;
        }

        public void PlayHover()   => Play(hoverClip);
        public void PlaySelect()  => Play(selectClip);
        public void PlayConfirm() => Play(confirmClip);
        public void PlayCancel()  => Play(cancelClip);
        public void PlayError()   => Play(errorClip);

        public void PlayClip(AudioClip clip) => Play(clip);

        void Play(AudioClip clip)
        {
            if (clip == null || source == null) return;
            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.PlayOneShot(clip);
        }
    }
}
