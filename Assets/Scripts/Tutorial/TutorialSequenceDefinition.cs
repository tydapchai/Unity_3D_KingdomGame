using System.Collections.Generic;
using UnityEngine;

namespace Unity.FantasyKingdom
{
    public enum TutorialAutoStartMode
    {
        Manual,
        AfterIntroConversation,
        Immediate
    }

    [CreateAssetMenu(fileName = "Tutorial Sequence", menuName = "Fantasy Kingdom/Tutorial/Sequence", order = 19)]
    public class TutorialSequenceDefinition : ScriptableObject
    {
        public string sequenceId = "intro_onboarding";
        public bool onlyRunOnce = true;
        public TutorialAutoStartMode autoStartMode = TutorialAutoStartMode.AfterIntroConversation;
        public List<TutorialStepDefinition> steps = new();

        public string BuildPlayerPrefsKey()
        {
            return $"tutorial.sequence.{sequenceId}.completed";
        }
    }
}
