using System.Collections;
using System.Collections.Generic;
using DialogueEditor;
using DevionGames.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FantasyKingdom
{
    public class TutorialCoordinator : MonoBehaviour
    {
        [System.Serializable]
        private struct StepTargetBinding
        {
            public string stepId;
            public Transform target;
        }

        private enum TutorialState
        {
            WaitingForIntro,
            Running,
            Completed,
            Disabled
        }

        [Header("Setup")]
        [SerializeField] private TutorialSequenceDefinition sequence;
        [SerializeField] private TutorialInputFacade inputFacade;
        [SerializeField] private TutorialUIWidget tutorialUI;
        [SerializeField] private TutorialWorldMarker worldMarker;
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private Notification completionNotification;
        [SerializeField] private List<StepTargetBinding> stepTargetBindings = new();

        [Header("Mission Handoff")]
        [SerializeField] private bool startMissionOnTutorialComplete = true;

        [Header("Debug")]
        [SerializeField] private bool forceStartTutorial;
        [SerializeField] private bool resetCompletionOnPlay;

        private TutorialState state = TutorialState.WaitingForIntro;
        private int currentStepIndex = -1;
        private float currentStepElapsed;
        private float lookAccumulation;
        private float sprintAccumulation;
        private Vector3 moveStartPosition;
        private bool moveStartCaptured;
        private bool jumpStartedWhileGrounded;
        private bool lastGroundedState = true;
        private bool stepCompletionQueued;
        private Coroutine advanceRoutine;
        private readonly List<bool> highlightStates = new();

        private void Awake()
        {
            if (inputFacade == null)
            {
                inputFacade = FindFirstObjectByType<TutorialInputFacade>();
            }

            if (inputFacade == null)
            {
                inputFacade = gameObject.AddComponent<TutorialInputFacade>();
            }

            inputFacade.AutoAssignReferences();

            if (tutorialUI == null)
            {
                tutorialUI = FindFirstObjectByType<TutorialUIWidget>(FindObjectsInactive.Include);
            }

            if (missionManager == null)
            {
                missionManager = MissionManager.Instance != null ? MissionManager.Instance : FindFirstObjectByType<MissionManager>();
            }

            if (completionNotification == null)
            {
                completionNotification = WidgetUtility.Find<Notification>("Notification");
            }

            if (resetCompletionOnPlay && sequence != null)
            {
                PlayerPrefs.DeleteKey(sequence.BuildPlayerPrefsKey());
                PlayerPrefs.Save();
            }

            if (tutorialUI != null)
            {
                tutorialUI.gameObject.SetActive(true);
                tutorialUI.Close();
            }

            if (worldMarker != null)
            {
                worldMarker.gameObject.SetActive(false);
            }

            if (sequence == null)
            {
                state = TutorialState.Disabled;
                return;
            }

            if (forceStartTutorial)
            {
                state = TutorialState.WaitingForIntro;
                return;
            }

            if (sequence.onlyRunOnce && PlayerPrefs.GetInt(sequence.BuildPlayerPrefsKey(), 0) == 1)
            {
                state = TutorialState.Completed;
                return;
            }

            if (sequence.autoStartMode == TutorialAutoStartMode.Immediate)
            {
                state = TutorialState.WaitingForIntro;
            }
        }

        private void OnEnable()
        {
            ConversationManager.OnConversationStarted += HandleConversationStarted;
            ConversationManager.OnConversationEnded += HandleConversationEnded;
        }

        private void OnDisable()
        {
            ConversationManager.OnConversationStarted -= HandleConversationStarted;
            ConversationManager.OnConversationEnded -= HandleConversationEnded;
        }

        private void Start()
        {
            if (state == TutorialState.Disabled || sequence == null)
            {
                return;
            }

            if (forceStartTutorial || sequence.autoStartMode == TutorialAutoStartMode.Immediate)
            {
                TryStartTutorial();
                return;
            }

            if (sequence.autoStartMode == TutorialAutoStartMode.AfterIntroConversation && ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
            {
                // Wait for intro conversation end event unless explicitly forced.
            }
        }

        private void Update()
        {
            if (state != TutorialState.Running || sequence == null || currentStepIndex < 0 || currentStepIndex >= sequence.steps.Count)
            {
                return;
            }

            TutorialStepDefinition step = sequence.steps[currentStepIndex];
            if (step == null)
            {
                AdvanceToNextStep();
                return;
            }

            if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            {
                if (!stepCompletionQueued && step.completionType == TutorialCompletionType.Interact && IsStepComplete(step))
                {
                    CompleteCurrentStep(step);
                }

                if (tutorialUI != null && tutorialUI.IsVisible)
                {
                    tutorialUI.Close();
                }
                return;
            }

            if (tutorialUI != null && !tutorialUI.IsVisible)
            {
                tutorialUI.Show();
            }

            currentStepElapsed += Time.deltaTime;
            UpdateHighlightState(step);
            tutorialUI?.UpdateHighlightState(highlightStates);

            if (!stepCompletionQueued && IsStepComplete(step))
            {
                CompleteCurrentStep(step);
            }
        }

        public void NotifyCustomEvent(string eventId)
        {
            if (state != TutorialState.Running || string.IsNullOrWhiteSpace(eventId) || sequence == null || currentStepIndex < 0 || currentStepIndex >= sequence.steps.Count)
            {
                return;
            }

            TutorialStepDefinition step = sequence.steps[currentStepIndex];
            if (step != null && step.completionType == TutorialCompletionType.CustomEvent && step.customEventId == eventId)
            {
                CompleteCurrentStep(step);
            }
        }

        private void HandleConversationStarted()
        {
            if (state == TutorialState.Running)
            {
                TutorialStepDefinition step = GetCurrentStep();
                if (step != null && step.completionType == TutorialCompletionType.Interact && !stepCompletionQueued)
                {
                    Transform interactTarget = ResolveTarget(step);
                    if (interactTarget == null || (inputFacade != null && inputFacade.IsNearTarget(interactTarget, step.triggerRadius)))
                    {
                        CompleteCurrentStep(step);
                    }
                }
            }

            if (tutorialUI != null && tutorialUI.IsVisible)
            {
                tutorialUI.Close();
            }
        }

        private void HandleConversationEnded()
        {
            if (state == TutorialState.Completed || state == TutorialState.Disabled)
            {
                return;
            }

            if (forceStartTutorial)
            {
                TryStartTutorial();
                return;
            }

            if (state == TutorialState.WaitingForIntro && sequence != null && sequence.autoStartMode == TutorialAutoStartMode.AfterIntroConversation)
            {
                TryStartTutorial();
                return;
            }

            if (state == TutorialState.Running)
            {
                TutorialStepDefinition step = GetCurrentStep();
                if (step != null && step.completionType == TutorialCompletionType.ConversationEnded)
                {
                    CompleteCurrentStep(step);
                }
                else if (tutorialUI != null && !tutorialUI.IsVisible)
                {
                    tutorialUI.Show();
                }
            }
        }

        private void TryStartTutorial()
        {
            if (sequence == null || state == TutorialState.Running || state == TutorialState.Completed)
            {
                return;
            }

            if (sequence.steps == null || sequence.steps.Count == 0)
            {
                CompleteSequence();
                return;
            }

            state = TutorialState.Running;
            currentStepIndex = -1;
            AdvanceToNextStep();
        }

        private void AdvanceToNextStep()
        {
            if (advanceRoutine != null)
            {
                StopCoroutine(advanceRoutine);
                advanceRoutine = null;
            }

            currentStepIndex++;
            if (sequence == null || currentStepIndex >= sequence.steps.Count)
            {
                CompleteSequence();
                return;
            }

            BeginStep(sequence.steps[currentStepIndex]);
        }

        private void BeginStep(TutorialStepDefinition step)
        {
            currentStepElapsed = 0f;
            lookAccumulation = 0f;
            sprintAccumulation = 0f;
            moveStartCaptured = false;
            jumpStartedWhileGrounded = false;
            stepCompletionQueued = false;
            lastGroundedState = inputFacade == null || inputFacade.IsGrounded;
            BuildHighlightStates(step);

            Transform resolvedTarget = ResolveTarget(step);

            if (tutorialUI != null)
            {
                tutorialUI.RenderStep(sequence, step, currentStepIndex, sequence.steps.Count);
                tutorialUI.UpdateHighlightState(highlightStates);
                if (!tutorialUI.IsVisible)
                {
                    tutorialUI.Show();
                }
            }

            RefreshWorldMarker(step, resolvedTarget);
        }

        private void CompleteCurrentStep(TutorialStepDefinition step)
        {
            if (state != TutorialState.Running || stepCompletionQueued)
            {
                return;
            }

            stepCompletionQueued = true;
            tutorialUI?.SetCompletionState(true, step.completionText);

            if (step.autoAdvance)
            {
                advanceRoutine = StartCoroutine(AdvanceAfterDelay(step.advanceDelay));
            }
        }

        private IEnumerator AdvanceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            advanceRoutine = null;
            AdvanceToNextStep();
        }

        private bool IsStepComplete(TutorialStepDefinition step)
        {
            if (inputFacade == null)
            {
                return false;
            }

            switch (step.completionType)
            {
                case TutorialCompletionType.None:
                    return currentStepElapsed >= step.advanceDelay;
                case TutorialCompletionType.Move:
                    return CheckMoveCompletion(step);
                case TutorialCompletionType.Look:
                    return CheckLookCompletion(step);
                case TutorialCompletionType.Jump:
                    return CheckJumpCompletion();
                case TutorialCompletionType.Sprint:
                    return CheckSprintCompletion(step);
                case TutorialCompletionType.EnterTrigger:
                    return inputFacade.IsNearTarget(ResolveTarget(step), step.triggerRadius);
                case TutorialCompletionType.Interact:
                    Transform interactTarget = ResolveTarget(step);
                    return inputFacade.InteractPressed && (interactTarget == null || inputFacade.IsNearTarget(interactTarget, step.triggerRadius));
                case TutorialCompletionType.ConversationEnded:
                case TutorialCompletionType.CustomEvent:
                    return false;
                default:
                    return false;
            }
        }

        private bool CheckMoveCompletion(TutorialStepDefinition step)
        {
            if (inputFacade.GetMoveMagnitude() < step.inputThreshold)
            {
                moveStartCaptured = false;
                return false;
            }

            if (!moveStartCaptured)
            {
                moveStartCaptured = true;
                moveStartPosition = inputFacade.GetPlayerPosition();
                return false;
            }

            Vector3 currentPosition = inputFacade.GetPlayerPosition();
            Vector3 origin = moveStartPosition;
            currentPosition.y = 0f;
            origin.y = 0f;
            return Vector3.Distance(origin, currentPosition) >= step.requiredDistance;
        }

        private bool CheckLookCompletion(TutorialStepDefinition step)
        {
            lookAccumulation += inputFacade.GetLookMagnitude() * Time.deltaTime * 60f;
            return lookAccumulation >= Mathf.Max(step.inputThreshold, 10f);
        }

        private bool CheckJumpCompletion()
        {
            bool grounded = inputFacade.IsGrounded;
            if (grounded && inputFacade.JumpPressed)
            {
                jumpStartedWhileGrounded = true;
            }

            bool leftGround = jumpStartedWhileGrounded && lastGroundedState && !grounded;
            lastGroundedState = grounded;
            return leftGround;
        }

        private bool CheckSprintCompletion(TutorialStepDefinition step)
        {
            if (inputFacade.SprintPressed && inputFacade.GetMoveMagnitude() >= step.inputThreshold)
            {
                sprintAccumulation += Time.deltaTime;
            }
            else
            {
                sprintAccumulation = 0f;
            }

            return sprintAccumulation >= Mathf.Max(step.requiredDuration, 0.2f);
        }

        private void BuildHighlightStates(TutorialStepDefinition step)
        {
            highlightStates.Clear();
            int promptCount = step.highlightType switch
            {
                TutorialHighlightType.WASD => 4,
                TutorialHighlightType.Custom => step.customHighlightLabels != null ? step.customHighlightLabels.Length : 0,
                TutorialHighlightType.None => 0,
                _ => 1
            };

            for (int i = 0; i < promptCount; i++)
            {
                highlightStates.Add(false);
            }
        }

        private void UpdateHighlightState(TutorialStepDefinition step)
        {
            if (highlightStates.Count == 0 || inputFacade == null)
            {
                return;
            }

            switch (step.highlightType)
            {
                case TutorialHighlightType.WASD:
                    Vector2 move = inputFacade.Move;
                    highlightStates[0] = move.y > 0.1f;
                    highlightStates[1] = move.x < -0.1f;
                    highlightStates[2] = move.y < -0.1f;
                    highlightStates[3] = move.x > 0.1f;
                    break;
                case TutorialHighlightType.MouseLook:
                    highlightStates[0] = inputFacade.GetLookMagnitude() > 0.05f;
                    break;
                case TutorialHighlightType.Jump:
                    highlightStates[0] = inputFacade.JumpPressed;
                    break;
                case TutorialHighlightType.Sprint:
                    highlightStates[0] = inputFacade.SprintPressed;
                    break;
                case TutorialHighlightType.Interact:
                    highlightStates[0] = Input.GetKey(KeyCode.E);
                    break;
                case TutorialHighlightType.Custom:
                    for (int i = 0; i < highlightStates.Count; i++)
                    {
                        highlightStates[i] = false;
                    }
                    break;
            }
        }

        private void RefreshWorldMarker(TutorialStepDefinition step, Transform resolvedTarget)
        {
            if (worldMarker == null)
            {
                return;
            }

            if (step != null && step.showWorldMarker && resolvedTarget != null)
            {
                worldMarker.gameObject.SetActive(true);
                worldMarker.AttachTo(resolvedTarget);
            }
            else
            {
                worldMarker.ClearTarget();
                worldMarker.gameObject.SetActive(false);
            }
        }

        private void CompleteSequence()
        {
            state = TutorialState.Completed;

            if (advanceRoutine != null)
            {
                StopCoroutine(advanceRoutine);
                advanceRoutine = null;
            }

            tutorialUI?.Close();
            RefreshWorldMarker(null, null);

            if (sequence != null && sequence.onlyRunOnce)
            {
                PlayerPrefs.SetInt(sequence.BuildPlayerPrefsKey(), 1);
                PlayerPrefs.Save();
            }

            if (completionNotification != null)
            {
                completionNotification.AddItem("Hoàn tất hướng dẫn nhập môn");
            }

            if (startMissionOnTutorialComplete && missionManager != null && !missionManager.MissionStarted)
            {
                missionManager.StartMission();
            }
        }

        private Transform ResolveTarget(TutorialStepDefinition step)
        {
            if (step == null)
            {
                return null;
            }

            if (step.target != null)
            {
                return step.target;
            }

            if (!string.IsNullOrWhiteSpace(step.stepId))
            {
                for (int i = 0; i < stepTargetBindings.Count; i++)
                {
                    StepTargetBinding binding = stepTargetBindings[i];
                    if (binding.target != null && string.Equals(binding.stepId, step.stepId, System.StringComparison.Ordinal))
                    {
                        return binding.target;
                    }
                }
            }

            return null;
        }

        private TutorialStepDefinition GetCurrentStep()
        {
            if (sequence == null || currentStepIndex < 0 || currentStepIndex >= sequence.steps.Count)
            {
                return null;
            }

            return sequence.steps[currentStepIndex];
        }
    }
}
