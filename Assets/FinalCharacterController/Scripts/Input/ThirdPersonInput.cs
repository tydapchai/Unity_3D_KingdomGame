using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GinjaGaming.FinalCharacterController
{
    [DefaultExecutionOrder(-2)]
    public class ThirdPersonInput : MonoBehaviour, PlayerControls.IThirdPersonMapActions
    {
        #region Class Variables
        public Vector2 ScrollInput { get; private set; }

        [SerializeField] private CinemachineCamera _virtualCamera;
        [SerializeField] private float _cameraZoomSpeed = 0.1f;
        [SerializeField] private float _cameraMinZoom = 1f;
        [SerializeField] private float _cameraMaxZoom = 5f;

        // Đã sửa đổi: Sử dụng đúng class Cinemachine3rdPersonFollow của Cinemachine 3
        private Cinemachine3rdPersonFollow _thirdPersonFollow;
        #endregion

        #region Startup
        private void Awake()
        {
            // Đã sửa đổi: Lấy component Cinemachine3rdPersonFollow
            if (_virtualCamera != null)
            {
                _thirdPersonFollow = _virtualCamera.GetComponent<Cinemachine3rdPersonFollow>();
                
                if (_thirdPersonFollow == null)
                {
                    Debug.LogWarning("Không tìm thấy component Cinemachine3rdPersonFollow trên Virtual Camera!");
                }
            }
        }
        
        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot enable");
                return;
            }

            PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.Enable();
            PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot disable");
                return;
            }

            PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.Disable();
            PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.RemoveCallbacks(this);
        }
        #endregion

        #region Update
        private void Update()
        {
            // Đã sửa đổi: Thêm điều kiện check null để code chạy an toàn
            if (_thirdPersonFollow != null)
            {
                _thirdPersonFollow.CameraDistance = Mathf.Clamp(_thirdPersonFollow.CameraDistance + ScrollInput.y, _cameraMinZoom, _cameraMaxZoom);
            }
        }

        private void LateUpdate()
        {
            ScrollInput = Vector2.zero;
        }
        #endregion

        #region Input Callbacks
        public void OnScrollCamera(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            Vector2 scrollInput = context.ReadValue<Vector2>();
            ScrollInput = -1f * scrollInput.normalized * _cameraZoomSpeed;
        }
        #endregion
    }
}