using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// Lightweight prototype HUD for playtests. Replace with the real minigame HUD later.
    /// </summary>
    public sealed class ShrinkingIslandPrototypeHud : MonoBehaviour
    {
        private const string SceneNameToken = "ShrinkingIsland";
        private static readonly Color PanelColor = new(0f, 0f, 0f, 0.45f);
        private static readonly Color TextColor = new(1f, 1f, 1f, 0.95f);

        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.name.Contains(SceneNameToken))
            {
                return;
            }

            if (FindAnyObjectByType<ShrinkingIslandPrototypeHud>() != null)
            {
                return;
            }

            GameObject hud = new("ShrinkingIslandPrototypeHud");
            hud.AddComponent<ShrinkingIslandPrototypeHud>();
        }

        private void OnGUI()
        {
            EnsureStyles();

            const float width = 360f;
            const float height = 96f;
            Rect rect = new(24f, 24f, width, height);

            Color previous = GUI.color;
            GUI.color = PanelColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;

            Rect titleRect = new(rect.x + 18f, rect.y + 14f, rect.width - 36f, 28f);
            Rect bodyRect = new(rect.x + 18f, rect.y + 48f, rect.width - 36f, 38f);
            GUI.Label(titleRect, "GET TO THE CENTER", _titleStyle);
            GUI.Label(bodyRect, "Corners fall first. Bridges are risky. Launch pads kick you back to center.", _bodyStyle);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = TextColor },
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = TextColor },
            };
        }
    }
}
