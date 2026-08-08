using UnityEngine;

namespace EtherealTerror
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public float speed = 9.0f;
        public float sprintSpeed = 12.0f; // Sprinting speed
        public static bool sprinting;
        public float mouseSensitivity = 100.0f;

        private CharacterController controller;
        private float xRotation = 0f;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            // Mouse look
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            transform.Rotate(Vector3.up * mouseX);
            Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Movement
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 move = transform.right * moveX + transform.forward * moveZ;

            // Normalize the movement vector to prevent faster diagonal movement
            if (move.magnitude > 1f)
            {
                move.Normalize();
            }

            // Sprint mechanic
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : speed;

            if (Input.GetKey(KeyCode.LeftShift)) // Check if sprinting
            {
                sprinting = true;
            }
            else
            {
                sprinting = false;
            }

            controller.Move(move * currentSpeed * Time.deltaTime);
        }
    }
}
