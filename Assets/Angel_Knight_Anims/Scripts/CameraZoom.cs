using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LowPolyAngelKnight
{
    public class CameraZoom : MonoBehaviour
    {
        public float minFov = 10f;
        public float maxFov = 90f;
        public float sensitivity = 10f;

        void Update()
        {
            float fov = Camera.main.fieldOfView;
            fov += Input.GetAxis("Mouse ScrollWheel") * sensitivity;
            fov = Mathf.Clamp(fov, minFov, maxFov);
            Camera.main.fieldOfView = fov;
        }
    }
}
