using UnityEngine;
using ChaosDraftParty.Core.Configs;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// 仮の PlayerController。M1 実装用のプレースホルダ。
    /// GameConfig 参照の割当/読込、シリアライズ動作検証に使う。
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Config Reference")]
        [SerializeField] private GameConfig config;

        [Header("Debug State")]
        [SerializeField] private float currentSpeed;
        [SerializeField] private int currentHP = 100;

        public GameConfig Config => config;

        void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning($"[PlayerController] GameConfig 未 assign on {gameObject.name}");
                return;
            }
            currentSpeed = config.playerBaseSpeed;
        }

        public float GetStackMultiplier(int count) =>
            config != null ? config.GetStackMultiplier(count) : 1f;
    }
}
