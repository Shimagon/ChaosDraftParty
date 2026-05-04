using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using ChaosDraftParty.Core.Configs;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT 用 3rd person PlayerController (M1 single-player、M2 で NetworkBehaviour 化想定)。
    ///
    /// 設計 (v2、2026-05-04):
    /// - CharacterController で移動 (Rigidbody は爆弾のみ)
    /// - **OrbitalFollow** で自由視点カメラ (ThirdPersonFollow は target.rotation 読まないため使えない)
    /// - 視点入力 (Look) を直接 OrbitalFollow.HorizontalAxis/VerticalAxis に流す (InputAxisController 不要、code 制御)
    /// - WASD 移動方向 = camera 相対 (Vector3.ProjectOnPlane で XZ 投影)
    /// - **キャラを進行方向に Slerp で自動回転** (Quaternion.LookRotation + Slerp)
    /// - 重力は自前累積、CharacterController.Move 1 回呼びで水平+垂直まとめ
    /// - しゃがみ: Shift で EggybotCrouchSequence.PlayEnter/Exit を toggle (中は移動禁止)
    ///
    /// M2 移行時:
    /// - Apply* 関数分離なので IsOwner ガード追加だけで NetworkBehaviour 化可能
    /// - 入力は client local、位置は ClientNetworkTransform で同期
    /// </summary>
    [DefaultExecutionOrder(50)] // PoseLock(0) → 自分(50) → Crouch(100) → Capsule(110)
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
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
            ApplyLook();
            ApplyMovement();

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
            if (cachedCrouch.IsCrouching) crouchSequence.PlayExitCrouch();
            else crouchSequence.PlayEnterCrouch();
        }
    }
}
