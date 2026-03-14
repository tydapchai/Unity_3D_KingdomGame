using UnityEngine;

namespace Unity.FantasyKingdom
{
    public enum TutorialHighlightType
    {
        None,
        WASD,
        MouseLook,
        Jump,
        Sprint,
        Interact,
        Custom
    }

    public enum TutorialCompletionType
    {
        None,
        Move,
        Look,
        Jump,
        Sprint,
        EnterTrigger,
        Interact,
        ConversationEnded,
        CustomEvent
    }

    [CreateAssetMenu(fileName = "Tutorial Step", menuName = "Fantasy Kingdom/Tutorial/Step", order = 20)]
    public class TutorialStepDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string stepId = "step";
        public string title = "Tutorial";
        [TextArea(2, 4)] public string objectiveText;
        [TextArea(1, 3)] public string subText;
        [TextArea(1, 3)] public string completionText = "Completed";

        [Header("Visuals")]
        public TutorialHighlightType highlightType = TutorialHighlightType.None;
        public string[] customHighlightLabels;
        public bool showWorldMarker;
        public Transform target;

        [Header("Completion")]
        public TutorialCompletionType completionType = TutorialCompletionType.None;
        public string customEventId;
        [Min(0f)] public float inputThreshold = 0.25f;
        [Min(0f)] public float requiredDistance = 1.5f;
        [Min(0f)] public float requiredDuration = 0.5f;
        [Min(0f)] public float triggerRadius = 3f;

        [Header("Flow")]
        public bool autoAdvance = true;
        [Min(0f)] public float advanceDelay = 0.75f;
    }
}
