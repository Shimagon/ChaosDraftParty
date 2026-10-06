using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// 縮む島の 1 マス。NetworkObject ではなく単純な MonoBehaviour。
    /// 状態同期は ShrinkingIslandManager (ClientRpc) か TileManager (NetworkList) のどちらでも駆動可能。
    ///
    /// 設計:
    /// - state は Healthy → Warning → Fading → Gone へ単方向遷移 (外部 API 互換維持)
    /// - 内部的に subPhase で WarningShake → Tilt → Sink → Dust の 5 phase を駆動 (AAA defacto)
    /// - 視覚は MaterialPropertyBlock 経由で _BaseColor / _Color / _WarningProgress / _DissolveProgress を補間
    ///   (instance 化せず batching を守る)
    /// - SE / Particle hook は SerializeField (null 許容)
    /// - Network 同期は不要 (manager が ClientRpc で StartFade をブロードキャスト)
    ///
    /// 後方互換:
    /// - 既存 TileManager (NetworkList&lt;byte&gt; 駆動) も SetState(byte) で呼び出し可能
    /// - 既存 StartFade(warningDuration, fadeDuration) API は維持
    ///   (引数 > 0 で Inspector default を override)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Tile : MonoBehaviour
    {
        public enum TileState
        {
            Healthy = 0,
            Warning = 1,
            Fading  = 2,
            Gone    = 3,
        }

        /// <summary>
        /// 内部細分化 phase。State == Warning 中で WarningShake → Tilt、
        /// State == Fading 中で Sink → Dust を駆動する。
        /// </summary>
        private enum SubPhase
        {
            None          = 0,
            WarningShake  = 1,
            Tilt          = 2,
            Sink          = 3,
            Dust          = 4,
        }

        [Header("Index")]
        [Tooltip("Grid 上の (x, z) index。ShrinkingIslandManager が起動時に set。")]
        [FormerlySerializedAs("tileIndex")]
        [SerializeField] private int _tileIndex = -1;

        [SerializeField] private int _gridX = -1;
        [SerializeField] private int _gridZ = -1;

        [Header("Visual")]
        [Tooltip("活性時の MeshRenderer (Healthy = enabled、Gone = disabled)。")]
        [FormerlySerializedAs("visualRenderer")]
        [SerializeField] private MeshRenderer _visualRenderer;

        [Tooltip("足場の Collider (踏める時 enabled、Gone で disabled)。")]
        [FormerlySerializedAs("standCollider")]
        [SerializeField] private Collider _standCollider;

        [Tooltip("Warning 中の点滅速度 (Hz)。視覚フィードバック用 (legacy、_warningTintLerp と併用)。")]
        [FormerlySerializedAs("fadingBlinkRate")]
        [SerializeField] private float _warningBlinkRate = 4f;

        [Tooltip("Warning 中の color PingPong 周期 (秒)。0.15 で赤白を高速 lerp。")]
        [SerializeField] private float _warningColorPingPongPeriod = 0.15f;

        [Tooltip("Warning 中の scale 振動 最小値 (1.0 → 最小)。1.0 で振動 OFF。")]
        [SerializeField] private float _warningScaleMin = 0.95f;
        [SerializeField] private Color _warningColor = new(1f, 0.42f, 0.18f, 1f);
        [SerializeField] private float _warningTintStrength = 0.48f;

        [Tooltip("Fading 中の沈下距離 (m)。AAA defacto: Fall Guys Hex-a-tile / Mario Falling Platform は 1.0m+。\"重さと諦め感\" を出す")]
        [SerializeField] private float _fadeDropDistance = 1.0f;

        [Tooltip("Fading 中の傾き (度)。AAA: 15-20°帯。これより小さいと \"板が静かに沈む\" 印象に留まる")]
        [SerializeField] private float _fadeTiltDegrees = 18f;

        [Header("Phase Durations (AAA 3 phase 1.25s 構造)")]
        [Tooltip("Warning phase: 警告点滅 (旧 warningDuration 引数 default、Inspector で override 可)")]
        [SerializeField] private float _warningPhaseDuration = 0.4f;

        [Tooltip("Tilt phase: Warning→Fading 間の急峻 tilt 段 (anticipation)。AAA: 0.15s")]
        [SerializeField] private float _tiltPhaseDuration = 0.15f;

        [Tooltip("Sink phase: 沈下メイン (旧 fadeDuration、Inspector で override 可)")]
        [SerializeField] private float _sinkPhaseDuration = 0.45f;

        [Tooltip("Dust phase: 沈下後の dust 残留 (visual tail)。SetActive(false) はこの phase 終了で実施")]
        [SerializeField] private float _dustPhaseDuration = 0.6f;

        [Header("Warning Visual (Phase 2 A2 defacto)")]
        [Tooltip("Base color → warning color への tint 強度 (AAA: 0.4、full red 置換禁止)")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _warningTintLerp = 0.4f;

        [Tooltip("Warning shake 振幅 (m、horizontal yoyo)。AAA: 0.02m")]
        [SerializeField] private float _warningShakeAmplitude = 0.02f;

        [Tooltip("Warning shake 周波数 (cycle/sec)")]
        [SerializeField] private float _warningShakeFrequency = 20f;

        [Tooltip("Warning edge emission 強度 (Bloom 連動)")]
        [SerializeField] private float _warningEdgeEmission = 2.5f;

        [Header("Tilt Phase Visual")]
        [Tooltip("Tilt 軸ランダム化 (±deg、X/Z 軸の randomization)")]
        [SerializeField] private float _tiltAxisRandomization = 30f;

        [Header("VFX/SFX (任意)")]
        [Tooltip("Warning 開始時に 1 回再生する SE (PlayClipAtPoint)。null OK。")]
        [SerializeField] private AudioClip _warningSfx;

        [Tooltip("Fading 開始時に 1 回再生する SE (PlayClipAtPoint)。null OK。")]
        [SerializeField] private AudioClip _fadeSfx;

        [Tooltip("Warning 中継続して表示する Particle prefab。Warning 開始時 1 回 Instantiate、Fading 突入時に Destroy。null OK。")]
        [SerializeField] private GameObject _warningParticlePrefab;

        [Tooltip("Fading 開始時に 1 回 Instantiate する Particle prefab (自動 destroy 想定)。null OK。")]
        [SerializeField] private GameObject _fadeParticlePrefab;

        [Header("Effects (legacy, optional)")]
        [Tooltip("旧 inspector 配線: Fading 突入時に Play する Particle (null OK)。後方互換のため残置。")]
        [SerializeField] private ParticleSystem _fadeParticle;

        [Tooltip("旧 inspector 配線: Fading 突入時に Play する SE (null OK)。後方互換のため残置。")]
        [SerializeField] private AudioSource _fadeSe;

        // Runtime state
        private TileState _state = TileState.Healthy;
        private float _stateTime;

        // 旧 API 互換用 (StartFade 引数で override 可)
        private float _warningDuration = 0.4f;
        private float _fadeDuration = 0.45f;

        // SubPhase 内部状態
        private SubPhase _subPhase = SubPhase.None;
        private float _subPhaseTime;
        private Vector3 _subPhaseRandomTiltAxis = Vector3.right;

        // 視覚 cache
        private Color _baseColor = Color.white;
        private MeshRenderer[] _visualRenderers;
        private Color[] _baseColors;
        private MaterialPropertyBlock _propBlock;
        private bool _initialized;
        private Vector3 _baseLocalScale = Vector3.one;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation = Quaternion.identity;

        // EnterState(Warning) で保存する \"その時点の\" local position / rotation
        // (shake / sink で書き換えるベース、_baseLocalPosition と別管理)
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation = Quaternion.identity;

        private GameObject _warningParticleInstance;

        // System.Random (UnityEngine.Random は規約禁止、tile 毎に独立 seed)
        private System.Random _random;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int WarningProgressId = Shader.PropertyToID("_WarningProgress");
        private static readonly int DissolveProgressId = Shader.PropertyToID("_DissolveProgress");
        private static readonly int EdgeEmissionMulId = Shader.PropertyToID("_EdgeEmissionMul");

        public int TileIndex => _tileIndex;
        public int GridX => _gridX;
        public int GridZ => _gridZ;
        public TileState State => _state;

        /// <summary>初期化。grid index を埋め、Healthy 状態に揃える。</summary>
        public void Initialize(int index, int gridX, int gridZ)
        {
            _tileIndex = index;
            _gridX = gridX;
            _gridZ = gridZ;
            EnsureInitialized();
            ResetTile();
        }

        /// <summary>後方互換: 旧 TileManager 用の (mgr, index) 形式。</summary>
        public void Initialize(TileManager manager, int index)
        {
            EnsureInitialized();
            _tileIndex = index;
            ResetTile();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            DestroyWarningParticle();
        }

        private void EnsureInitialized()
        {
            if (_initialized && _propBlock != null && _visualRenderers != null && _baseColors != null)
            {
                return;
            }
            _propBlock = new MaterialPropertyBlock();
            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponentInChildren<MeshRenderer>();
            }
            _visualRenderers = GetComponentsInChildren<MeshRenderer>(true);
            if (_visualRenderers == null || _visualRenderers.Length == 0)
            {
                _visualRenderers = _visualRenderer != null
                    ? new[] { _visualRenderer }
                    : Array.Empty<MeshRenderer>();
            }
            _baseColors = new Color[_visualRenderers.Length];
            if (_standCollider == null)
            {
                _standCollider = GetComponentInChildren<Collider>();
            }
            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                Material sharedMat = _visualRenderers[i] != null ? _visualRenderers[i].sharedMaterial : null;
                Color baseColor = Color.white;
                if (sharedMat != null)
                {
                    if (sharedMat.HasProperty(BaseColorId))
                    {
                        baseColor = sharedMat.GetColor(BaseColorId);
                    }
                    else if (sharedMat.HasProperty(ColorId))
                    {
                        baseColor = sharedMat.GetColor(ColorId);
                    }
                }
                _baseColors[i] = baseColor;
            }
            if (_baseColors.Length > 0)
            {
                _baseColor = _baseColors[0];
            }
            _baseLocalScale = transform.localScale;
            _baseLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;
            _initialLocalPosition = _baseLocalPosition;
            _initialLocalRotation = _baseLocalRotation;

            // tile 毎に独立した seed (index + grid から決定論的に生成)
            int seed = (_tileIndex * 73856093) ^ (_gridX * 19349663) ^ (_gridZ * 83492791);
            if (seed == 0) { seed = 1; }
            _random = new System.Random(seed);

            _initialized = true;
        }

        /// <summary>
        /// host から ClientRpc 経由で呼ばれる。Warning → (Tilt) → Fading → (Dust) → Gone へ自走。
        /// 引数 > 0 のとき Inspector default を override する。
        /// </summary>
        public void StartFade(float warningDuration = -1f, float fadeDuration = -1f)
        {
            EnsureInitialized();
            if (_state == TileState.Fading || _state == TileState.Gone)
            {
                return;
            }
            // 旧 API 互換: 引数があれば override、なければ Inspector default を使う
            _warningDuration = warningDuration > 0f ? warningDuration : _warningPhaseDuration;
            _fadeDuration = fadeDuration > 0f ? fadeDuration : _sinkPhaseDuration;
            EnterState(TileState.Warning);
        }

        /// <summary>ラウンド開始時に Healthy へ戻す。</summary>
        public void ResetTile()
        {
            EnsureInitialized();
            EnterState(TileState.Healthy);
        }

        /// <summary>後方互換: 既存 TileManager 経由の byte state を受ける。</summary>
        public void SetState(byte rawState)
        {
            // 旧 TileManager の規約: 0=Active, 1=Fading, 2=Gone
            switch (rawState)
            {
                case 0:
                    ResetTile();
                    break;
                case 1:
                    StartFade(_warningDuration, _fadeDuration);
                    break;
                case 2:
                    EnterState(TileState.Gone);
                    break;
            }
        }

        private void EnterState(TileState next)
        {
            _state = next;
            _stateTime = 0f;

            switch (next)
            {
                case TileState.Healthy:
                    DestroyWarningParticle();
                    _subPhase = SubPhase.None;
                    _subPhaseTime = 0f;
                    transform.localPosition = _baseLocalPosition;
                    transform.localRotation = _baseLocalRotation;
                    transform.localScale = _baseLocalScale;
                    _initialLocalPosition = _baseLocalPosition;
                    _initialLocalRotation = _baseLocalRotation;
                    SetVisualRenderersEnabled(true);
                    ApplyBaseColors();
                    ResetShaderProgress();
                    if (_standCollider != null)
                    {
                        _standCollider.enabled = true;
                    }
                    if (gameObject.activeSelf == false)
                    {
                        gameObject.SetActive(true);
                    }
                    break;

                case TileState.Warning:
                    // EnterState 時点の local pos/rot をベースとして保存
                    // (shake / tilt はここから offset で書き換える)
                    _initialLocalPosition = transform.localPosition;
                    _initialLocalRotation = transform.localRotation;
                    EnterSubPhase(SubPhase.WarningShake);
                    // 新規 SFX (1 shot)
                    if (_warningSfx != null)
                    {
                        AudioSource.PlayClipAtPoint(_warningSfx, transform.position);
                    }
                    // 新規 Particle (継続生成 → Fading で Destroy)
                    if (_warningParticlePrefab != null && _warningParticleInstance == null)
                    {
                        _warningParticleInstance = Instantiate(_warningParticlePrefab, transform.position, Quaternion.identity, transform);
                    }
                    // 後方互換: 旧 inspector 配線
                    if (_fadeParticle != null)
                    {
                        _fadeParticle.Play(true);
                    }
                    if (_fadeSe != null)
                    {
                        _fadeSe.Play();
                    }
                    break;

                case TileState.Fading:
                    if (_standCollider != null)
                    {
                        _standCollider.enabled = false;
                    }
                    // Warning particle を停止
                    DestroyWarningParticle();
                    // 新規 Fading SFX (1 shot)
                    if (_fadeSfx != null)
                    {
                        AudioSource.PlayClipAtPoint(_fadeSfx, transform.position);
                    }
                    // 新規 Fading Particle (1 shot Instantiate、自動 destroy 想定)
                    if (_fadeParticlePrefab != null)
                    {
                        var fadeInstance = Instantiate(_fadeParticlePrefab, transform.position, Quaternion.identity);
                        // F2 fix: prefab Stop Action 設定漏れに対する defensive destroy
                        // _dustPhaseDuration + 余裕 2 秒で確実に scene から消える
                        Destroy(fadeInstance, _dustPhaseDuration + 2f);
                    }
                    break;

                case TileState.Gone:
                    DestroyWarningParticle();
                    _subPhase = SubPhase.None;
                    _subPhaseTime = 0f;
                    SetVisualRenderersEnabled(false);
                    if (_standCollider != null)
                    {
                        _standCollider.enabled = false;
                    }
                    gameObject.SetActive(false);
                    break;
            }
        }

        private void EnterSubPhase(SubPhase sub)
        {
            _subPhase = sub;
            _subPhaseTime = 0f;

            if (sub == SubPhase.Tilt)
            {
                // ランダム tilt 軸生成 (X 軸ベースに ±_tiltAxisRandomization 度 yaw)
                double rnd = _random != null ? _random.NextDouble() : 0.5;
                float angle = ((float)rnd * 2f - 1f) * _tiltAxisRandomization;
                _subPhaseRandomTiltAxis = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right;
            }
        }

        private void DestroyWarningParticle()
        {
            if (_warningParticleInstance != null)
            {
                Destroy(_warningParticleInstance);
                _warningParticleInstance = null;
            }
        }

        private void Update()
        {
            if (_state == TileState.Healthy || _state == TileState.Gone)
            {
                return;
            }

            _stateTime += Time.deltaTime;
            _subPhaseTime += Time.deltaTime;

            switch (_state)
            {
                case TileState.Warning:
                    UpdateWarningState();
                    break;

                case TileState.Fading:
                    UpdateFadingState();
                    break;
            }
        }

        private void UpdateWarningState()
        {
            // visual: shake + tint + edge emission
            ApplyWarningVisualNew();

            if (_subPhase == SubPhase.WarningShake)
            {
                if (_subPhaseTime >= _warningPhaseDuration)
                {
                    // shake 終了、tilt 段へ
                    transform.localScale = _baseLocalScale;
                    EnterSubPhase(SubPhase.Tilt);
                }
                return;
            }

            if (_subPhase == SubPhase.Tilt)
            {
                if (_subPhaseTime >= _tiltPhaseDuration)
                {
                    // Warning → Fading transition
                    EnterState(TileState.Fading);
                    EnterSubPhase(SubPhase.Sink);
                }
                return;
            }

            // 旧 API 互換: StartFade(warningDuration, _) で warningDuration が極端に大きい場合の安全策
            if (_stateTime >= _warningDuration + _tiltPhaseDuration)
            {
                transform.localScale = _baseLocalScale;
                EnterState(TileState.Fading);
                EnterSubPhase(SubPhase.Sink);
            }
        }

        private void UpdateFadingState()
        {
            ApplyFadingVisualNew();

            if (_subPhase == SubPhase.Sink)
            {
                if (_subPhaseTime >= _sinkPhaseDuration)
                {
                    // sink 終了、collider は既に OFF、dust 段へ (SetActive(false) はまだしない)
                    EnterSubPhase(SubPhase.Dust);
                }
                return;
            }

            if (_subPhase == SubPhase.Dust)
            {
                if (_subPhaseTime >= _dustPhaseDuration)
                {
                    EnterState(TileState.Gone);
                }
                return;
            }

            // 旧 API 互換: subPhase が未設定で Fading に入ってきた場合
            if (_stateTime >= _fadeDuration + _dustPhaseDuration)
            {
                EnterState(TileState.Gone);
            }
        }

        /// <summary>
        /// Warning phase の visual: tint lerp (0.4 max) + edge emission pulse + horizontal shake。
        /// AAA defacto に基づく。
        /// </summary>
        private void ApplyWarningVisualNew()
        {
            // 4Hz pulse (legacy _warningBlinkRate も加味)
            float pulseFreq = _warningBlinkRate > 0f ? _warningBlinkRate : 4f;
            float tintT = Mathf.PingPong(_subPhaseTime * pulseFreq, 1f);

            // Tint lerp (full red 置換禁止、_warningTintLerp で上限)
            ApplyWarningTintAll(tintT * _warningTintLerp);

            // Edge emission via MaterialPropertyBlock (SI_TileWarning shader 連動)
            ApplyWarningProgress(tintT, _warningEdgeEmission);

            // Horizontal shake (WarningShake 段でのみ)
            if (_subPhase == SubPhase.WarningShake && _warningShakeAmplitude > 0f)
            {
                float shakePhase = Mathf.Sin(_subPhaseTime * _warningShakeFrequency * 2f * Mathf.PI);
                Vector3 shakeOffset = new Vector3(shakePhase * _warningShakeAmplitude, 0f, 0f);
                transform.localPosition = _initialLocalPosition + shakeOffset;

                // Scale 振動 (1.0 ↔ _warningScaleMin、shake と同期)
                if (_warningScaleMin < 1f)
                {
                    float scaleK = Mathf.PingPong(_subPhaseTime * pulseFreq, 1f);
                    float scale = Mathf.Lerp(1f, _warningScaleMin, scaleK);
                    transform.localScale = _baseLocalScale * scale;
                }
            }
            else if (_subPhase == SubPhase.Tilt)
            {
                // Tilt 段: shake 停止、急峻 tilt anticipation
                transform.localPosition = _initialLocalPosition;
                float tiltT = Mathf.Clamp01(_subPhaseTime / Mathf.Max(0.0001f, _tiltPhaseDuration));
                float easeT = tiltT * tiltT; // ease-in (急峻に向かう)
                float tiltAngle = _fadeTiltDegrees * 0.35f * easeT;
                transform.localRotation = _initialLocalRotation * Quaternion.AngleAxis(tiltAngle, _subPhaseRandomTiltAxis);
            }
        }

        /// <summary>
        /// Fading phase の visual: sink Y + tilt rotation + Dissolve shader progress。
        /// </summary>
        private void ApplyFadingVisualNew()
        {
            if (_subPhase == SubPhase.Sink)
            {
                float t = _sinkPhaseDuration > 0f ? Mathf.Clamp01(_subPhaseTime / _sinkPhaseDuration) : 1f;
                float easeT = 1f - Mathf.Pow(1f - t, 2f); // ease-out quad

                // Sink Y position
                Vector3 pos = _initialLocalPosition;
                pos.y -= _fadeDropDistance * easeT;
                transform.localPosition = pos;

                // Tilt rotation (急峻、subPhase 開始時に決まったランダム軸を使用)
                float tiltAngle = _fadeTiltDegrees * easeT;
                transform.localRotation = _initialLocalRotation * Quaternion.AngleAxis(tiltAngle, _subPhaseRandomTiltAxis);

                // Dissolve shader 連動 (SI_TileDissolve)
                ApplyDissolveProgress(easeT);

                // alpha も fading
                ApplyFadingColors(t);
            }
            else if (_subPhase == SubPhase.Dust)
            {
                // Dust 段階: tile 本体は alpha 0、視覚は dust prefab に任せる
                ApplyDissolveProgress(1f);
                ApplyFadingColors(1f);
            }
        }

        private void ApplyColor(Color c)
        {
            if (_visualRenderer == null)
            {
                return;
            }
            ApplyColor(_visualRenderer, c);
        }

        private void SetVisualRenderersEnabled(bool enabled)
        {
            if (_visualRenderers == null)
            {
                return;
            }
            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                if (_visualRenderers[i] != null)
                {
                    _visualRenderers[i].enabled = enabled;
                }
            }
        }

        private void ApplyBaseColors()
        {
            if (_visualRenderers == null || _baseColors == null)
            {
                return;
            }
            for (int i = 0; i < _visualRenderers.Length && i < _baseColors.Length; i++)
            {
                ApplyColor(_visualRenderers[i], _baseColors[i]);
            }
        }

        private void ApplyWarningTintAll(float strength)
        {
            if (_visualRenderers == null || _baseColors == null)
            {
                return;
            }
            float clamped = Mathf.Clamp01(strength);
            for (int i = 0; i < _visualRenderers.Length && i < _baseColors.Length; i++)
            {
                Color baseColor = _baseColors[i];
                Color tinted = Color.Lerp(baseColor, _warningColor, clamped);
                tinted.a = baseColor.a;
                ApplyColor(_visualRenderers[i], tinted);
            }
        }

        private void ApplyFadingColors(float t)
        {
            if (_visualRenderers == null || _baseColors == null)
            {
                return;
            }
            for (int i = 0; i < _visualRenderers.Length && i < _baseColors.Length; i++)
            {
                Color baseColor = _baseColors[i];
                Color warningTint = GetWarningTint(baseColor, 1f);
                ApplyColor(_visualRenderers[i], Color.Lerp(warningTint, new Color(baseColor.r, baseColor.g, baseColor.b, 0f), t));
            }
        }

        private Color GetWarningTint(Color baseColor, float t)
        {
            float strength = Mathf.Clamp01(t * _warningTintStrength);
            Color tinted = Color.Lerp(baseColor, _warningColor, strength);
            tinted.a = baseColor.a;
            return tinted;
        }

        /// <summary>
        /// SI_TileWarning shader の _WarningProgress / _EdgeEmissionMul を更新。
        /// 該当 property が無い material でも MaterialPropertyBlock 経由なので無害。
        /// </summary>
        private void ApplyWarningProgress(float progress, float emission)
        {
            if (_visualRenderers == null || _propBlock == null)
            {
                return;
            }
            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                MeshRenderer renderer = _visualRenderers[i];
                if (renderer == null)
                {
                    continue;
                }
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(WarningProgressId, progress);
                _propBlock.SetFloat(EdgeEmissionMulId, emission);
                renderer.SetPropertyBlock(_propBlock);
            }
        }

        /// <summary>
        /// SI_TileDissolve shader の _DissolveProgress を更新。
        /// </summary>
        private void ApplyDissolveProgress(float progress)
        {
            if (_visualRenderers == null || _propBlock == null)
            {
                return;
            }
            float clamped = Mathf.Clamp01(progress);
            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                MeshRenderer renderer = _visualRenderers[i];
                if (renderer == null)
                {
                    continue;
                }
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(DissolveProgressId, clamped);
                renderer.SetPropertyBlock(_propBlock);
            }
        }

        /// <summary>
        /// Healthy 戻し時の shader progress リセット (0 化)。
        /// </summary>
        private void ResetShaderProgress()
        {
            if (_visualRenderers == null || _propBlock == null)
            {
                return;
            }
            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                MeshRenderer renderer = _visualRenderers[i];
                if (renderer == null)
                {
                    continue;
                }
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(WarningProgressId, 0f);
                _propBlock.SetFloat(DissolveProgressId, 0f);
                _propBlock.SetFloat(EdgeEmissionMulId, 0f);
                renderer.SetPropertyBlock(_propBlock);
            }
        }

        private void ApplyColor(MeshRenderer renderer, Color c)
        {
            if (renderer == null)
            {
                return;
            }
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }
            renderer.GetPropertyBlock(_propBlock);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId))
            {
                _propBlock.SetColor(BaseColorId, c);
            }
            else
            {
                _propBlock.SetColor(ColorId, c);
            }
            renderer.SetPropertyBlock(_propBlock);
        }
    }
}
