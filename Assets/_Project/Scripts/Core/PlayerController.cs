using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Netcode;
using ChaosDraftParty.Core.Configs;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT 用 3rd person PlayerController (NetworkBehaviour、Owner 権威)。
    ///
    /// 設計 (v3、2026-05-05 NGO 化):
    /// - CharacterController で移動 (Rigidbody は爆弾のみ)
    /// - **OrbitalFollow** で自由視点カメラ (ThirdPersonFollow は target.rotation 読まないため使えない)
    /// - 視点入力 (Look) を直接 OrbitalFollow.HorizontalAxis/VerticalAxis に流す (InputAxisController 不要、code 制御)
    /// - WASD 移動方向 = camera 相対 (Vector3.ProjectOnPlane で XZ 投影)
    /// - **キャラを進行方向に Slerp で自動回転** (Quaternion.LookRotation + Slerp)
    /// - 重力は自前累積、CharacterController.Move 1 回呼びで水平+垂直まとめ
    /// - しゃがみ: Shift で EggybotCrouchSequence.PlayEnter/Exit を toggle (中は移動禁止)
    ///
    /// Network 設計 (CLAUDE.md 同期表準拠):
    /// - **位置 = Owner 権威**: prefab に NetworkTransform (AuthorityMode=Owner) を付ける。
    ///   PlayerController.Update は Owner のみで動き、CharacterController.Move でローカル移動。
    ///   NetworkTransform が他クライアントへ補間配信。
    /// - HP / Score / RoundState 等は別途 NetworkVariable<T> で server→all 同期 (M2)
    /// - Animator 同期は NetworkAnimator を prefab に追加して owner→all で broadcast 推奨
    /// - 単独再生 (Sandbox_v19 等、NetworkManager 無し) でも従来通り動作する (IsSpawned=false で gate しない)
    /// </summary>
    [DefaultExecutionOrder(50)] // PoseLock(0) → 自分(50) → Crouch(100) → Capsule(110)
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField] GameConfig config;

        [Header("References")]
        [Tooltip("OrbitalFollow が attach された CinemachineCamera (yaw/pitch 駆動先)。")]
        [SerializeField] CinemachineCamera cinemachineCamera;

        [Tooltip("移動方向計算用の camera (CinemachineBrain 付き Main Camera 推奨)。")]
        [SerializeField] Transform cameraTransform;

        [Tooltip("しゃがみ動作呼出用 (任意、未設定なら shift 無効)。")]
        [SerializeField] EggybotCrouchSequence crouchSequence;

        [Header("Movement")]
        [Tooltip("通常歩行速度 (m/s)。Walking clip の自然歩幅に合わせる。1.5 推奨。")]
        [SerializeField] float walkSpeed = 1.5f;

        [Tooltip("ダッシュ速度 (m/s)。Ctrl 押下中。8 推奨。")]
        [SerializeField] float dashSpeed = 8f;

        [Tooltip("水平方向加減速 (m/s²)。値大で即応。")]
        [SerializeField] float horizontalAccel = 25f;

        [Tooltip("キャラ進行方向追従の Slerp 速度 (1/s)。値大で即旋回。12 推奨。")]
        [SerializeField] float turnSmoothness = 12f;

        [Header("Look")]
        [Tooltip("マウス yaw 感度 (deg / pixel)。")]
        [SerializeField] float yawSensitivity = 0.20f;

        [Tooltip("マウス pitch 感度 (deg / pixel)。")]
        [SerializeField] float pitchSensitivity = 0.15f;

        [Tooltip("通常時のカメラ距離 (m)。OrbitalFollow.Radius の base 値。")]
        [SerializeField] float cameraBaseRadius = 5f;

        [Tooltip("ダッシュ中のカメラ距離 (m)。少し離れて速度感増し。6 推奨。")]
        [SerializeField] float cameraDashRadius = 6f;

        [Tooltip("カメラ Radius lerp 速度 (1/s)。値大で即追従。4 推奨。")]
        [SerializeField] float cameraRadiusLerpSpeed = 4f;

        [Tooltip("カメラの world Y がこれを下回らないように pitch を動的 clamp する。地面 Y=0 + 余裕 0.3 推奨。")]
        [SerializeField] float cameraMinWorldY = 0.3f;

        [Header("Animation")]
        [Tooltip("Animator (Locomotion blend tree、Speed parameter で Idle/Walk/Run を駆動)。")]
        [SerializeField] Animator animator;

        [Tooltip("Animator.Speed の damp time (秒)。0.1 で滑らか開始/停止。")]
        [SerializeField] float speedDampTime = 0.1f;

        [Header("Network State")]
        [Tooltip("KO 判定用 Y 閾値。world Y がこれ未満で host が isAlive=false にする。地面 Y=0 + 余裕。-5 推奨。")]
        [SerializeField] float koThresholdY = -5f;

        [Header("Debug")]
        [SerializeField] bool lockCursor = true;

        public float CurrentPlanarSpeed => new Vector2(horizontalVel.x, horizontalVel.z).magnitude;
        public bool IsGrounded => characterController != null && characterController.isGrounded;

        CharacterController characterController;
        CinemachineOrbitalFollow orbital;
        EggybotCrouch cachedCrouch;
        InputAction moveAction;
        InputAction lookAction;
        InputAction jumpAction;
        InputAction crouchAction;
        InputAction dashAction;

        Vector3 horizontalVel;
        float verticalVel;
        bool jumpQueued;
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int JumpHash = Animator.StringToHash("Jump");

        // Network: 生存状態。host が write 権限、全 client read。
        // 縮む島で Y < koThresholdY になったら host が false に → 全 client で操作不能 + visual 落下継続
        public readonly Unity.Netcode.NetworkVariable<bool> IsAlive = new(
            true,
            Unity.Netcode.NetworkVariableReadPermission.Everyone,
            Unity.Netcode.NetworkVariableWritePermission.Server);

        // Network: しゃがみ state (truth)。Owner が write、全 client read。
        // OnValueChanged で各 client が PlayCrouchLocal を起動 → coroutine sequence + CC リサイズ同期。
        // 後から join した client も初期値で正しい状態に snap される (catch-up)。
        public readonly Unity.Netcode.NetworkVariable<bool> NetIsCrouching = new(
            false,
            Unity.Netcode.NetworkVariableReadPermission.Everyone,
            Unity.Netcode.NetworkVariableWritePermission.Owner);

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (config == null)
            {
                Debug.LogWarning($"[PlayerController] GameConfig 未 assign on {gameObject.name}");
            }

            if (cinemachineCamera != null)
            {
                orbital = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
                if (orbital == null)
                    Debug.LogWarning("[PlayerController] CinemachineCamera に OrbitalFollow が無い");
            }

            if (crouchSequence != null)
            {
                cachedCrouch = crouchSequence.GetComponent<EggybotCrouch>();
                if (cachedCrouch == null)
                    Debug.LogWarning($"[PlayerController] crouchSequence に EggybotCrouch が無い on {gameObject.name} — ToggleCrouch は無効");
            }

            BuildInputActions();
        }

        void OnDestroy()
        {
            moveAction?.Dispose();
            lookAction?.Dispose();
            jumpAction?.Dispose();
            crouchAction?.Dispose();
            dashAction?.Dispose();
        }

        void OnEnable()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
            crouchAction?.Enable();
            dashAction?.Enable();
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            crouchAction?.Disable();
            dashAction?.Disable();
        }

        public override void OnNetworkSpawn()
        {
            // 全 client が NetIsCrouching を購読 (host / 他人問わず)
            NetIsCrouching.OnValueChanged += HandleNetCrouchChanged;
            // 後 join での catch-up: 既に crouch 中なら即 snap
            if (NetIsCrouching.Value && cachedCrouch != null)
                cachedCrouch.Crouch();

            if (!IsOwner)
            {
                // Non-owner (他クライアントから見た他人キャラ): 入力 / Update を止める。
                // 位置は NetworkTransform が補間配信、Animator は NetworkAnimator が同期する想定。
                // enabled=false で OnDisable が走り inputs も Disable される。
                enabled = false;
                return;
            }
            // Owner: scene の CinemachineCamera を見つけて自分の CameraTarget に bind する
            SetupOwnerCamera();
        }

        public override void OnNetworkDespawn()
        {
            NetIsCrouching.OnValueChanged -= HandleNetCrouchChanged;
        }

        void HandleNetCrouchChanged(bool oldVal, bool newVal)
        {
            PlayCrouchLocal(newVal);
        }

        /// <summary>Owner のみ: scene の CinemachineCamera を find して、自分の CameraTarget child に Follow/LookAt 設定。</summary>
        void SetupOwnerCamera()
        {
            if (cinemachineCamera == null)
                cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogWarning("[PlayerController] CinemachineCamera が scene に無い、カメラ追従なし");
                return;
            }
            orbital = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();

            var camTarget = transform.Find("CameraTarget");
            var followTarget = camTarget != null ? camTarget : transform;
            cinemachineCamera.Follow = followTarget;
            cinemachineCamera.LookAt = followTarget;
            cinemachineCamera.Priority = 10;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void BuildInputActions()
        {
            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
            lookAction.AddBinding("<Mouse>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
            jumpAction.performed += _ => jumpQueued = true;

            crouchAction = new InputAction("Crouch", InputActionType.Button);
            crouchAction.AddBinding("<Keyboard>/leftShift");
            crouchAction.AddBinding("<Gamepad>/buttonEast");
            crouchAction.performed += _ => ToggleCrouch();

            dashAction = new InputAction("Dash", InputActionType.Button);
            dashAction.AddBinding("<Keyboard>/leftCtrl");
            dashAction.AddBinding("<Keyboard>/rightCtrl");
            dashAction.AddBinding("<Gamepad>/leftTrigger");
        }

        void Update()
        {
            // KO されたら入力 / camera 駆動を止める (落下 visual は継続)
            if (IsSpawned && !IsAlive.Value)
            {
                // CC は持ち主の重力に任せて落とす (Move は呼ばない、または gravity だけ適用)
                if (characterController != null && characterController.enabled)
                {
                    verticalVel += (config != null ? config.GravityAcceleration : -9.81f) * Time.deltaTime;
                    characterController.Move(Vector3.up * verticalVel * Time.deltaTime);
                }
                return;
            }

            ApplyLook();
            ApplyMovement();

            // host のみ: Y < threshold で KO 確定 (NetworkVariable で全 client 同期)
            if (IsSpawned && IsServer && IsAlive.Value && transform.position.y < koThresholdY)
            {
                IsAlive.Value = false;
                Debug.Log($"[PlayerController] KO clientId={(IsOwner ? "self" : "remote")} pos={transform.position}");
            }

            // Editor DX: Escape で cursor 一時 unlock
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void ApplyLook()
        {
            // Look 入力を OrbitalFollow の axes に直接流す (Cinemachine が camera 配置)
            if (orbital == null) return;
            var lookDelta = lookAction.ReadValue<Vector2>();

            // Horizontal: Wrap=true 時は wraparound、code 直書きは clamp 不要
            orbital.HorizontalAxis.Value += lookDelta.x * yawSensitivity;

            // Vertical: 動的 minPitch で hard clamp。
            // camera.Y = target.Y + radius * sin(pitch) で、camera.Y >= cameraMinWorldY を保つ角度を逆算。
            // この方式なら床貫通せず、しかも 1 つの clamp で完結 (GroundClamp/Deoccluder と競合しない)。
            var v = orbital.VerticalAxis;
            float radius = Mathf.Max(orbital.Radius, 0.1f);
            float targetY = (cinemachineCamera != null && cinemachineCamera.LookAt != null)
                ? cinemachineCamera.LookAt.position.y
                : transform.position.y + 0.5f;
            float sinMin = Mathf.Clamp((cameraMinWorldY - targetY) / radius, -1f, 1f);
            float dynamicMinPitch = Mathf.Asin(sinMin) * Mathf.Rad2Deg;
            // Inspector Range.x も尊重 (より厳しい方を採用 = 大きい方の負値)
            float clampMin = Mathf.Max(v.Range.x, dynamicMinPitch);
            v.Value = Mathf.Clamp(v.Value - lookDelta.y * pitchSensitivity, clampMin, v.Range.y);
            orbital.VerticalAxis = v;
        }

        void ApplyMovement()
        {
            // しゃがみ sequence (open/close 動作中) は移動・ジャンプ禁止 (coroutine と衝突回避)
            // しゃがみ静止中は移動 OK (user 要求 2026-05-04)
            bool moveLocked = crouchSequence != null && crouchSequence.IsRunning;

            Vector2 moveInput = moveLocked ? Vector2.zero : moveAction.ReadValue<Vector2>();

            // camera 相対方向 (XZ projection)
            Vector3 forward, right;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }
            else
            {
                forward = Vector3.forward;
                right = Vector3.right;
            }

            Vector3 dir = forward * moveInput.y + right * moveInput.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // キャラを進行方向に Slerp 回転 (移動入力がある時だけ)
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSmoothness * Time.deltaTime);
            }

            // ダッシュ判定 (Ctrl 押下中) — 移動入力がある時だけ加速、しゃがみ中は無効
            bool dashing = !moveLocked && dir.sqrMagnitude > 0.01f
                           && dashAction != null && dashAction.IsPressed();
            float speed = dashing ? dashSpeed : walkSpeed;
            Vector3 wishVel = dir * speed;
            horizontalVel = Vector3.MoveTowards(horizontalVel, wishVel, horizontalAccel * Time.deltaTime);

            // 重力 + ジャンプ (jump trigger は実際にジャンプ実行時のみ発火)
            if (characterController.isGrounded)
            {
                if (verticalVel < 0f) verticalVel = -1f;
                if (jumpQueued && !moveLocked)
                {
                    verticalVel = config != null ? config.playerJumpSpeed : 7f;
                    if (animator != null) animator.SetTrigger(JumpHash);
                }
            }
            jumpQueued = false;

            float gravity = config != null ? config.GravityAcceleration : -9.81f;
            verticalVel += gravity * Time.deltaTime;

            characterController.Move((horizontalVel + Vector3.up * verticalVel) * Time.deltaTime);

            // Animator: Idle / Walk / Run blend (Speed param = 実速度 m/s、BlendTree threshold 0/1.5/8 と直接対応)
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, CurrentPlanarSpeed, speedDampTime, Time.deltaTime);
            }

            // ダッシュ中はカメラ少し離す (OrbitalFollow.Radius を lerp)
            if (orbital != null)
            {
                float targetRadius = dashing ? cameraDashRadius : cameraBaseRadius;
                orbital.Radius = Mathf.Lerp(orbital.Radius, targetRadius, cameraRadiusLerpSpeed * Time.deltaTime);
            }
        }

        void ToggleCrouch()
        {
            if (crouchSequence == null || crouchSequence.IsRunning) return;
            if (cachedCrouch == null) return;

            bool wantCrouch = !cachedCrouch.IsCrouching;

            // 立ち上がり要求時は先に天井チェック。塞がれてたら Bonk 演出して crouch 維持。
            // (Owner-side で先に判定。NetworkVariable に false を書いても他 client では立ち上がるが、
            //  この block は physics 局所判定なので owner-only でよい。網羅性は M2 で改善予定。)
            if (!wantCrouch && !cachedCrouch.CanStandUp())
            {
                cachedCrouch.PlayBonk();
                return;
            }

            if (IsSpawned)
            {
                // Network mode: Owner 権限の NetworkVariable を更新するだけ。
                // OnValueChanged で全 client (自分含む) が HandleNetCrouchChanged → PlayCrouchLocal 実行。
                NetIsCrouching.Value = wantCrouch;
            }
            else
            {
                // Single-player (Sandbox_v19 単独再生等): 直接ローカル実行
                PlayCrouchLocal(wantCrouch);
            }
        }

        void PlayCrouchLocal(bool wantCrouch)
        {
            if (crouchSequence == null || crouchSequence.IsRunning) return;
            if (cachedCrouch == null) return;
            if (wantCrouch && !cachedCrouch.IsCrouching) crouchSequence.PlayEnterCrouch();
            else if (!wantCrouch && cachedCrouch.IsCrouching) crouchSequence.PlayExitCrouch();
        }
    }
}
