using System.Collections.Generic;
using DevionGames.UIWidgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FantasyKingdom
{
    [DisallowMultipleComponent]
    public class TutorialUIWidget : UIWidget
    {
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text subText;
        [SerializeField] private TMP_Text completionToastText;
        [SerializeField] private RectTransform keyPromptContainer;
        [SerializeField] private RectTransform checklistContainer;
        [SerializeField] private TutorialKeyPromptView keyPromptPrefab;
        [SerializeField] private Image completionPulse;

        private readonly List<TutorialKeyPromptView> promptPool = new();
        private readonly List<TMP_Text> checklistPool = new();

        protected override void OnAwake()
        {
            base.OnAwake();
            EnsureRuntimeReferences();
            ApplyHudCanvasState();
            if (completionToastText != null)
            {
                completionToastText.gameObject.SetActive(false);
            }

            if (completionPulse != null)
            {
                completionPulse.gameObject.SetActive(false);
            }
        }

        public void RenderStep(TutorialSequenceDefinition sequence, TutorialStepDefinition step, int index, int totalSteps)
        {
            EnsureRuntimeReferences();
            ApplyHudCanvasState();

            if (headerText != null)
            {
                headerText.text = string.IsNullOrWhiteSpace(step.title) ? sequence.sequenceId : step.title;
            }

            if (counterText != null)
            {
                counterText.text = $"{index + 1}/{Mathf.Max(totalSteps, 1)}";
            }

            if (objectiveText != null)
            {
                objectiveText.text = step.objectiveText;
            }

            if (subText != null)
            {
                subText.text = step.subText;
                subText.gameObject.SetActive(!string.IsNullOrWhiteSpace(step.subText));
            }

            SetKeyPrompts(BuildHighlightLabels(step));
            SetChecklist(totalSteps, index);
            SetCompletionState(false, string.Empty);
        }

        public void SetCompletionState(bool completed, string completionText)
        {
            EnsureRuntimeReferences();

            if (completionToastText != null)
            {
                completionToastText.text = completionText;
                completionToastText.gameObject.SetActive(completed && !string.IsNullOrWhiteSpace(completionText));
            }

            if (completionPulse != null)
            {
                completionPulse.gameObject.SetActive(false);
            }
        }

        public void UpdateHighlightState(IReadOnlyList<bool> activeStates)
        {
            for (int i = 0; i < promptPool.Count; i++)
            {
                bool highlighted = activeStates != null && i < activeStates.Count && activeStates[i];
                promptPool[i].SetHighlighted(highlighted);
            }
        }

        private void SetKeyPrompts(IReadOnlyList<string> labels)
        {
            EnsureRuntimeReferences();

            int required = labels?.Count ?? 0;
            EnsurePromptPool(required);

            for (int i = 0; i < promptPool.Count; i++)
            {
                bool active = i < required;
                promptPool[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                promptPool[i].SetLabel(labels[i]);
                promptPool[i].SetHighlighted(false);
            }
        }

        private void SetChecklist(int totalSteps, int currentIndex)
        {
            EnsureRuntimeReferences();
            EnsureChecklistPool(totalSteps);

            for (int i = 0; i < checklistPool.Count; i++)
            {
                bool active = i < totalSteps;
                checklistPool[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                bool completed = i < currentIndex;
                bool current = i == currentIndex;
                string prefix = completed ? "✓" : current ? "•" : "○";
                checklistPool[i].text = $"{prefix} {i + 1}";
                checklistPool[i].color = completed
                    ? new Color32(210, 177, 98, 255)
                    : current
                        ? new Color32(245, 236, 214, 255)
                        : new Color32(150, 155, 170, 255);
            }
        }

        private List<string> BuildHighlightLabels(TutorialStepDefinition step)
        {
            if (step.highlightType == TutorialHighlightType.Custom && step.customHighlightLabels != null && step.customHighlightLabels.Length > 0)
            {
                return new List<string>(step.customHighlightLabels);
            }

            return step.highlightType switch
            {
                TutorialHighlightType.WASD => new List<string> { "W", "A", "S", "D" },
                TutorialHighlightType.MouseLook => new List<string> { "Mouse" },
                TutorialHighlightType.Jump => new List<string> { "Space" },
                TutorialHighlightType.Sprint => new List<string> { "Shift" },
                TutorialHighlightType.Interact => new List<string> { "E" },
                _ => new List<string>()
            };
        }

        private void EnsureRuntimeReferences()
        {
            if (headerText == null || counterText == null || objectiveText == null || subText == null || completionToastText == null || keyPromptContainer == null || checklistContainer == null)
            {
                CreateRuntimeLayout();
            }
        }

        private void ApplyHudCanvasState()
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
        }

        private void CreateRuntimeLayout()
        {
            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(36f, -36f);
            root.sizeDelta = new Vector2(460f, 260f);

            Image panel = GetComponent<Image>();
            if (panel == null)
            {
                panel = gameObject.AddComponent<Image>();
            }
            panel.color = new Color32(13, 18, 27, 220);

            Outline outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color32(182, 140, 72, 150);
            outline.effectDistance = new Vector2(2f, -2f);

            Shadow shadow = GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color32(0, 0, 0, 130);
            shadow.effectDistance = new Vector2(0f, -8f);

            VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layout.padding = new RectOffset(20, 20, 18, 18);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            if (headerText == null)
            {
                GameObject headerRow = CreateChild("HeaderRow", transform);
                HorizontalLayoutGroup headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
                headerLayout.childAlignment = TextAnchor.MiddleLeft;
                headerLayout.spacing = 8f;
                headerLayout.childForceExpandWidth = false;
                headerLayout.childControlWidth = false;

                headerText = CreateText("Header", headerRow.transform, 24f, FontStyles.Bold, TextAlignmentOptions.Left);
                LayoutElement headerLayoutElement = headerText.gameObject.AddComponent<LayoutElement>();
                headerLayoutElement.flexibleWidth = 1f;

                counterText = CreateText("Counter", headerRow.transform, 20f, FontStyles.Bold, TextAlignmentOptions.Right);
            }

            if (objectiveText == null)
            {
                objectiveText = CreateText("Objective", transform, 32f, FontStyles.Bold, TextAlignmentOptions.Left);
                objectiveText.enableWordWrapping = true;
            }

            if (subText == null)
            {
                subText = CreateText("SubText", transform, 20f, FontStyles.Normal, TextAlignmentOptions.Left);
                subText.enableWordWrapping = true;
                subText.color = new Color32(190, 198, 214, 255);
            }

            if (keyPromptContainer == null)
            {
                GameObject keyPromptRow = CreateChild("KeyPrompts", transform);
                keyPromptContainer = keyPromptRow.GetComponent<RectTransform>();
                HorizontalLayoutGroup keyLayout = keyPromptRow.AddComponent<HorizontalLayoutGroup>();
                keyLayout.spacing = 10f;
                keyLayout.childAlignment = TextAnchor.MiddleLeft;
                keyLayout.childForceExpandWidth = false;
                keyLayout.childForceExpandHeight = false;
            }

            if (checklistContainer == null)
            {
                GameObject checklistRow = CreateChild("Checklist", transform);
                checklistContainer = checklistRow.GetComponent<RectTransform>();
                HorizontalLayoutGroup checklistLayout = checklistRow.AddComponent<HorizontalLayoutGroup>();
                checklistLayout.spacing = 12f;
                checklistLayout.childAlignment = TextAnchor.MiddleLeft;
                checklistLayout.childForceExpandWidth = false;
                checklistLayout.childForceExpandHeight = false;
            }

            if (completionToastText == null)
            {
                completionToastText = CreateText("Completion", transform, 18f, FontStyles.Bold, TextAlignmentOptions.Left);
                completionToastText.color = new Color32(222, 189, 106, 255);
            }

            if (completionPulse == null)
            {
                GameObject pulseObject = CreateChild("CompletionPulse", transform);
                completionPulse = pulseObject.AddComponent<Image>();
                completionPulse.color = Color.clear;
                LayoutElement pulseLayout = pulseObject.AddComponent<LayoutElement>();
                pulseLayout.preferredHeight = 4f;
            }
        }

        private void EnsurePromptPool(int count)
        {
            for (int i = promptPool.Count; i < count; i++)
            {
                TutorialKeyPromptView prompt = CreatePromptView(i);
                promptPool.Add(prompt);
            }
        }

        private void EnsureChecklistPool(int count)
        {
            for (int i = checklistPool.Count; i < count; i++)
            {
                TMP_Text text = CreateText($"Checklist_{i}", checklistContainer, 16f, FontStyles.Bold, TextAlignmentOptions.Left);
                checklistPool.Add(text);
            }
        }

        private TutorialKeyPromptView CreatePromptView(int index)
        {
            if (keyPromptPrefab != null)
            {
                TutorialKeyPromptView instance = Instantiate(keyPromptPrefab, keyPromptContainer);
                instance.name = $"Prompt_{index}";
                return instance;
            }

            GameObject promptObject = CreateChild($"Prompt_{index}", keyPromptContainer);
            TutorialKeyPromptView view = promptObject.AddComponent<TutorialKeyPromptView>();
            view.EnsureRuntimeVisuals();
            return view;
        }

        private TMP_Text CreateText(string objectName, Transform parent, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateChild(objectName, parent);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.color = new Color32(245, 236, 214, 255);
            return text;
        }

        private static GameObject CreateChild(string objectName, Transform parent)
        {
            GameObject child = new(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}
