namespace Portals
{
    using UnityEngine;

    public class Move : MonoBehaviour
    {
        public float speed = 7f;
        public float jumpHeight = 2f;
        public float gravity = 5f;
        public float sensitivity = 10f;
        public float clampAngle = 90f;
        public float smoothFactor = 25f;

        private Vector2 targetRotation;
        private Vector3 targetMovement;
        private Vector2 currentRotation;
        private Vector3 currentForce;

        private Camera Cam;

        private CharacterController Player;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Start()
        {
            Cam = Camera.main;

            Player = GetComponent<CharacterController>();
        }

        void FixedUpdate()
        {
            if (!Player.isGrounded)
            {
                currentForce.y -= gravity * Time.deltaTime;
            }
        }

        public void PlayerInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
            if (Input.GetKeyDown(KeyCode.Space) && Player.isGrounded)
            {
                currentForce.y = jumpHeight;
            }

            float mousex = Input.GetAxisRaw("Mouse X");
            float mousey = Input.GetAxisRaw("Mouse Y");

            targetRotation.x -= mousey * sensitivity;
            targetRotation.y += mousex * sensitivity;

            targetRotation.x = Mathf.Clamp(targetRotation.x, -clampAngle, clampAngle);

            currentRotation = Vector2.Lerp(currentRotation, targetRotation, smoothFactor * Time.deltaTime);

            Cam.transform.localRotation = Quaternion.Euler(currentRotation.x, 0f, 0f);
            Player.transform.rotation = Quaternion.Euler(0f, currentRotation.y, 0f);

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            targetMovement = (Player.transform.right * horizontal + Player.transform.forward * vertical).normalized;

            Player.Move((targetMovement + currentForce) * speed * Time.deltaTime);
        }
    }
}
