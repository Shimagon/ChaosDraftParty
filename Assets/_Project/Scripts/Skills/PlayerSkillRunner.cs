using UnityEngine;
using ChaosDraftParty.Core;

namespace ChaosDraftParty.Skills
{
    /// <summary>
    /// プレイヤーのスキル発動ランナー (M2、Skills assembly 側に常駐)。
    ///
    /// 設計:
    /// - PlayerController と同じ GameObject に attach する。
    /// - PlayerController.SkillInputPressed (Action&lt;int&gt;) を subscribe して slot を受取。
    /// - Owner / KO / crouch lock の gate は PlayerController 側で済んでいるためここでは行わない。
    /// - CD 管理 + SkillDefinition.Effect.Apply 実行をここで担当する。
    ///
    /// asmdef 構成:
    /// - Core → Skills を参照しない (Skills → Core 既存依存と循環するため)。
    /// - Skills → Core 参照は維持。PlayerController の public event 経由で薄く繋ぐ。
    ///
    /// M3 migration plan:
    /// - SerializeField の _skillSlot1 / _skillSlot2 はドラフト UI で runtime に SetSkillSlot で差替え可能にする。
    /// - stack count は UI / Inventory 側から取得 (現状は 1 固定)。
    /// - ホスト権威化: 現状 Owner 側で Apply 直書きだが、M3 で ServerRpc 経由に切替予定。
    /// </summary>
    [DefaultExecutionOrder(60)] // PlayerController(50) より後、subscribe を確実にする
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerSkillRunner : MonoBehaviour
    {
        [Header("Skill Slots (M2: 2 slots、M3 でドラフト UI で動的設定)")]
        [Tooltip("Skill slot 1。Q (PC) / RB (Gamepad) で発動。M2 では Inspector 固定、M3 でドラフト UI で差替え。")]
        [SerializeField] private SkillDefinition _skillSlot1;

        [Tooltip("Skill slot 2。E (PC) / LB (Gamepad) で発動。")]
        [SerializeField] private SkillDefinition _skillSlot2;

        [Header("Debug")]
        [Tooltip("発動時に Debug.Log を出すか (sandbox 検証用)。")]
        [SerializeField] private bool _logActivation = false;

        // CD 残時間 (秒、Update で減算)。SerializeField しない (serialization.md: runtime state は serialize しない)。
        private float _skillSlot1CdRemaining;
        private float _skillSlot2CdRemaining;

        private PlayerController _controller;

        /// <summary>Slot 1 の CD 残時間 (秒、UI 連動 M3 用)。0 で発動可。</summary>
        public float SkillSlot1CdRemaining => _skillSlot1CdRemaining;

        /// <summary>Slot 2 の CD 残時間 (秒、UI 連動 M3 用)。</summary>
        public float SkillSlot2CdRemaining => _skillSlot2CdRemaining;

        /// <summary>Slot 1 の SkillDefinition (UI 表示用)。</summary>
        public SkillDefinition SkillSlot1 => _skillSlot1;

        /// <summary>Slot 2 の SkillDefinition (UI 表示用)。</summary>
        public SkillDefinition SkillSlot2 => _skillSlot2;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.SkillInputPressed += OnSkillInputPressed;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.SkillInputPressed -= OnSkillInputPressed;
            }
        }

        private void Update()
        {
            // CD 減算 (Owner / 単独再生問わず): event は PlayerController 側 gate 済なので
            // listener が unsubscribe されてれば呼ばれない。CD は visual 表示用に常時走らせる。
            if (_skillSlot1CdRemaining > 0f)
            {
                _skillSlot1CdRemaining -= Time.deltaTime;
                if (_skillSlot1CdRemaining < 0f) _skillSlot1CdRemaining = 0f;
            }
            if (_skillSlot2CdRemaining > 0f)
            {
                _skillSlot2CdRemaining -= Time.deltaTime;
                if (_skillSlot2CdRemaining < 0f) _skillSlot2CdRemaining = 0f;
            }
        }

        /// <summary>
        /// M3 ドラフト UI 用の runtime slot 差替え API。slot=1 or 2。
        /// </summary>
        public void SetSkillSlot(int slot, SkillDefinition definition)
        {
            if (slot == 1)
            {
                _skillSlot1 = definition;
                _skillSlot1CdRemaining = 0f;
            }
            else if (slot == 2)
            {
                _skillSlot2 = definition;
                _skillSlot2CdRemaining = 0f;
            }
        }

        private void OnSkillInputPressed(int slot)
        {
            SkillDefinition def = slot == 1 ? _skillSlot1 : (slot == 2 ? _skillSlot2 : null);
            if (def == null)
            {
                if (_logActivation) Debug.Log($"[PlayerSkillRunner] slot {slot} 未設定 (noop)");
                return;
            }

            float cdRemaining = slot == 1 ? _skillSlot1CdRemaining : _skillSlot2CdRemaining;
            if (cdRemaining > 0f)
            {
                if (_logActivation) Debug.Log($"[PlayerSkillRunner] slot {slot} ({def.id}) CD 中 残{cdRemaining:F1}s");
                return;
            }

            // SkillContext (M2: stack=1 固定、M3 でドラフト後 inventory から取得)
            // ホスト権威の clientId / aimDirection は NGO 統合後 M3 で正しい値を渡す。
            ulong clientId = 0;
            if (Unity.Netcode.NetworkManager.Singleton != null && _controller != null && _controller.IsSpawned)
            {
                clientId = _controller.OwnerClientId;
            }
            var ctx = new SkillContext(1, clientId, transform.position, transform.forward);

            // Effect 実行 (null は noop = M3 以降に本実装予定の skill stub 対応)
            if (def.Effect != null)
            {
                def.Effect.Apply(gameObject, ctx);
            }
            else if (_logActivation)
            {
                Debug.Log($"[PlayerSkillRunner] slot {slot} ({def.id}) Effect 未実装 (stub noop)");
            }

            // CD 設定 (Effect が null でも CD は走らせる: UI 表示のため)
            float cd = Mathf.Max(0.1f, def.cooldownSec);
            if (slot == 1) _skillSlot1CdRemaining = cd;
            else _skillSlot2CdRemaining = cd;

            if (_logActivation)
            {
                Debug.Log($"[PlayerSkillRunner] slot {slot} ({def.id}) 発動 CD={cd}s");
            }
        }
    }
}
