using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT 移動 VFX 3 種 (Day 4、Mario / Splatoon / Crash Bandicoot defacto)。
    ///
    /// Landing Dust: 着地で足元砂埃、fallSpeed に応じて size/count スケール
    /// Footstep: 歩行距離累積 0.6m ごとに L/R 交互で puff (ダッシュ中は 0.4m)
    /// Dash Trail: dash 中の背中 trail (state change で on/off)
    ///
    /// Particle System (Shuriken) ベース、Shared Material で SRP Batcher 対応想定。
    /// 全 SerializeField は nullable、Inspector 未配線でも null check で safe (placeholder OK)。
    /// 視覚値 (色 / size / count) は User Play test で調整。
    /// </summary>
    [DefaultExecutionOrder(60)] // PlayerController(50) 後
    public sealed class EggybotMovementVFX : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("着地 / dash state 取得用 PlayerController。null で無効。")]
        [SerializeField] private PlayerController _player;

        [Header("Landing Dust")]
        [Tooltip("着地砂埃 ParticleSystem。null で skip。")]
        [SerializeField] private ParticleSystem _landingDust;

        [Tooltip("着地強度しきい値 (m/s)。これ未満の落下速度では skip (歩行中の段差着地で連発防止)。")]
        [SerializeField] private float _landingMinFallSpeed = 0.5f;

        [Tooltip("着地最大 fallSpeed (m/s)、burst count を 0..1 で normalize する分母。")]
        [SerializeField] private float _landingMaxFallSpeed = 12f;

        [Tooltip("burst count 最小値 (fallSpeed=min 時)。")]
        [SerializeField] private int _landingBurstMin = 4;

        [Tooltip("burst count 最大値 (fallSpeed=max 時)。")]
        [SerializeField] private int _landingBurstMax = 16;

        [Header("Footstep Puff")]
        [Tooltip("左足 puff ParticleSystem。null で skip。")]
        [SerializeField] private ParticleSystem _footstepL;

        [Tooltip("右足 puff ParticleSystem。null で skip。")]
        [SerializeField] private ParticleSystem _footstepR;

        [Tooltip("通常歩行時の footstep 間隔 (m)。0.6 推奨 (Yoshi 系)。")]
        [SerializeField] private float _footstepIntervalWalk = 0.6f;

        [Tooltip("ダッシュ中の footstep 間隔 (m)。0.4 推奨 (歩幅短く密集)。")]
        [SerializeField] private float _footstepIntervalDash = 0.4f;

        [Header("Dash Trail")]
        [Tooltip("ダッシュ中 trail ParticleSystem。null で skip。dash 開始で Play / 終了で Stop。")]
        [SerializeField] private ParticleSystem _dashTrail;

        float _footstepAccum;
        bool _leftStepNext = true; // 次に発動する足 (L/R 交互)

        void Awake()
        {
            if (_player == null) _player = GetComponent<PlayerController>();
        }

        public void PlayLanding(float fallSpeed)
        {
            if (_landingDust == null) return;
            if (fallSpeed < _landingMinFallSpeed) return;
            float k = Mathf.InverseLerp(_landingMinFallSpeed, _landingMaxFallSpeed, fallSpeed);
            int count = (int)Mathf.Lerp(_landingBurstMin, _landingBurstMax, k);
            _landingDust.Emit(count);
        }

        public void OnDashStateChanged(bool dashing)
        {
            if (_dashTrail == null) return;
            if (dashing) { if (!_dashTrail.isPlaying) _dashTrail.Play(); }
            else { if (_dashTrail.isPlaying) _dashTrail.Stop(); }
        }

        void Update()
        {
            if (_player == null) return;
            if (!_player.IsGrounded) { _footstepAccum = 0f; return; }

            float speed = _player.CurrentPlanarSpeed;
            if (speed < 0.1f) { _footstepAccum = 0f; return; }

            _footstepAccum += speed * Time.deltaTime;
            float interval = _player.IsDashing ? _footstepIntervalDash : _footstepIntervalWalk;
            if (_footstepAccum >= interval)
            {
                _footstepAccum = 0f;
                var ps = _leftStepNext ? _footstepL : _footstepR;
                _leftStepNext = !_leftStepNext;
                if (ps != null) ps.Play();
            }
        }
    }
}
