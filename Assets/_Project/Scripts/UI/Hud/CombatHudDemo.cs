using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Feeds a <see cref="CombatHudFixtures"/> state to the HUD so the Combat scene can be opened and inspected
    /// before the combat presentation exists. Runs in the editor too, so the scene looks right without Play Mode.
    /// </summary>
    [ExecuteAlways]
    public sealed class CombatHudDemo : MonoBehaviour
    {
        [SerializeField] private CombatHudView hud;
        [SerializeField] private HudFixture fixture = HudFixture.PartyActing;

        // In Play Mode the screen controller owns what the HUD shows; this component is for the Editor and captures.
        private void OnEnable()
        {
            if (!Application.isPlaying) Show();
        }

#if UNITY_EDITOR
        // The HUD creates and destroys children, which is not allowed inside OnValidate: wait for the editor loop.
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Show();
            };
        }
#endif

        /// <summary>Applies the selected fixture to the HUD.</summary>
        public void Show()
        {
            if (hud != null) hud.Apply(CombatHudFixtures.Create(fixture));
        }
    }
}
