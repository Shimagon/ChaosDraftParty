using ChaosDraftParty.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// Recovery launcher for Shrinking Island. It kicks players back toward the center
    /// arena instead of only bouncing vertically. AAA-grade feedback: 3-stage squash,
    /// emissive idle breathing, ring flash, Cinemachine impulse.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BouncePad : MonoBehaviour
    {
        // ---- Launch -----------------------------------------------------------------
        [Header("Launch")]
        [Tooltip("打ち上げ高さ (m)。中央島誘導の起点となる高度。AAA defacto: 6m 前後")]
        [SerializeField] private float _launchHeight = 6.0f;

        [Tooltip("打ち上げ時間 (秒)。短すぎるとワープ感、長いとモッタリ。0.55s 推奨")]
        [SerializeField] private float _launchDuration = 0.55f;

        [Tooltip("中央島誘導強度。0=純鉛直、1=中央集約。AAA: 0.8 で穏やかな magnetism")]
        [Range(0f, 1f)]
        [SerializeField] private float _targetXZMagnetism = 0.8f;

        [Tooltip("打ち上げ目標 Transform (assigned なら磁性源、null 時は world origin)")]
        [SerializeField] private Transform _launchTarget;

        [Tooltip("fallback: 目標未指定なら world origin へ磁性")]
        [SerializeField] private bool _fallbackLaunchTowardWorldOrigin = true;

        [Tooltip("fallback: world origin もなければ local forward へ")]
        [SerializeField] private Vector3 _fallbackLocalDirection = Vector3.forward;

        [Tooltip("使用回数: true なら 1 回で collider 無効化")]
        [SerializeField] private bool _singleUse = false;

        [Tooltip("単一でない場合の再使用クールダウン (秒)")]
        [SerializeField] private float _reuseCooldown = 1.5f;

        // ---- Legacy launch tuning (PlayerController.ApplyBounce が要求する速度) -----
        [Tooltip("(legacy) 上方向初速。PlayerController.ApplyBounce へ渡す upward speed")]
        [SerializeField] private float _upwardSpeed = 10.5f;

        [Tooltip("(legacy) 水平方向初速。中央誘導に使用")]
        [SerializeField] private float _horizontalSpeed = 7.5f;

        // ---- Squash (3 段、AAA defacto Mario/Splatoon 帯) ----------------------------
        [Header("Squash (3 段、AAA defacto Mario/Splatoon 帯)")]
        [Tooltip("Anticipation 沈み込み duration (秒)。AAA: 0.05-0.08s。Y 1→0.65 / XZ 1→1.30")]
        [SerializeField] private float _anticipationDuration = 0.07f;

        [FormerlySerializedAs("_launchPunchDuration")]
        [Tooltip("Punch 解放 duration (秒)。AAA: 0.10-0.15s。Y 0.65→1.18 / XZ 1.30→0.90")]
        [SerializeField] private float _punchDuration = 0.12f;

        [Tooltip("Settle (バネ自然減衰) duration (秒)。AAA: 0.30-0.45s")]
        [SerializeField] private float _settleDuration = 0.38f;

        [Tooltip("Anticipation: Y scale 最小値")]
        [Range(0.3f, 1.0f)]
        [SerializeField] private float _anticipationScaleY = 0.65f;

        [Tooltip("Anticipation: XZ scale 最大値 (横膨張)")]
        [Range(1.0f, 2.0f)]
        [SerializeField] private float _anticipationScaleXZ = 1.30f;

        [FormerlySerializedAs("_launchPunchScale")]
        [Tooltip("Punch: Y scale 最大値 (縦伸び)")]
        [Range(1.0f, 2.0f)]
        [SerializeField] private float _punchScaleY = 1.18f;

        [Tooltip("Punch: XZ scale 最小値 (横圧縮)")]
        [Range(0.5f, 1.0f)]
        [SerializeField] private float _punchScaleXZ = 0.90f;

        // ---- Idle Breathing ---------------------------------------------------------
        [Header("Idle Breathing")]
        [Tooltip("Idle emissive 呼吸周波数 (Hz)。AAA: 0.6-1.0Hz。Splatoon ジャンプビーコン defacto")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float _idleFrequency = 0.8f;

        [Tooltip("Idle emissive 振幅 (base ± %)。AAA: 0.2-0.3")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _idleEmissiveAmplitude = 0.25f;

        [Tooltip("(legacy) Ready 時の scale 微振幅 (visual root に掛かる呼吸 scale)")]
        [SerializeField] private float _readyPulseAmplitude = 0.035f;

        [Tooltip("(legacy) Ready 時の scale 微振幅 speed (rad/s)")]
        [SerializeField] private float _readyPulseSpeed = 3.5f;

        [Tooltip("Idle 時の emissive base color (この色 × emissiveScale で MaterialPropertyBlock に set)")]
        [SerializeField] private Color _readyEmissionColor = new Color(0.4f, 1.0f, 0.85f, 1f);

        // ---- Visual References ------------------------------------------------------
        [Header("Visual References")]
        [Tooltip("Pad 本体 MeshRenderer (cooldown 色変化 + idle pulse 対象)")]
        [SerializeField] private MeshRenderer _visualRenderer;

        [Tooltip("クールダウン中の色 (grey 風)")]
        [SerializeField] private Color _cooldownColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Tooltip("Squash anim 対象 Transform")]
        [SerializeField] private Transform _visualRoot;

        // ---- Ring VFX ---------------------------------------------------------------
        [Header("Ring VFX")]
        [Tooltip("Ring 表示 duration (秒)。AAA: 120-180ms。150ms 推奨")]
        [SerializeField] private float _ringDuration = 0.15f;

        [Tooltip("Ring 開始サイズ")]
        [SerializeField] private float _ringScaleStart = 0.3f;

        [Tooltip("Ring 終了サイズ")]
        [SerializeField] private float _ringScaleEnd = 2.5f;

        [Tooltip("Ring MeshRenderer (新 SI_BouncePadRing shader 適用)")]
        [SerializeField] private MeshRenderer _ringMesh;

        // ---- Particle VFX -----------------------------------------------------------
        [Header("Particle VFX")]
        [Tooltip("Streak (cyan/mint 中心方向ストリーク) PS")]
        [SerializeField] private ParticleSystem _streakParticle;

        [Tooltip("Spark (放射状火花) PS (sub-emitter 1 段)")]
        [SerializeField] private ParticleSystem _sparkParticle;

        [Tooltip("Idle Glow PS (looping)")]
        [SerializeField] private ParticleSystem _idleGlowParticle;

        [Tooltip("(後方互換) 既存 _launchParticle")]
        [SerializeField] private ParticleSystem _launchParticle;

        [Tooltip("(後方互換) 既存 _launchParticlePrefab、リーク対策で pool 化推奨")]
        [SerializeField] private GameObject _launchParticlePrefab;

        // ---- Cinemachine Impulse ----------------------------------------------------
        [Header("Cinemachine Impulse")]
        [Tooltip("Impulse Source (Cinemachine 直参照、SendMessage 廃止)")]
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        [Tooltip("Impulse 振幅 (m)。AAA defacto: 酔わない sweet spot 0.18-0.20")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _impulseAmplitude = 0.18f;

        [Tooltip("Impulse duration (秒)。AAA: 0.15-0.35s")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _impulseDuration = 0.35f;

        // ---- Runtime state ----------------------------------------------------------
        private bool _used;
        private float _cooldownRemaining;
        private float _squashElapsed = -1f;  // -1: idle (no squash playing)
        private float _ringElapsed = -1f;
        private Collider _collider;
        private Rigidbody _rigidbody;
        private MaterialPropertyBlock _propertyBlock;
        private MaterialPropertyBlock _ringPropertyBlock;
        private Vector3 _visualBaseScale = Vector3.one;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        private void Awake()
        {
            EnsureInitialized();
        }

        public void ResetPad()
        {
            EnsureInitialized();
            _used = false;
            _cooldownRemaining = 0f;
            _squashElapsed = -1f;
            _ringElapsed = -1f;
            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (_visualRenderer != null)
            {
                if (_propertyBlock == null)
                {
                    _propertyBlock = new MaterialPropertyBlock();
                }
                _propertyBlock.Clear();
                _visualRenderer.SetPropertyBlock(_propertyBlock);
            }
            if (_ringMesh != null)
            {
                _ringMesh.enabled = false;
            }
            if (_visualRoot != null)
            {
                _visualRoot.localScale = _visualBaseScale;
            }
        }

        private void EnsureInitialized()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;

            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponentInChildren<MeshRenderer>();
            }
            if (_visualRoot == null && _visualRenderer != null)
            {
                _visualRoot = _visualRenderer.transform;
            }
            if (_visualRoot != null)
            {
                _visualBaseScale = _visualRoot.localScale;
            }

            _propertyBlock = new MaterialPropertyBlock();
            _ringPropertyBlock = new MaterialPropertyBlock();

            if (_ringMesh != null)
            {
                _ringMesh.enabled = false;
            }
        }

        private void Update()
        {
            UpdateReadyPulse();
            UpdateSquashState();
            UpdateRingFlash();

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= Time.deltaTime;
                if (_cooldownRemaining <= 0f && !_used)
                {
                    _cooldownRemaining = 0f;
                    ClearVisualOverride();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryBounce(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryBounce(other);
        }

        private void TryBounce(Collider other)
        {
            if (_used)
            {
                return;
            }
            if (_cooldownRemaining > 0f)
            {
                return;
            }

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            player.ApplyBounce(GetPlanarLaunchVelocity(), _upwardSpeed);
            PlayLaunchFeedback();
            TriggerSquash();
            StartRingFlash();

            if (_singleUse)
            {
                _used = true;
                if (_collider != null)
                {
                    _collider.enabled = false;
                }
            }
            else
            {
                _cooldownRemaining = Mathf.Max(0.05f, _reuseCooldown);
            }
            ApplyCooldownColor();
        }

        private void PlayLaunchFeedback()
        {
            if (_launchParticle != null)
            {
                _launchParticle.Play(true);
            }

            if (_streakParticle != null)
            {
                _streakParticle.Play(true);
            }

            if (_sparkParticle != null)
            {
                _sparkParticle.Play(true);
            }

            if (_launchParticlePrefab != null)
            {
                GameObject instance = Instantiate(
                    _launchParticlePrefab,
                    transform.position,
                    transform.rotation,
                    transform.parent);
                ParticleSystem ps = instance.GetComponent<ParticleSystem>();
                float lifetime = 2f;
                if (ps != null)
                {
                    ParticleSystem.MainModule main = ps.main;
                    lifetime = main.duration + main.startLifetime.constantMax;
                }
                Destroy(instance, lifetime);
            }

            if (_impulseSource != null && _impulseAmplitude > 0f)
            {
                _impulseSource.GenerateImpulseWithVelocity(Vector3.up * _impulseAmplitude);
            }
        }

        // ---- Squash 3-stage ---------------------------------------------------------
        private void TriggerSquash()
        {
            _squashElapsed = 0f;
        }

        private void UpdateSquashState()
        {
            if (_squashElapsed < 0f || _visualRoot == null)
            {
                return;
            }

            _squashElapsed += Time.deltaTime;

            float yMul;
            float xzMul;

            if (_squashElapsed < _anticipationDuration)
            {
                // Phase 1: Anticipation (沈み込み)
                float t = _squashElapsed / Mathf.Max(0.001f, _anticipationDuration);
                float ease = EaseInQuad(t);
                yMul = Mathf.Lerp(1f, _anticipationScaleY, ease);
                xzMul = Mathf.Lerp(1f, _anticipationScaleXZ, ease);
            }
            else if (_squashElapsed < _anticipationDuration + _punchDuration)
            {
                // Phase 2: Punch (解放、縦伸び)
                float t = (_squashElapsed - _anticipationDuration) / Mathf.Max(0.001f, _punchDuration);
                float ease = EaseOutBack(t);
                yMul = Mathf.Lerp(_anticipationScaleY, _punchScaleY, ease);
                xzMul = Mathf.Lerp(_anticipationScaleXZ, _punchScaleXZ, ease);
            }
            else if (_squashElapsed < _anticipationDuration + _punchDuration + _settleDuration)
            {
                // Phase 3: Settle (バネ自然減衰)
                float t = (_squashElapsed - _anticipationDuration - _punchDuration) /
                          Mathf.Max(0.001f, _settleDuration);
                float ease = EaseOutElastic(t);
                yMul = Mathf.Lerp(_punchScaleY, 1f, ease);
                xzMul = Mathf.Lerp(_punchScaleXZ, 1f, ease);
            }
            else
            {
                // Done
                _squashElapsed = -1f;
                _visualRoot.localScale = _visualBaseScale;
                return;
            }

            Vector3 scale = _visualBaseScale;
            scale.x *= xzMul;
            scale.z *= xzMul;
            scale.y *= yMul;
            _visualRoot.localScale = scale;
        }

        private static float EaseInQuad(float t)
        {
            return t * t;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseOutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            if (t <= 0f)
            {
                return 0f;
            }
            if (t >= 1f)
            {
                return 1f;
            }
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        // ---- Ring VFX ---------------------------------------------------------------
        private void StartRingFlash()
        {
            _ringElapsed = 0f;
        }

        private void UpdateRingFlash()
        {
            if (_ringElapsed < 0f || _ringMesh == null)
            {
                return;
            }

            _ringElapsed += Time.deltaTime;
            if (_ringElapsed > _ringDuration)
            {
                _ringMesh.enabled = false;
                _ringElapsed = -1f;
                return;
            }

            _ringMesh.enabled = true;
            float t = _ringElapsed / Mathf.Max(0.001f, _ringDuration);
            float scale = Mathf.Lerp(_ringScaleStart, _ringScaleEnd, t);
            _ringMesh.transform.localScale = new Vector3(scale, scale, scale);

            if (_ringPropertyBlock == null)
            {
                _ringPropertyBlock = new MaterialPropertyBlock();
            }
            _ringMesh.GetPropertyBlock(_ringPropertyBlock);
            _ringPropertyBlock.SetFloat(ProgressId, t);
            _ringMesh.SetPropertyBlock(_ringPropertyBlock);
        }

        // ---- Cooldown color override ------------------------------------------------
        private void ApplyCooldownColor()
        {
            if (_visualRenderer == null)
            {
                return;
            }
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            // F5 fix: 前 frame の EmissionColor 等の override を排除し、
            // cooldown grey と idle emissive の混在表示を防ぐ。
            _propertyBlock.Clear();

            Material material = _visualRenderer.sharedMaterial;
            if (material != null && material.HasProperty(BaseColorId))
            {
                _propertyBlock.SetColor(BaseColorId, _cooldownColor);
            }
            else
            {
                _propertyBlock.SetColor(ColorId, _cooldownColor);
            }
            _visualRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void ClearVisualOverride()
        {
            if (_visualRenderer == null)
            {
                return;
            }
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
            // F5 fix: SetPropertyBlock(null) と Clear() + SetPropertyBlock(_propertyBlock)
            // パターンを統一。empty PB を assign することで前 frame の override を確実に消す。
            _propertyBlock.Clear();
            _visualRenderer.SetPropertyBlock(_propertyBlock);
        }

        // ---- Idle breathing (emissive pulse + 微小 scale 呼吸) ---------------------
        private void UpdateReadyPulse()
        {
            // Idle 中だけ scale 微振幅と emissive pulse を行う。
            // squash 再生中は UpdateSquashState() が scale を上書きするので衝突しない。
            bool isIdle = _cooldownRemaining <= 0f && !_used && _squashElapsed < 0f;

            if (_visualRoot != null && isIdle)
            {
                float pulse = Mathf.Sin(Time.time * _readyPulseSpeed) * _readyPulseAmplitude;
                _visualRoot.localScale = _visualBaseScale * (1f + pulse);
            }

            // Emissive 呼吸: idle 中のみ pulse、cooldown 中は base emission を控えめに
            if (_visualRenderer == null)
            {
                return;
            }
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            if (isIdle)
            {
                float emissivePhase = Mathf.Sin(Time.time * _idleFrequency * 2f * Mathf.PI);
                float emissiveScale = 1f + emissivePhase * _idleEmissiveAmplitude;
                _visualRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(EmissionColorId, _readyEmissionColor * emissiveScale);
                _visualRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        // ---- Launch direction -------------------------------------------------------
        private Vector3 GetPlanarLaunchVelocity()
        {
            Vector3 direction;
            if (_launchTarget != null)
            {
                direction = _launchTarget.position - transform.position;
            }
            else if (_fallbackLaunchTowardWorldOrigin)
            {
                direction = -transform.position;
            }
            else
            {
                direction = transform.TransformDirection(_fallbackLocalDirection);
            }

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
                direction.y = 0f;
            }

            // 中央誘導強度 (_targetXZMagnetism) で純鉛直 (0) ↔ 中央集約 (1) を補間
            float magnetism = Mathf.Clamp01(_targetXZMagnetism);
            return direction.normalized * Mathf.Max(0f, _horizontalSpeed) * magnetism;
        }
    }
}
