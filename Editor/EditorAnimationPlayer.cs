using UnityEngine;

namespace TakoBoyStudios.Animation
{
    /// <summary>
    /// Simple editor component for testing animations during development.
    /// </summary>
    public class EditorAnimationPlayer : MonoBehaviour
    {
        [Tooltip("Name of the animation to test")]
        public string animationToTestName;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("Play Animation")]
#endif
        public void PlayAnimation()
        {
            SpriteAnimation anim = GetComponent<SpriteAnimation>();
            if (anim != null && !string.IsNullOrEmpty(animationToTestName))
            {
                anim.Play(animationToTestName);
            }
            else
            {
                Debug.LogWarning("[EditorAnimationPlayer] No SpriteAnimation component or animation name not set!");
            }
        }
    }
}
