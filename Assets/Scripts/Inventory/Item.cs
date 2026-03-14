using UnityEngine;

namespace Unity.FantasyKingdom
{
    [DisallowMultipleComponent]
    public class Item : MonoBehaviour
    {
        public ItemSO item;
        public int amount = 1;

        private const float MinColliderSize = 0.05f;

        private void Reset()
        {
            EnsurePickupCollider();
        }

        private void Awake()
        {
            EnsurePickupCollider();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsurePickupCollider();
        }
#endif

        private void EnsurePickupCollider()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            BoxCollider rootBoxCollider = GetComponent<BoxCollider>();
            bool hasOnlyRootBoxCollider = colliders.Length == 1 && colliders[0] == rootBoxCollider;
            if (colliders.Length > 0 && !hasOnlyRootBoxCollider)
            {
                return;
            }

            if (!TryGetLocalRendererBounds(out Bounds localBounds))
            {
                return;
            }

            if (rootBoxCollider == null)
            {
                rootBoxCollider = gameObject.AddComponent<BoxCollider>();
            }

            rootBoxCollider.center = localBounds.center;
            rootBoxCollider.size = new Vector3(
                Mathf.Max(localBounds.size.x, MinColliderSize),
                Mathf.Max(localBounds.size.y, MinColliderSize),
                Mathf.Max(localBounds.size.z, MinColliderSize));
        }

        private bool TryGetLocalRendererBounds(out Bounds localBounds)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
            localBounds = default;
            bool foundBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Bounds rendererBounds = renderer.bounds;
                Vector3 extents = rendererBounds.extents;
                Vector3 center = rendererBounds.center;
                Vector3[] corners =
                {
                    center + new Vector3(extents.x, extents.y, extents.z),
                    center + new Vector3(extents.x, extents.y, -extents.z),
                    center + new Vector3(extents.x, -extents.y, extents.z),
                    center + new Vector3(extents.x, -extents.y, -extents.z),
                    center + new Vector3(-extents.x, extents.y, extents.z),
                    center + new Vector3(-extents.x, extents.y, -extents.z),
                    center + new Vector3(-extents.x, -extents.y, extents.z),
                    center + new Vector3(-extents.x, -extents.y, -extents.z)
                };

                for (int j = 0; j < corners.Length; j++)
                {
                    Vector3 localCorner = transform.InverseTransformPoint(corners[j]);
                    if (!foundBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        foundBounds = true;
                        continue;
                    }

                    localBounds.Encapsulate(localCorner);
                }
            }

            return foundBounds;
        }
    }
}
