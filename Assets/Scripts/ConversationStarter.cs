using DialogueEditor;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Unity.FantasyKingdom
{
    public class ConversationStarter : MonoBehaviour
    {
        [SerializeField] private NPCConversation myConversation;
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private TMP_Text interactionPromptText;
        [SerializeField] private string interactionPromptMessage = "Press E to talk";
        [SerializeField] private Vector2 interactionPromptPosition = new(0f, 120f);

        private NpcPatrol npcPatrol;
        private NavMeshAgent navMeshAgent;
        private bool conversationOwnedByThisStarter;
        private bool createdRuntimePrompt;

        private void Awake()
        {
            npcPatrol = GetComponent<NpcPatrol>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            EnsureInteractionPrompt();
            RefreshInteractionPrompt();
            SetInteractionPromptVisible(false);
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
            SetInteractionPromptVisible(false);

            if (conversationOwnedByThisStarter)
            {
                ResumeNpcMovement();
            }
        }

        private void EnsureInteractionPrompt()
        {
            if (interactionPromptRoot != null && interactionPromptText != null)
            {
                return;
            }

            Canvas promptCanvas = FindPromptCanvas();
            if (promptCanvas == null)
            {
                GameObject canvasObject = new("ConversationPromptCanvas");
                promptCanvas = canvasObject.AddComponent<Canvas>();
                promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            GameObject promptRootObject = new("ConversationPrompt");
            RectTransform promptRootRect = promptRootObject.AddComponent<RectTransform>();
            promptRootRect.SetParent(promptCanvas.transform, false);
            promptRootRect.anchorMin = new Vector2(0.5f, 0f);
            promptRootRect.anchorMax = new Vector2(0.5f, 0f);
            promptRootRect.pivot = new Vector2(0.5f, 0f);
            promptRootRect.anchoredPosition = interactionPromptPosition;
            promptRootRect.sizeDelta = new Vector2(460f, 76f);

            Image promptBackground = promptRootObject.AddComponent<Image>();
            promptBackground.color = new Color32(12, 18, 28, 220);

            Outline promptOutline = promptRootObject.AddComponent<Outline>();
            promptOutline.effectColor = new Color32(180, 140, 72, 180);
            promptOutline.effectDistance = new Vector2(2f, -2f);

            Shadow promptShadow = promptRootObject.AddComponent<Shadow>();
            promptShadow.effectColor = new Color32(0, 0, 0, 120);
            promptShadow.effectDistance = new Vector2(0f, -6f);

            GameObject keyBadgeObject = new("ConversationPromptKey");
            RectTransform keyBadgeRect = keyBadgeObject.AddComponent<RectTransform>();
            keyBadgeRect.SetParent(promptRootRect, false);
            keyBadgeRect.anchorMin = new Vector2(0f, 0.5f);
            keyBadgeRect.anchorMax = new Vector2(0f, 0.5f);
            keyBadgeRect.pivot = new Vector2(0f, 0.5f);
            keyBadgeRect.anchoredPosition = new Vector2(18f, 0f);
            keyBadgeRect.sizeDelta = new Vector2(56f, 44f);

            Image keyBadgeBackground = keyBadgeObject.AddComponent<Image>();
            keyBadgeBackground.color = new Color32(214, 179, 92, 255);

            GameObject keyTextObject = new("ConversationPromptKeyText");
            RectTransform keyTextRect = keyTextObject.AddComponent<RectTransform>();
            keyTextRect.SetParent(keyBadgeRect, false);
            keyTextRect.anchorMin = Vector2.zero;
            keyTextRect.anchorMax = Vector2.one;
            keyTextRect.offsetMin = Vector2.zero;
            keyTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI keyTextComponent = keyTextObject.AddComponent<TextMeshProUGUI>();
            keyTextComponent.alignment = TextAlignmentOptions.Center;
            keyTextComponent.fontSize = 28f;
            keyTextComponent.fontStyle = FontStyles.Bold;
            keyTextComponent.textWrappingMode = TextWrappingModes.NoWrap;
            keyTextComponent.raycastTarget = false;
            keyTextComponent.color = new Color32(32, 24, 12, 255);
            keyTextComponent.text = "E";

            GameObject promptTextObject = new("ConversationPromptText");
            RectTransform promptTextRect = promptTextObject.AddComponent<RectTransform>();
            promptTextRect.SetParent(promptRootRect, false);
            promptTextRect.anchorMin = Vector2.zero;
            promptTextRect.anchorMax = Vector2.one;
            promptTextRect.offsetMin = new Vector2(92f, 0f);
            promptTextRect.offsetMax = new Vector2(-20f, 0f);

            TextMeshProUGUI promptTextComponent = promptTextObject.AddComponent<TextMeshProUGUI>();
            promptTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
            promptTextComponent.fontSize = 24f;
            promptTextComponent.fontStyle = FontStyles.Bold;
            promptTextComponent.textWrappingMode = TextWrappingModes.NoWrap;
            promptTextComponent.raycastTarget = false;
            promptTextComponent.color = new Color32(245, 236, 214, 255);
            promptTextComponent.text = interactionPromptMessage;

            interactionPromptRoot = promptRootObject;
            interactionPromptText = promptTextComponent;
            createdRuntimePrompt = true;
        }

        private Canvas FindPromptCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                return canvas;
            }

            return null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            RefreshInteractionPrompt();

            if (!IsConversationAvailable())
            {
                return;
            }

            SetInteractionPromptVisible(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            SetInteractionPromptVisible(false);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            RefreshInteractionPrompt();

            bool canStartConversation = IsConversationAvailable();
            SetInteractionPromptVisible(canStartConversation);

            if (!canStartConversation)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                conversationOwnedByThisStarter = true;
                SetInteractionPromptVisible(false);
                ConversationManager.Instance.StartConversation(myConversation);
            }
        }

        private void HandleConversationStarted()
        {
            if (!conversationOwnedByThisStarter)
            {
                return;
            }

            SetInteractionPromptVisible(false);
            StopNpcMovement();
        }

        private void HandleConversationEnded()
        {
            if (!conversationOwnedByThisStarter)
            {
                return;
            }

            ResumeNpcMovement();
            conversationOwnedByThisStarter = false;
            SetInteractionPromptVisible(true);
        }

        private bool IsConversationAvailable()
        {
            return myConversation != null
                && ConversationManager.Instance != null
                && !ConversationManager.Instance.IsConversationActive;
        }

        private void RefreshInteractionPrompt()
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.text = interactionPromptMessage;
            }
        }

        private void SetInteractionPromptVisible(bool visible)
        {
            if (createdRuntimePrompt && interactionPromptRoot == null)
            {
                return;
            }

            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(visible);
            }
        }

        private void StopNpcMovement()
        {
            if (npcPatrol != null)
            {
                npcPatrol.SetMovementEnabled(false);
                return;
            }

            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
        }

        private void ResumeNpcMovement()
        {
            if (npcPatrol != null)
            {
                npcPatrol.SetMovementEnabled(true);
                return;
            }

            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = false;
            }
        }
    }
}
