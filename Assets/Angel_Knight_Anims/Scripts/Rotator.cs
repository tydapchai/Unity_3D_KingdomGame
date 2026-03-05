using UnityEngine;

namespace LowPolyAngelKnight
{

    public class Rotator : MonoBehaviour
    {
        [SerializeField] float rotSpeed = 100f;
        bool dragging = false;
        Rigidbody rb;

        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void OnMouseDrag()
        {
            dragging = true;
        }

        void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }

            Vector3 eulerRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(eulerRotation.x, eulerRotation.y, 0);

            transform.position = new Vector3(.18f, .5f, 0);

            if (Input.GetMouseButtonUp(1))
            {
                transform.rotation = Quaternion.Euler(0, 180f, 0);
            }
        }

        private void FixedUpdate()
        {
            if (dragging)
            {
                float x = Input.GetAxis("Mouse X") * rotSpeed * Time.fixedDeltaTime;
                float y = Input.GetAxis("Mouse Y") * rotSpeed * Time.fixedDeltaTime;

                rb.AddTorque(Vector3.down * x);
                rb.AddTorque(Vector3.right * 2 * y);
            }
        }
    }
}
