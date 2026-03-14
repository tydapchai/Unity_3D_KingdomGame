using System;
using System.Collections.Generic;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.FantasyKingdom
{
    public class Inventory : MonoBehaviour
    {
        public static bool IsInputBlocked { get; private set; }

        public ItemSO woodItem;
        public ItemSO axeItem;
        public GameObject hotbarObj;
        public GameObject inventorySlotParent;
        public GameObject container;
        public Image dragIcon;
        public float pickupRange = 3f;
        [SerializeField] private float dropForwardDistance = 1.2f;
        [SerializeField] private float dropHeightOffset = 0.75f;
        [SerializeField] private float dropClearanceBuffer = 0.35f;
        [SerializeField] private float droppedItemPickupGraceDuration = 0.2f;
        private Item lockedItem = null;
        private Item recentlyDroppedItem = null;
        private float recentlyDroppedItemIgnoreUntil = -1f;
        public Material highlightMaterial;
        private readonly RaycastHit[] itemDetectionHits = new RaycastHit[16];
        private readonly Collider[] nearbyItemColliders = new Collider[32];
        private readonly List<Item> candidateItems = new List<Item>();
        private readonly List<Renderer> lookedAtRenderers = new List<Renderer>();
        private readonly List<Material[]> originalRendererMaterials = new List<Material[]>();
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);
        private EventSystem fallbackEventSystem;
        private bool createdFallbackEventSystem;
        private Camera cachedPickupCamera;
        private static readonly KeyCode[] HotbarKeyCodes =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6
        };
        private static readonly KeyCode[] HotbarNumpadKeyCodes =
        {
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6
        };
        private Transform cachedPlayerTransform;
        [SerializeField] private KeyCode inventoryToggleKey = KeyCode.B;
        [SerializeField] private KeyCode inventoryCursorToggleKey = KeyCode.Escape;

        private int equippedHotBarIndex = 0;
        public float equippedOpacity = 0.9f;
        public float normalOpacity = 0.58f;
        [SerializeField] private Color equippedSlotColor = new Color(1f, 0.92f, 0.65f, 0.9f);
        [SerializeField] private Color normalSlotColor = new Color(1f, 1f, 1f, 0.58f);
        [SerializeField] private string handAttachBoneName = "Wrist_R";
        [SerializeField] private string defaultHeldVisualName = "FREE GREAT SWORD 3 COLOR 2";
        [SerializeField] private bool hideDefaultHeldVisualWhenNoItem = true;
        private List<Slot> inventorySlots = new List<Slot>();
        private List<Slot> hotbarSlots = new List<Slot>();
        private List<Slot> allSlots = new List<Slot>();
        private List<DisabledBehaviourState> disabledCameraBehaviours = new List<DisabledBehaviourState>();

        private Slot draggedSlot = null;
        private bool isDragging = false;
        private bool isInventoryInteractionActive = false;
        private Transform cachedHandAttachPoint;
        private GameObject cachedDefaultHeldVisual;
        private GameObject equippedHandItemInstance;
        private ItemSO currentEquippedHandItem;
        private int currentEquippedHandSlotIndex = -1;
        private int currentEquippedHandAmount = -1;
        private Vector3 equippedHandLocalPosition;
        private Quaternion equippedHandLocalRotation = Quaternion.identity;
        private Vector3 equippedHandLocalScale = Vector3.one;
        private bool hasEquippedHandPose;

        private struct DisabledBehaviourState
        {
            public Behaviour behaviour;
            public bool wasEnabled;
        }

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit left, RaycastHit right)
            {
                return left.distance.CompareTo(right.distance);
            }
        }

        private void Awake()
        {
            CacheSlots();
            ResolveDragIcon();
        }

        private void Start()
        {
            if (!OwnsPlayerInput())
            {
                return;
            }

            SyncInventoryState();
            UpdateHotBarOpacity();
            RefreshEquippedHandItem(forceRefresh: true);
        }

        private void OnDisable()
        {
            if (!OwnsPlayerInput())
            {
                return;
            }

            ReleaseGameplayLock();
        }

        void Update()
        {
            if (!OwnsPlayerInput())
            {
                return;
            }

            if (Input.GetKeyDown(inventoryToggleKey))
            {
                ToggleInventory();
            }

            if (Input.GetKeyDown(inventoryCursorToggleKey) && IsInventoryOpen())
            {
                SetInventoryOpen(false);
            }

            if (!isInventoryInteractionActive)
            {
                HandleHotBarSelection();
                HandleDropEquippedItem();
                RefreshEquippedHandItem();
                UpdateHotBarOpacity();
                DetectLookedAtItem();
                Pickup();
                return;
            }

            EnsureInventoryCursorState();
            StartDrag();
            UpdateDragItemPosition();
            EndDrag();
            RefreshEquippedHandItem();
            UpdateHotBarOpacity();
        }

        public int AddItem(ItemSO itemToAdd, int amount)
        {
            if (itemToAdd == null)
            {
                Debug.LogError("Cannot add a null item to the inventory.");
                return 0;
            }

            if (amount <= 0)
            {
                return 0;
            }

            if (itemToAdd.maxStackSize <= 0)
            {
                Debug.LogError($"Item '{itemToAdd.name}' has an invalid max stack size: {itemToAdd.maxStackSize}.");
                return 0;
            }

            if (allSlots.Count == 0)
            {
                CacheSlots();
            }

            int remaining = amount;
            AddItemToMatchingSlots(hotbarSlots, itemToAdd, ref remaining);
            AddItemToEmptySlots(hotbarSlots, itemToAdd, ref remaining);
            AddItemToMatchingSlots(inventorySlots, itemToAdd, ref remaining);
            AddItemToEmptySlots(inventorySlots, itemToAdd, ref remaining);

            if (remaining > 0)
            {
                Debug.Log("Inventory is full, could not add " + remaining + " of " + itemToAdd.name);
            }

            return amount - remaining;
        }

        private static void AddItemToMatchingSlots(List<Slot> slots, ItemSO itemToAdd, ref int remaining)
        {
            if (remaining <= 0)
            {
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];
                if (slot == null || !slot.HasItem() || slot.GetItem() != itemToAdd)
                {
                    continue;
                }

                int currentAmount = slot.GetAmount();
                int maxStack = itemToAdd.maxStackSize;
                if (currentAmount >= maxStack)
                {
                    continue;
                }

                int spaceLeft = maxStack - currentAmount;
                int amountToAdd = Mathf.Min(spaceLeft, remaining);
                slot.SetItem(itemToAdd, currentAmount + amountToAdd);
                remaining -= amountToAdd;
                if (remaining <= 0)
                {
                    return;
                }
            }
        }

        private static void AddItemToEmptySlots(List<Slot> slots, ItemSO itemToAdd, ref int remaining)
        {
            if (remaining <= 0)
            {
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];
                if (slot == null || slot.HasItem())
                {
                    continue;
                }

                int amountToPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                slot.SetItem(itemToAdd, amountToPlace);
                remaining -= amountToPlace;
                if (remaining <= 0)
                {
                    return;
                }
            }
        }

        private static void SortSlotsByHierarchyOrder(List<Slot> slots)
        {
            slots.Sort(CompareSlotsByHierarchyOrder);
        }

        private static int CompareSlotsByHierarchyOrder(Slot left, Slot right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            List<int> leftPath = GetSiblingIndexPath(left.transform);
            List<int> rightPath = GetSiblingIndexPath(right.transform);
            int minDepth = Mathf.Min(leftPath.Count, rightPath.Count);
            for (int i = 0; i < minDepth; i++)
            {
                if (leftPath[i] != rightPath[i])
                {
                    return leftPath[i].CompareTo(rightPath[i]);
                }
            }

            return leftPath.Count.CompareTo(rightPath.Count);
        }

        private static List<int> GetSiblingIndexPath(Transform target)
        {
            List<int> path = new List<int>();
            while (target != null)
            {
                path.Add(target.GetSiblingIndex());
                target = target.parent;
            }

            path.Reverse();
            return path;
        }

        private void CacheSlots()
        {
            inventorySlots.Clear();
            hotbarSlots.Clear();
            allSlots.Clear();

            if (inventorySlotParent == null || hotbarObj == null)
            {
                Debug.LogError("Inventory references are missing. Assign inventorySlotParent and hotbarObj in the inspector.");
                return;
            }

            inventorySlots.AddRange(inventorySlotParent.GetComponentsInChildren<Slot>(true));
            hotbarSlots.AddRange(hotbarObj.GetComponentsInChildren<Slot>(true));
            SortSlotsByHierarchyOrder(inventorySlots);
            SortSlotsByHierarchyOrder(hotbarSlots);

            allSlots.AddRange(hotbarSlots);
            allSlots.AddRange(inventorySlots);
            if (hotbarSlots.Count > 0)
            {
                equippedHotBarIndex = Mathf.Clamp(equippedHotBarIndex, 0, hotbarSlots.Count - 1);
            }
        }

        private void StartDrag()
        {
            if (!Input.GetMouseButtonDown(0) || dragIcon == null)
            {
                return;
            }

            Slot hovered = GetHoveredSlot();
            if (hovered != null && hovered.HasItem())
            {
                draggedSlot = hovered;
                isDragging = true;
                dragIcon.sprite = hovered.GetItem().icon;
                dragIcon.color = new Color(1, 1, 1, 0.5f);
                dragIcon.enabled = true;
            }
        }

        private void EndDrag()
        {
            if (!Input.GetMouseButtonUp(0) || !isDragging)
            {
                return;
            }

            Slot hovered = GetHoveredSlot();
            if (hovered != null)
            {
                HandleDrop(draggedSlot, hovered);
            }

            StopDrag();
        }

        private Slot GetHoveredSlot()
        {
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                uiRaycastResults.Clear();
                EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
                for (int i = 0; i < uiRaycastResults.Count; i++)
                {
                    GameObject hitObject = uiRaycastResults[i].gameObject;
                    if (hitObject == null)
                    {
                        continue;
                    }

                    Slot slot = hitObject.GetComponentInParent<Slot>();
                    if (slot != null && allSlots.Contains(slot))
                    {
                        return slot;
                    }
                }
            }

            foreach (Slot s in allSlots)
            {
                if (s.hovering)
                {
                    return s;
                }
            }
            return null;
        }

        private void HandleDrop(Slot from, Slot to)
        {
            if (from == to) return;
            if (to.HasItem() && to.GetItem() == from.GetItem())
            {
                int max = to.GetItem().maxStackSize;
                int space = max - to.GetAmount();

                if (space > 0)
                {
                    int move = Mathf.Min(space, from.GetAmount());
                    to.SetItem(to.GetItem(), to.GetAmount() + move);
                    from.SetItem(from.GetItem(), from.GetAmount() - move);

                    if (from.GetAmount() <= 0)
                    {
                        from.ClearSlot();
                    }

                    return;
                }
            }

            if (to.HasItem())
            {
                ItemSO tempItem = to.GetItem();
                int tempAmount = to.GetAmount();

                to.SetItem(from.GetItem(), from.GetAmount());
                from.SetItem(tempItem, tempAmount);
                return;
            }

            to.SetItem(from.GetItem(), from.GetAmount());
            from.ClearSlot();
        }

        private void UpdateDragItemPosition()
        {
            if (isDragging)
            {
                dragIcon.transform.position = Input.mousePosition;
            }
        }

        private void ResolveDragIcon()
        {
            if (dragIcon == null)
            {
                foreach (Image image in GetComponentsInChildren<Image>(true))
                {
                    if (image.gameObject.name == "DragItem")
                    {
                        dragIcon = image;
                        break;
                    }
                }
            }

            if (dragIcon != null)
            {
                dragIcon.raycastTarget = false;
            }
        }

        private void SetInventoryInteraction(bool isActive)
        {
            if (isInventoryInteractionActive == isActive &&
                IsInputBlocked == isActive)
            {
                if (isActive)
                {
                    EnsureEventSystemForInventory();
                    EnsureInventoryCursorState();
                }

                return;
            }

            isInventoryInteractionActive = isActive;
            IsInputBlocked = isActive;
            bool gameplayEnabled = !isActive;

            if (isActive)
            {
                ClearLookedAtItemHighlight();
                EnsureEventSystemForInventory();
            }

            ApplyStarterAssetsInputState(gameplayEnabled);
            ApplyCinemachineInventoryState(isActive);

            if (isActive)
            {
                EnsureInventoryCursorState();
            }
            else
            {
                RestoreEventSystemAfterInventory();
                StopDrag();
            }
        }

        private void ApplyStarterAssetsInputState(bool gameplayEnabled)
        {
            StarterAssetsInputs[] inputs = FindObjectsByType<StarterAssetsInputs>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (inputs.Length == 0)
            {
                Cursor.lockState = gameplayEnabled ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !gameplayEnabled;
                return;
            }

            foreach (StarterAssetsInputs input in inputs)
            {
                if (input == null)
                {
                    continue;
                }

                input.cursorInputForLook = gameplayEnabled;
                input.MoveInput(Vector2.zero);
                input.LookInput(Vector2.zero);
                input.JumpInput(false);
                input.SprintInput(false);
                input.SetCursorState(gameplayEnabled);
            }
        }

        private void ApplyCinemachineInventoryState(bool isInventoryOpen)
        {
            if (!isInventoryOpen)
            {
                RestoreCameraBehaviours();
                return;
            }

            disabledCameraBehaviours.Clear();
            CacheAndDisableBehaviours(FindObjectsByType<CinemachineFreeLook>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None));
            CacheAndDisableBehaviours(FindObjectsByType<CinemachineInputProvider>(
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

                disabledCameraBehaviours.Add(new DisabledBehaviourState
                {
                    behaviour = behaviour,
                    wasEnabled = behaviour.enabled
                });
                behaviour.enabled = false;
            }
        }

        private void RestoreCameraBehaviours()
        {
            foreach (DisabledBehaviourState state in disabledCameraBehaviours)
            {
                if (state.behaviour != null)
                {
                    state.behaviour.enabled = state.wasEnabled;
                }
            }

            disabledCameraBehaviours.Clear();
        }

        private void EnsureInventoryCursorState()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void EnsureEventSystemForInventory()
        {
            if (EventSystem.current != null && EventSystem.current.gameObject.activeInHierarchy)
            {
                return;
            }

            if (createdFallbackEventSystem && fallbackEventSystem != null)
            {
                if (!fallbackEventSystem.gameObject.activeSelf)
                {
                    fallbackEventSystem.gameObject.SetActive(true);
                }

                return;
            }

            GameObject eventSystemObject = new GameObject("InventoryEventSystem");
            fallbackEventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            createdFallbackEventSystem = true;
        }

        private void RestoreEventSystemAfterInventory()
        {
            if (!createdFallbackEventSystem || fallbackEventSystem == null)
            {
                return;
            }

            Destroy(fallbackEventSystem.gameObject);
            fallbackEventSystem = null;
            createdFallbackEventSystem = false;
        }

        private void ReleaseGameplayLock()
        {
            if (!isInventoryInteractionActive && !IsInputBlocked &&
                disabledCameraBehaviours.Count == 0)
            {
                return;
            }

            RestoreCameraBehaviours();
            RestoreEventSystemAfterInventory();
            IsInputBlocked = false;
            isInventoryInteractionActive = false;
            ApplyStarterAssetsInputState(true);
            StopDrag();
        }

        private void StopDrag()
        {
            if (dragIcon != null)
            {
                dragIcon.enabled = false;
                dragIcon.sprite = null;
            }

            draggedSlot = null;
            isDragging = false;
        }

        private void Pickup()
        {
            if (!Input.GetKeyDown(KeyCode.E))
            {
                return;
            }

            Transform playerRoot = ResolvePlayerRoot();
            Camera activeCamera = ResolvePickupCamera();
            if (!TryResolvePickupTarget(activeCamera, playerRoot, out Item itemToPickup))
            {
                return;
            }

            int pickedUpAmount = AddItem(itemToPickup.item, itemToPickup.amount);
            if (pickedUpAmount <= 0)
            {
                return;
            }

            if (pickedUpAmount >= itemToPickup.amount)
            {
                Destroy(itemToPickup.gameObject);
                if (lockedItem == itemToPickup)
                {
                    lockedItem = null;
                }

                return;
            }

            itemToPickup.amount -= pickedUpAmount;
        }

        private void DetectLookedAtItem()
        {
            ClearLookedAtItemHighlight();

            Camera activeCamera = ResolvePickupCamera();
            if (isInventoryInteractionActive || activeCamera == null)
            {
                return;
            }

            Transform playerRoot = ResolvePlayerRoot();
            if (!TryGetLookedAtItem(activeCamera, playerRoot, out Item item))
            {
                return;
            }

            lockedItem = item;
            ApplyLookedAtItemHighlight(item);
        }

        private void ClearLookedAtItemHighlight()
        {
            for (int i = 0; i < lookedAtRenderers.Count; i++)
            {
                Renderer renderer = lookedAtRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.sharedMaterials = originalRendererMaterials[i];
            }

            lookedAtRenderers.Clear();
            originalRendererMaterials.Clear();
            lockedItem = null;
        }

        private bool TryGetLookedAtItem(Camera activeCamera, Transform playerRoot, out Item item)
        {
            int layerMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");
            Ray centerRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (TryGetItemFromRay(centerRay, GetPickupRayDistance(activeCamera, playerRoot), layerMask, playerRoot, out item))
            {
                return true;
            }

            return TryGetItemNearScreenCenter(activeCamera, playerRoot, out item);
        }

        private bool TryGetItemFromRay(
            Ray ray,
            float rayDistance,
            int layerMask,
            Transform playerRoot,
            out Item item)
        {
            item = null;
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                itemDetectionHits,
                rayDistance,
                layerMask,
                QueryTriggerInteraction.Collide);

            if (hitCount <= 0)
            {
                return false;
            }

            Array.Sort(itemDetectionHits, 0, hitCount, RaycastHitDistanceComparer.Instance);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = itemDetectionHits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (playerRoot != null && hit.collider.transform.root == playerRoot)
                {
                    continue;
                }

                Item hitItem = hit.collider.GetComponentInParent<Item>();
                if (!IsItemPickupable(hitItem))
                {
                    if (hit.collider.isTrigger)
                    {
                        continue;
                    }

                    return false;
                }

                if (IsPickupTemporarilyBlocked(hitItem))
                {
                    continue;
                }

                if (!IsItemWithinPickupRange(hitItem, playerRoot))
                {
                    continue;
                }

                if (!TryGetItemBounds(hitItem, out Bounds bounds))
                {
                    continue;
                }

                item = hitItem;
                return true;
            }

            return false;
        }

        private bool TryGetItemNearScreenCenter(Camera activeCamera, Transform playerRoot, out Item item)
        {
            item = null;
            Vector3 overlapOrigin = playerRoot != null ? playerRoot.position : activeCamera.transform.position;
            int layerMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");
            int colliderCount = Physics.OverlapSphereNonAlloc(
                overlapOrigin,
                pickupRange,
                nearbyItemColliders,
                layerMask,
                QueryTriggerInteraction.Collide);

            if (colliderCount <= 0)
            {
                return false;
            }

            candidateItems.Clear();
            float bestScore = float.MaxValue;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider nearbyCollider = nearbyItemColliders[i];
                if (nearbyCollider == null)
                {
                    continue;
                }

                Item candidate = nearbyCollider.GetComponentInParent<Item>();
                if (!IsItemPickupable(candidate) ||
                    candidateItems.Contains(candidate) ||
                    IsPickupTemporarilyBlocked(candidate) ||
                    !IsItemWithinPickupRange(candidate, playerRoot))
                {
                    continue;
                }

                candidateItems.Add(candidate);
                if (!TryGetItemBounds(candidate, out Bounds bounds))
                {
                    continue;
                }

                Vector3 targetPoint = GetPreferredTargetPoint(bounds, activeCamera);
                Vector3 viewportPoint = activeCamera.WorldToViewportPoint(targetPoint);
                if (viewportPoint.z <= 0f)
                {
                    continue;
                }

                float viewportOffset = Vector2.Distance(
                    new Vector2(viewportPoint.x, viewportPoint.y),
                    new Vector2(0.5f, 0.5f));
                if (viewportOffset > 0.35f)
                {
                    continue;
                }

                if (!HasLineOfSight(activeCamera, targetPoint, candidate, playerRoot))
                {
                    continue;
                }

                float distanceScore = playerRoot != null
                    ? GetDistanceToItem(candidate, playerRoot.position)
                    : GetDistanceToItem(candidate, activeCamera.transform.position);
                float score = viewportOffset + distanceScore * 0.1f;
                if (score < bestScore)
                {
                    bestScore = score;
                    item = candidate;
                }
            }

            return item != null;
        }

        private Camera ResolvePickupCamera()
        {
            if (cachedPickupCamera != null && cachedPickupCamera.isActiveAndEnabled)
            {
                return cachedPickupCamera;
            }

            if (Camera.main != null && Camera.main.isActiveAndEnabled)
            {
                cachedPickupCamera = Camera.main;
                return cachedPickupCamera;
            }

            Camera localCamera = GetComponentInChildren<Camera>(true);
            if (localCamera != null && localCamera.isActiveAndEnabled)
            {
                cachedPickupCamera = localCamera;
                return cachedPickupCamera;
            }

#if UNITY_2023_1_OR_NEWER
            cachedPickupCamera = FindFirstObjectByType<Camera>();
#else
            cachedPickupCamera = FindObjectOfType<Camera>();
#endif
            return cachedPickupCamera;
        }

        private static Vector3 GetPreferredTargetPoint(Bounds bounds, Camera activeCamera)
        {
            if (activeCamera == null)
            {
                return bounds.center;
            }

            Vector3 preferredPoint = bounds.ClosestPoint(activeCamera.transform.position);
            return preferredPoint == activeCamera.transform.position
                ? bounds.center
                : preferredPoint;
        }

        private bool TryResolvePickupTarget(Camera activeCamera, Transform playerRoot, out Item item)
        {
            item = null;
            if (IsItemPickupable(lockedItem) &&
                !IsPickupTemporarilyBlocked(lockedItem) &&
                IsItemWithinPickupRange(lockedItem, playerRoot))
            {
                item = lockedItem;
                return true;
            }

            if (activeCamera != null && TryGetLookedAtItem(activeCamera, playerRoot, out item))
            {
                return true;
            }

            return TryGetClosestPickupableItem(playerRoot, out item);
        }

        private bool TryGetClosestPickupableItem(Transform playerRoot, out Item item)
        {
            item = null;
            Vector3 origin = playerRoot != null ? playerRoot.position : transform.position;
            float bestDistance = float.MaxValue;

#if UNITY_2023_1_OR_NEWER
            Item[] items = FindObjectsByType<Item>(FindObjectsSortMode.None);
#else
            Item[] items = FindObjectsOfType<Item>();
#endif
            for (int i = 0; i < items.Length; i++)
            {
                Item candidate = items[i];
                if (!IsItemPickupable(candidate) ||
                    IsPickupTemporarilyBlocked(candidate) ||
                    !IsItemWithinPickupRange(candidate, playerRoot))
                {
                    continue;
                }

                float distance = GetDistanceToItem(candidate, origin);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    item = candidate;
                }
            }

            return item != null;
        }

        private static bool IsItemPickupable(Item item)
        {
            return item != null &&
                   item.isActiveAndEnabled &&
                   item.gameObject.activeInHierarchy &&
                   item.item != null &&
                   item.amount > 0;
        }

        private bool HasLineOfSight(Camera activeCamera, Vector3 targetPoint, Item item, Transform playerRoot)
        {
            Vector3 origin = activeCamera.transform.position;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return false;
            }

            int layerMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction.normalized,
                itemDetectionHits,
                distance,
                layerMask,
                QueryTriggerInteraction.Collide);
            if (hitCount <= 0)
            {
                return false;
            }

            Array.Sort(itemDetectionHits, 0, hitCount, RaycastHitDistanceComparer.Instance);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = itemDetectionHits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (playerRoot != null && hit.collider.transform.root == playerRoot)
                {
                    continue;
                }

                Item hitItem = hit.collider.GetComponentInParent<Item>();
                if (IsPickupTemporarilyBlocked(hitItem))
                {
                    continue;
                }

                if (hitItem == item)
                {
                    return true;
                }

                if (hitItem != null || hit.collider.isTrigger)
                {
                    continue;
                }

                return false;
            }

            return false;
        }

        private void ApplyLookedAtItemHighlight(Item item)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !ShouldHighlightRenderer(renderer))
                {
                    continue;
                }

                lookedAtRenderers.Add(renderer);
                originalRendererMaterials.Add(renderer.sharedMaterials);

                if (highlightMaterial == null)
                {
                    continue;
                }

                Material[] highlightMaterials = new Material[renderer.sharedMaterials.Length];
                for (int j = 0; j < highlightMaterials.Length; j++)
                {
                    highlightMaterials[j] = highlightMaterial;
                }

                renderer.sharedMaterials = highlightMaterials;
            }
        }

        private static bool ShouldHighlightRenderer(Renderer renderer)
        {
            return renderer is not ParticleSystemRenderer &&
                   renderer is not TrailRenderer &&
                   renderer is not LineRenderer;
        }

        private bool TryGetItemBounds(Item item, out Bounds bounds)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(false);
            bool foundBounds = false;
            bounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!foundBounds)
                {
                    bounds = renderer.bounds;
                    foundBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return foundBounds;
        }

        private Transform ResolvePlayerRoot()
        {
            if (cachedPlayerTransform != null && cachedPlayerTransform.gameObject.activeInHierarchy)
            {
                return cachedPlayerTransform.root;
            }

            Transform localRoot = transform.root;
            if (localRoot != null)
            {
                cachedPlayerTransform = localRoot;
                return cachedPlayerTransform;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return null;
            }

            cachedPlayerTransform = playerObject.transform.root;
            return cachedPlayerTransform;
        }

        private bool OwnsPlayerInput()
        {
            Transform playerRoot = ResolvePlayerRoot();
            if (playerRoot == null)
            {
                return true;
            }

            Inventory[] playerInventories = playerRoot.GetComponentsInChildren<Inventory>(true);
            for (int i = 0; i < playerInventories.Length; i++)
            {
                Inventory inventory = playerInventories[i];
                if (inventory == null || !inventory.enabled || !inventory.gameObject.activeInHierarchy)
                {
                    continue;
                }

                return inventory == this;
            }

            return true;
        }

        private float GetPickupRayDistance(Camera activeCamera, Transform playerRoot)
        {
            if (activeCamera == null || playerRoot == null)
            {
                return pickupRange;
            }

            return pickupRange + Vector3.Distance(activeCamera.transform.position, playerRoot.position);
        }

        private bool IsItemWithinPickupRange(Item item, Transform playerRoot)
        {
            if (item == null || playerRoot == null)
            {
                return true;
            }

            return GetDistanceToItem(item, playerRoot.position) <= pickupRange;
        }

        private float GetDistanceToItem(Item item, Vector3 origin)
        {
            if (item == null)
            {
                return float.MaxValue;
            }

            if (TryGetItemBounds(item, out Bounds bounds))
            {
                return Vector3.Distance(origin, bounds.ClosestPoint(origin));
            }

            return Vector3.Distance(origin, item.transform.position);
        }

        private void ToggleInventory()
        {
            SetInventoryOpen(!IsInventoryOpen());
        }

        private void SetInventoryOpen(bool isOpen)
        {
            if (container != null)
            {
                container.SetActive(isOpen);
            }

            SetInventoryInteraction(isOpen);
        }

        private bool IsInventoryOpen()
        {
            return container != null && container.activeInHierarchy;
        }

        private void SyncInventoryState()
        {
            SetInventoryInteraction(IsInventoryOpen());
        }

        private void UpdateHotBarOpacity()
        {
            if (hotbarSlots.Count == 0)
            {
                return;
            }

            equippedHotBarIndex = Mathf.Clamp(equippedHotBarIndex, 0, hotbarSlots.Count - 1);
            for (int i = 0; i < hotbarSlots.Count; i++)
            {
                Image slotBackground = hotbarSlots[i].GetComponent<Image>();
                if (slotBackground != null)
                {
                    Color targetColor = i == equippedHotBarIndex ? equippedSlotColor : normalSlotColor;
                    targetColor.a = i == equippedHotBarIndex ? equippedOpacity : normalOpacity;
                    slotBackground.color = targetColor;
                }

                if (hotbarSlots[i].iconImage != null)
                {
                    Color iconColor = hotbarSlots[i].iconImage.color;
                    iconColor.a = hotbarSlots[i].HasItem() ? 1f : 0f;
                    hotbarSlots[i].iconImage.color = iconColor;
                }
            }
        }

        private void HandleHotBarSelection()
        {
            int slotCount = Mathf.Min(hotbarSlots.Count, HotbarKeyCodes.Length);
            for (int i = 0; i < slotCount; i++)
            {
                if (Input.GetKeyDown(HotbarKeyCodes[i]) || Input.GetKeyDown(HotbarNumpadKeyCodes[i]))
                {
                    equippedHotBarIndex = i;
                    return;
                }
            }
        }

        private void HandleDropEquippedItem()
        {
            if (!Input.GetKeyDown(KeyCode.G) || hotbarSlots.Count == 0)
            {
                return;
            }

            equippedHotBarIndex = Mathf.Clamp(equippedHotBarIndex, 0, hotbarSlots.Count - 1);
            Slot equippedSlot = hotbarSlots[equippedHotBarIndex];
            if (!equippedSlot.HasItem())
            {
                return;
            }

            ItemSO itemSO = equippedSlot.GetItem();
            GameObject prefab = itemSO.itemPrefab;

            if (prefab == null)
            {
                Debug.LogWarning($"Item '{itemSO.name}' does not have an item prefab to drop.");
                return;
            }

            Transform playerRoot = ResolvePlayerRoot();
            Vector3 dropForward = GetDropForward();
            Vector3 dropPosition = GetDropSpawnPosition();
            Quaternion dropRotation = Camera.main != null
                ? Quaternion.LookRotation(dropForward)
                : Quaternion.identity;

            GameObject dropped = Instantiate(prefab, dropPosition, dropRotation);
            PrepareDroppedItemForPickup(dropped, itemSO, equippedSlot.GetAmount());
            MoveDroppedItemClearOfPlayer(dropped, playerRoot, dropForward);
            TrackRecentlyDroppedItem(dropped);

            equippedSlot.ClearSlot();
            UpdateHotBarOpacity();
        }

        private static void PrepareDroppedItemForPickup(GameObject droppedObject, ItemSO itemSO, int amount)
        {
            if (droppedObject == null || itemSO == null)
            {
                return;
            }

            CollectibleItem[] collectibleItems = droppedObject.GetComponentsInChildren<CollectibleItem>(true);
            for (int i = 0; i < collectibleItems.Length; i++)
            {
                if (collectibleItems[i] != null)
                {
                    collectibleItems[i].enabled = false;
                }
            }

            Item rootItem = droppedObject.GetComponent<Item>();
            if (rootItem == null)
            {
                rootItem = droppedObject.AddComponent<Item>();
            }

            Item[] itemComponents = droppedObject.GetComponentsInChildren<Item>(true);
            for (int i = 0; i < itemComponents.Length; i++)
            {
                if (itemComponents[i] == null)
                {
                    continue;
                }

                itemComponents[i].item = itemSO;
                itemComponents[i].amount = amount;
            }
        }

        private void MoveDroppedItemClearOfPlayer(GameObject droppedObject, Transform playerRoot, Vector3 dropForward)
        {
            if (droppedObject == null || playerRoot == null || dropForward.sqrMagnitude < 0.01f)
            {
                return;
            }

            if (!TryGetObjectBounds(droppedObject, includeTriggers: true, out Bounds droppedBounds))
            {
                return;
            }

            if (!TryGetObjectBounds(playerRoot.gameObject, includeTriggers: false, out Bounds playerBounds))
            {
                playerBounds = new Bounds(playerRoot.position + Vector3.up * 0.9f, new Vector3(0.8f, 1.8f, 0.8f));
            }

            float currentSeparation = Vector3.Dot(droppedBounds.center - playerBounds.center, dropForward);
            float desiredSeparation =
                GetBoundsExtentAlongAxis(playerBounds, dropForward) +
                GetBoundsExtentAlongAxis(droppedBounds, dropForward) +
                dropClearanceBuffer;
            float pushDistance = desiredSeparation - currentSeparation;
            if (pushDistance <= 0f)
            {
                return;
            }

            droppedObject.transform.position += dropForward * pushDistance;
        }

        private void TrackRecentlyDroppedItem(GameObject droppedObject)
        {
            if (droppedObject == null)
            {
                return;
            }

            Item droppedItem = droppedObject.GetComponent<Item>();
            if (droppedItem == null)
            {
                return;
            }

            recentlyDroppedItem = droppedItem;
            recentlyDroppedItemIgnoreUntil = Time.time + droppedItemPickupGraceDuration;
        }

        private bool IsPickupTemporarilyBlocked(Item item)
        {
            if (item == null || recentlyDroppedItem == null)
            {
                return false;
            }

            if (Time.time >= recentlyDroppedItemIgnoreUntil || recentlyDroppedItem == null)
            {
                recentlyDroppedItem = null;
                recentlyDroppedItemIgnoreUntil = -1f;
                return false;
            }

            return item == recentlyDroppedItem;
        }

        private static bool TryGetObjectBounds(GameObject targetObject, bool includeTriggers, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!includeTriggers && collider.isTrigger)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(collider.bounds);
            }

            if (hasBounds)
            {
                return true;
            }

            Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private static float GetBoundsExtentAlongAxis(Bounds bounds, Vector3 axis)
        {
            Vector3 normalizedAxis = axis.normalized;
            Vector3 absoluteAxis = new Vector3(
                Mathf.Abs(normalizedAxis.x),
                Mathf.Abs(normalizedAxis.y),
                Mathf.Abs(normalizedAxis.z));
            return Vector3.Dot(bounds.extents, absoluteAxis);
        }

        private void RefreshEquippedHandItem(bool forceRefresh = false)
        {
            if (hotbarSlots.Count == 0)
            {
                ApplyEquippedHandItem(null, -1, 0);
                return;
            }

            equippedHotBarIndex = Mathf.Clamp(equippedHotBarIndex, 0, hotbarSlots.Count - 1);
            Slot equippedSlot = hotbarSlots[equippedHotBarIndex];
            ItemSO equippedItem = equippedSlot.HasItem() ? equippedSlot.GetItem() : null;
            int equippedAmount = equippedSlot.HasItem() ? equippedSlot.GetAmount() : 0;

            if (!forceRefresh &&
                currentEquippedHandItem == equippedItem &&
                currentEquippedHandSlotIndex == equippedHotBarIndex &&
                currentEquippedHandAmount == equippedAmount &&
                (equippedItem == null || equippedHandItemInstance != null))
            {
                return;
            }

            ApplyEquippedHandItem(equippedItem, equippedHotBarIndex, equippedAmount);
        }

        private void ApplyEquippedHandItem(ItemSO itemToEquip, int slotIndex, int amount)
        {
            currentEquippedHandItem = itemToEquip;
            currentEquippedHandSlotIndex = slotIndex;
            currentEquippedHandAmount = amount;

            Transform attachPoint = ResolveHandAttachPoint();
            GameObject defaultHeldVisual = ResolveDefaultHeldVisual();

            if (equippedHandItemInstance != null)
            {
                Destroy(equippedHandItemInstance);
                equippedHandItemInstance = null;
            }

            if (defaultHeldVisual != null)
            {
                defaultHeldVisual.SetActive(!hideDefaultHeldVisualWhenNoItem && itemToEquip == null);
            }

            if (attachPoint == null || itemToEquip == null)
            {
                return;
            }

            GameObject handVisualPrefab = ResolveHandVisualPrefab(itemToEquip);
            if (handVisualPrefab == null)
            {
                return;
            }

            if (defaultHeldVisual != null)
            {
                defaultHeldVisual.SetActive(false);
            }

            equippedHandItemInstance = Instantiate(handVisualPrefab, attachPoint, false);
            equippedHandItemInstance.name = $"{handVisualPrefab.name}_Equipped";
            ApplyEquippedHandPose(equippedHandItemInstance.transform);
            PrepareHandVisualInstance(equippedHandItemInstance, itemToEquip.handItemPrefab == null);
        }

        private GameObject ResolveHandVisualPrefab(ItemSO item)
        {
            if (item == null)
            {
                return null;
            }

            if (item.handItemPrefab != null)
            {
                return item.handItemPrefab;
            }

            return item.itemPrefab;
        }

        private void PrepareHandVisualInstance(GameObject handVisualInstance, bool isFallbackVisual)
        {
            if (handVisualInstance == null)
            {
                return;
            }

            foreach (Collider collider in handVisualInstance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in handVisualInstance.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.detectCollisions = false;
            }

            foreach (MonoBehaviour behaviour in handVisualInstance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            if (!isFallbackVisual)
            {
                return;
            }

            foreach (Transform child in handVisualInstance.GetComponentsInChildren<Transform>(true))
            {
                if (child == handVisualInstance.transform)
                {
                    continue;
                }

                if (child.name.Equals("Quad", StringComparison.OrdinalIgnoreCase) ||
                    child.name.Equals("Cube", StringComparison.OrdinalIgnoreCase))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void ApplyEquippedHandPose(Transform equippedTransform)
        {
            if (equippedTransform == null)
            {
                return;
            }

            Vector3 prefabLocalPosition = equippedTransform.localPosition;
            Quaternion prefabLocalRotation = equippedTransform.localRotation;
            Vector3 prefabLocalScale = equippedTransform.localScale;

            if (!hasEquippedHandPose)
            {
                equippedTransform.localPosition = prefabLocalPosition;
                equippedTransform.localRotation = prefabLocalRotation;
                equippedTransform.localScale = prefabLocalScale;
                return;
            }

            equippedTransform.localPosition = equippedHandLocalPosition + prefabLocalPosition;
            equippedTransform.localRotation = equippedHandLocalRotation * prefabLocalRotation;
            equippedTransform.localScale = Vector3.Scale(equippedHandLocalScale, prefabLocalScale);
        }

        private Transform ResolveHandAttachPoint()
        {
            if (cachedHandAttachPoint != null)
            {
                return cachedHandAttachPoint;
            }

            GameObject defaultHeldVisual = ResolveDefaultHeldVisual();
            if (defaultHeldVisual != null && defaultHeldVisual.transform.parent != null)
            {
                cachedHandAttachPoint = defaultHeldVisual.transform.parent;
                CacheEquippedHandPose(defaultHeldVisual.transform);
                return cachedHandAttachPoint;
            }

            Transform playerRoot = ResolvePlayerRoot();
            if (playerRoot == null)
            {
                return null;
            }

            cachedHandAttachPoint = FindChildRecursive(playerRoot, handAttachBoneName);
            return cachedHandAttachPoint;
        }

        private GameObject ResolveDefaultHeldVisual()
        {
            if (cachedDefaultHeldVisual != null)
            {
                return cachedDefaultHeldVisual;
            }

            Transform playerRoot = ResolvePlayerRoot();
            if (playerRoot == null)
            {
                return null;
            }

            Transform defaultHeldTransform = FindChildRecursive(playerRoot, defaultHeldVisualName);
            if (defaultHeldTransform == null)
            {
                return null;
            }

            cachedDefaultHeldVisual = defaultHeldTransform.gameObject;
            CacheEquippedHandPose(defaultHeldTransform);
            if (defaultHeldTransform.parent != null)
            {
                cachedHandAttachPoint = defaultHeldTransform.parent;
            }

            return cachedDefaultHeldVisual;
        }

        private void CacheEquippedHandPose(Transform sourceTransform)
        {
            if (sourceTransform == null || hasEquippedHandPose)
            {
                return;
            }

            equippedHandLocalPosition = sourceTransform.localPosition;
            equippedHandLocalRotation = sourceTransform.localRotation;
            equippedHandLocalScale = sourceTransform.localScale;
            hasEquippedHandPose = true;
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private Vector3 GetDropSpawnPosition()
        {
            Transform playerRoot = ResolvePlayerRoot();
            Vector3 origin = playerRoot != null ? playerRoot.position : transform.position;
            return origin + GetDropForward() * dropForwardDistance + Vector3.up * dropHeightOffset;
        }

        private Vector3 GetDropForward()
        {
            Vector3 forward = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }
    }
}
