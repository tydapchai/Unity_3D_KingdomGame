using UnityEngine;

namespace Unity.FantasyKingdom
{
    public class TutorialWorldMarker : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 worldOffset = new(0f, 2.2f, 0f);
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private bool hideWhenNoTarget = true;

        public Transform Target => target;

        private Camera cachedCamera;

        private void Awake()
        {
            cachedCamera = Camera.main;
            RefreshVisibility();
        }

        private void LateUpdate()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            if (target != null)
            {
                transform.position = target.position + worldOffset;

                if (faceCamera && cachedCamera != null)
                {
                    Vector3 direction = transform.position - cachedCamera.transform.position;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(direction.normalized);
                    }
                }
            }

            RefreshVisibility();
        }

        public void AttachTo(Transform newTarget)
        {
            target = newTarget;
            RefreshVisibility();
        }

        public void ClearTarget()
        {
            target = null;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (!hideWhenNoTarget)
            {
                return;
            }

            bool shouldShow = target != null;
            if (gameObject.activeSelf != shouldShow)
            {
                gameObject.SetActive(shouldShow);
            }
        }
    }
}
