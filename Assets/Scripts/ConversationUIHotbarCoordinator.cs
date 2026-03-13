using System.Collections.Generic;
using DialogueEditor;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.FantasyKingdom
{
    public class ConversationUIHotbarCoordinator : MonoBehaviour
    {
        [SerializeField] private GameObject hotbarRoot;
        [SerializeField] private RectTransform conversationRoot;

        private EventSystem eventSystemBeforeConversation;
        private bool createdFallbackEventSystem;
        private bool hotbarWasActive;
        private bool conversationLayoutApplied;
        private bool gameplayInputSuppressed;
        private bool cursorWasVisible;
        private CursorLockMode cursorLockModeBeforeConversation;
        private readonly List<StarterAssetsInputState> starterAssetsInputStates = new();
        private readonly List<BehaviourState> disabledGameplayBehaviours = new();

        private void OnEnable()
        {
            ConversationManager.OnConversationStarted += HandleConversationStarted;
            ConversationManager.OnConversationEnded += HandleConversationEnded;
        }

        private void OnDisable()
        {
            ConversationManager.OnConversationStarted -= HandleConversationStarted;
            ConversationManager.OnConversationEnded -= HandleConversationEnded;

            RestoreHotbarVisibility();
            RestoreEventSystemAfterConversation();

            if (gameplayInputSuppressed)
            {
                RestoreGameplayInput();
            }
        }

        private void HandleConversationStarted()
        {
            EnsureEventSystemForConversation();
            SuppressGameplayInput();

            hotbarWasActive = hotbarRoot != null && hotbarRoot.activeSelf;

            if (hotbarRoot != null)
            {
                hotbarRoot.SetActive(false);
            }
        }

        private void HandleConversationEnded()
        {
            RestoreHotbarVisibility();
            RestoreEventSystemAfterConversation();
            RestoreGameplayInput();
        }

        private void RestoreHotbarVisibility()
        {
            if (hotbarRoot != null)
            {
                hotbarRoot.SetActive(hotbarWasActive);
            }
        }

        private void SuppressGameplayInput()
        {
            if (gameplayInputSuppressed)
            {
                return;
            }

            cursorWasVisible = Cursor.visible;
            cursorLockModeBeforeConversation = Cursor.lockState;

            CacheStarterAssetsInputStates();
            ApplyStarterAssetsInputState(false);
            DisableGameplayBehaviours();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            gameplayInputSuppressed = true;
        }

        private void RestoreGameplayInput()
        {
            if (!gameplayInputSuppressed)
            {
                return;
            }

            RestoreGameplayBehaviours();
            RestoreStarterAssetsInputStates();

            Cursor.lockState = cursorLockModeBeforeConversation;
            Cursor.visible = cursorWasVisible;
            gameplayInputSuppressed = false;
        }

        private void EnsureEventSystemForConversation()
        {
            if (createdFallbackEventSystem)
            {
                return;
            }

            eventSystemBeforeConversation = EventSystem.current;

            if (eventSystemBeforeConversation != null && eventSystemBeforeConversation.gameObject.activeInHierarchy)
            {
                return;
            }

            GameObject eventSystemObject = new("ConversationEventSystem");
            eventSystemBeforeConversation = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            createdFallbackEventSystem = true;
        }

        private void RestoreEventSystemAfterConversation()
        {
            if (!createdFallbackEventSystem || eventSystemBeforeConversation == null)
            {
                return;
            }

            Destroy(eventSystemBeforeConversation.gameObject);
            eventSystemBeforeConversation = null;
            createdFallbackEventSystem = false;
        }

        private void CacheStarterAssetsInputStates()
        {
            starterAssetsInputStates.Clear();

            StarterAssetsInputs[] inputs = FindObjectsByType<StarterAssetsInputs>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (StarterAssetsInputs input in inputs)
            {
                if (input == null)
                {
                    continue;
                }

                starterAssetsInputStates.Add(new StarterAssetsInputState(
                    input,
                    input.cursorInputForLook,
                    input.cursorLocked));
            }
        }

        private void ApplyStarterAssetsInputState(bool gameplayEnabled)
        {
            foreach (StarterAssetsInputState state in starterAssetsInputStates)
            {
                if (state.Input == null)
                {
                    continue;
                }

                state.Input.cursorInputForLook = gameplayEnabled;
                state.Input.cursorLocked = gameplayEnabled;
                state.Input.MoveInput(Vector2.zero);
                state.Input.LookInput(Vector2.zero);
                state.Input.JumpInput(false);
                state.Input.SprintInput(false);
                state.Input.SetCursorState(gameplayEnabled);
            }
        }

        private void RestoreStarterAssetsInputStates()
        {
            foreach (StarterAssetsInputState state in starterAssetsInputStates)
            {
                if (state.Input == null)
                {
                    continue;
                }

                state.Input.cursorInputForLook = state.CursorInputForLook;
                state.Input.cursorLocked = state.CursorLocked;
                state.Input.MoveInput(Vector2.zero);
                state.Input.LookInput(Vector2.zero);
                state.Input.JumpInput(false);
                state.Input.SprintInput(false);
                state.Input.SetCursorState(state.CursorLocked);
            }

            starterAssetsInputStates.Clear();
        }

        private void DisableGameplayBehaviours()
        {
            disabledGameplayBehaviours.Clear();
            CacheAndDisableBehaviours(FindObjectsByType<CinemachineFreeLook>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None));
            CacheAndDisableBehaviours(FindObjectsByType<CinemachineInputProvider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None));
            CacheAndDisableBehaviours(FindObjectsByType<PlayerControl>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None));
        }

        private void CacheAndDisableBehaviours<T>(T[] behaviours) where T : Behaviour
        {
            foreach (T behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                disabledGameplayBehaviours.Add(new BehaviourState(behaviour, behaviour.enabled));
                behaviour.enabled = false;
            }
        }

        private void RestoreGameplayBehaviours()
        {
            foreach (BehaviourState state in disabledGameplayBehaviours)
            {
                if (state.Behaviour != null)
                {
                    state.Behaviour.enabled = state.WasEnabled;
                }
            }

            disabledGameplayBehaviours.Clear();
        }

        private readonly struct StarterAssetsInputState
        {
            public StarterAssetsInputState(StarterAssetsInputs input, bool cursorInputForLook, bool cursorLocked)
            {
                Input = input;
                CursorInputForLook = cursorInputForLook;
                CursorLocked = cursorLocked;
            }

            public StarterAssetsInputs Input { get; }
            public bool CursorInputForLook { get; }
            public bool CursorLocked { get; }
        }

        private readonly struct BehaviourState
        {
            public BehaviourState(Behaviour behaviour, bool wasEnabled)
            {
                Behaviour = behaviour;
                WasEnabled = wasEnabled;
            }

            public Behaviour Behaviour { get; }
            public bool WasEnabled { get; }
        }
    }
}
