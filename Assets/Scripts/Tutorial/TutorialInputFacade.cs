using StarterAssets;
using UnityEngine;

namespace Unity.FantasyKingdom
{
    public class TutorialInputFacade : MonoBehaviour
    {
        [SerializeField] private StarterAssetsInputs starterAssetsInputs;
        [SerializeField] private StarterAssets.ThirdPersonController thirdPersonController;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        public StarterAssetsInputs StarterAssetsInputs => starterAssetsInputs;
        public StarterAssets.ThirdPersonController ThirdPersonController => thirdPersonController;
        public Transform PlayerTransform => playerTransform;
        public Vector2 Move => starterAssetsInputs != null ? starterAssetsInputs.move : Vector2.zero;
        public Vector2 Look => starterAssetsInputs != null ? starterAssetsInputs.look : Vector2.zero;
        public bool JumpPressed => starterAssetsInputs != null && starterAssetsInputs.jump;
        public bool SprintPressed => starterAssetsInputs != null && starterAssetsInputs.sprint;
        public bool InteractPressed => Input.GetKeyDown(interactKey);
        public bool IsGrounded => thirdPersonController == null || thirdPersonController.Grounded;

        private void Awake()
        {
            AutoAssignReferences();
        }

        public void AutoAssignReferences()
        {
            if (starterAssetsInputs == null)
            {
                starterAssetsInputs = FindFirstObjectByType<StarterAssetsInputs>();
            }

            if (thirdPersonController == null)
            {
                thirdPersonController = FindFirstObjectByType<StarterAssets.ThirdPersonController>();
            }

            if (playerTransform == null)
            {
                if (thirdPersonController != null)
                {
                    playerTransform = thirdPersonController.transform;
                }
                else
                {
                    PlayerControl player = FindFirstObjectByType<PlayerControl>();
                    if (player != null)
                    {
                        playerTransform = player.transform;
                    }
                }
            }
        }

        public Vector3 GetPlayerPosition()
        {
            return playerTransform != null ? playerTransform.position : Vector3.zero;
        }

        public float GetMoveMagnitude()
        {
            return Move.magnitude;
        }

        public float GetLookMagnitude()
        {
            return Mathf.Abs(Look.x) + Mathf.Abs(Look.y);
        }

        public bool IsNearTarget(Transform target, float radius)
        {
            if (playerTransform == null || target == null)
            {
                return false;
            }

            Vector3 from = playerTransform.position;
            Vector3 to = target.position;
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to) <= radius;
        }
    }
}
