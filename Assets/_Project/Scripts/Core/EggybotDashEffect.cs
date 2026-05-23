using Unity.Cinemachine;
using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT ダッシュ仕上げ 3 つ (Day 5、Apex / Doom 2016 / モンハン defacto)。
    ///
    /// FOV Punch: dash 開始で Lens.FieldOfView を 60 → 75 に attack 80ms / decay 250ms
    /// Camera Dip: dash 開始で CinemachineImpulseSource で Y -0.5 velocity impulse
    /// Speed Lines: dash 中 ParticleSystem を Play (画面 overlay 風)
    ///
    /// Cinemachine Lens は struct なので書戻し必須。
    /// 既存 Day 3 Camera 系 (Roll/Noise/Look-Ahead) は別 field で非干渉。
    /// 視覚値 (FOV / 時間 / 強度) は SerializeField で User Play test 調整。
    /// </summary>
    [DefaultExecutionOrder(60)] // PlayerController(50) 後
    public sealed class EggybotDashEffect : MonoBehaviour
    {
        [Header("Cinemachine")]
        [Tooltip("FOV 制御対象の CinemachineCamera。null で FOV 効果無効。")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;

        [Tooltip("Camera Dip 用 Impulse Source。null で Dip 無効。Camera 側に CinemachineImpulseListener 必要。")]
        [SerializeField] private CinemachineImpulseSource _dipImpulseSource;

        [Header("FOV Punch (Apex Legends defacto)")]
        [Tooltip("通常 FOV (度)。60 推奨 (Cinemachine default)。")]
        [Range(40f, 90f)]
        [SerializeField] private float _baseFov = 60f;

        [Tooltip("Dash 中 peak FOV (度)。+15° の 75 推奨、控えめなら 68。")]
        [Range(40f, 110f)]
        [SerializeField] private float _dashFov = 75f;

        [Tooltip("FOV attack 時間 (秒)。0.08 推奨 (Apex defacto、fast in)。")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _fovAttack = 0.08f;

        [Tooltip("FOV decay 時間 (秒、dash 終了後 base に戻る)。0.25 推奨 (slow out)。")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _fovDecay = 0.25f;

        [Header("Camera Dip (TPS standard)")]
        [Tooltip("Impulse velocity (m/s)。Y 負値で下方向 dip。(0, -0.5, 0) 推奨。")]
        [SerializeField] private Vector3 _dipImpulseVelocity = new Vector3(0f, -0.5f, 0f);

        [Header("Speed Lines (モンハン / Apex defacto)")]
        [Tooltip("Dash 中 speed lines ParticleSystem。null で skip。dash 開始で Play / 終了で Stop。")]
        [SerializeField] private ParticleSystem _speedLines;

        float _currentFov;
        float _targetFov;
        bool _dashing;

        void Awake()
        {
            // scene 個別配線忘れ防止: 未配線なら scene 内 CinemachineCamera を自動取得
            // (CDP は 1 cam 想定、Match/MainMenu でも同 pattern で動作)
            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
            }
            _currentFov = _baseFov;
            _targetFov = _baseFov;
        }

        public void OnDashStart()
        {
            // enabled=false でも外部から呼ばれる経路があるため、guard 必須
            // (User 「画面が一瞬動く」の真因: enabled=false でも Impulse / FOV punch が発火してた)
            if (!enabled) return;

            _dashing = true;
            _targetFov = _dashFov;
            if (_dipImpulseSource != null) _dipImpulseSource.GenerateImpulseWithVelocity(_dipImpulseVelocity);
            if (_speedLines != null && !_speedLines.isPlaying) _speedLines.Play();
        }

        public void OnDashEnd()
        {
            if (!enabled) return;

            _dashing = false;
            _targetFov = _baseFov;
            if (_speedLines != null && _speedLines.isPlaying) _speedLines.Stop();
        }

        void Update()
        {
            if (_cinemachineCamera == null) return;

            float duration = _dashing ? _fovAttack : _fovDecay;
            if (duration > 0f)
            {
                // 固定速度ベース (baseFov ↔ dashFov の振幅で 1 duration かけて全幅移動するレート)
                float step = Mathf.Abs(_dashFov - _baseFov) / duration * Time.deltaTime;
                _currentFov = Mathf.MoveTowards(_currentFov, _targetFov, step);
            }
            else
            {
                _currentFov = _targetFov;
            }

            var lens = _cinemachineCamera.Lens;
            lens.FieldOfView = _currentFov;
            _cinemachineCamera.Lens = lens;
        }
    }
}
