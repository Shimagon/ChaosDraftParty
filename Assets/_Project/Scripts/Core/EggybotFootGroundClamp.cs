using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT 足球の床めり込み防止 (v3e、LateUpdate world.y clamp)。
    ///
    /// 設計 (User 2026-05-21 確定、Plan A/B'/v2 3 周失敗の根本解):
    /// - Plan A/B'/v2 は「ある瞬間の bone.y で計算した固定 offset」アプローチ、全滅
    /// - 真因: bone (mixamorig:LeftFoot) world.y が Animator + PoseLock で動的振動
    ///   (実測 0.011〜0.162 で、フレームごとに大きく変動)
    /// - 動的解: LateUpdate で Foot 球の world.y を実測して、床より下に行く時のみ補正
    /// - Mario Odyssey 風: 足が床に張り付き、たまに浮く自然な見た目 (Fall Guys 系も同)
    ///
    /// DefaultExecutionOrder(130): EggybotFootSquash(120) 後に動作、最終的に world.y を保証。
    /// PoseLock / Mixamo / Spring 物理 全部に非破壊 (bone も Animator も触らない、Foot 球 world.y のみ補正)。
    ///
    /// M2 暫定: groundY=0 固定 (Sandbox_v21 + ミニゲーム全部平面床、Raycast 不要)。
    /// M3 以降: 傾斜地 / 段差で Physics.Raycast に拡張予定。
    ///
    /// FootSquash(120) との関係: FootSquash が接地時に localPosition で Y を上げた後に
    /// 本 component が world.y を評価するため、squash 有効時は intrusion がほぼ 0 で
    /// clamp はスキップされる (squash が既に床密着を演出している状態)。空中→着地の
    /// 遷移フレームと、squash 無効時の Animator clip 動的駆動で本 clamp が主に効く。
    /// </summary>
    [DefaultExecutionOrder(130)]
    public sealed class EggybotFootGroundClamp : MonoBehaviour
    {
        [Header("Foot References")]
        [Tooltip("床めり込み補正対象の Foot 球 (Foot_L, Foot_R)。FootSocket の child でも bone 直 child でも可。")]
        [SerializeField] Transform[] _feet;

        [Tooltip("接地判定参照。null の場合は親から自動取得。落下中は clamp しない。")]
        [SerializeField] PlayerController _player;

        [Header("Ground")]
        [Tooltip("床の Y 座標。Sandbox_v21 + ミニゲーム全部平面床なので 0 で十分。M3 で Raycast 化予定。")]
        [SerializeField] float _groundY = 0f;

        [Tooltip("Foot 球の半径 (lossyScale.y=0.20 で primitive Sphere bounds.extent.y = 0.10)。" +
                 "球底 = position.y - radius で計算、めり込み時のみ補正。")]
        [SerializeField] float _footRadius = 0.10f;

        [Header("Safety")]
        [Tooltip("補正の最大量 (m)。Animator が大きく足を上げてる瞬間に過補正しないよう上限。0.30 推奨。")]
        [SerializeField] float _maxClampDistance = 0.30f;

        void Awake()
        {
            if (_player == null)
            {
                _player = GetComponentInParent<PlayerController>();
            }
        }

        void LateUpdate()
        {
            if (_feet == null || _feet.Length == 0) return;
            if (_player != null && !_player.IsGrounded) return;
            if (_player != null && _player.transform.position.y < _groundY - _maxClampDistance)
            {
                return;
            }

            for (int i = 0; i < _feet.Length; i++)
            {
                var foot = _feet[i];
                if (foot == null) continue;

                Vector3 pos = foot.position;
                float bottom = pos.y - _footRadius;
                float intrusion = _groundY - bottom;

                if (intrusion > 0f)
                {
                    float clamp = Mathf.Min(intrusion, _maxClampDistance);
                    pos.y += clamp;
                    foot.position = pos;
                }
            }
        }
    }
}
