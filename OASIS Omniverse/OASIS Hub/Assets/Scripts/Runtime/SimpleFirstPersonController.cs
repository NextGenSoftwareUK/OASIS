using UnityEngine;
using OASIS.Omniverse.UnityHost.Rendering;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFirstPersonController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private Transform cameraTransform;

        private CharacterController _controller;
        private float _pitch;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraTransform == null)
            {
                var cameraObject = new GameObject("PlayerCamera");
                cameraObject.transform.SetParent(transform);
                cameraObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                cameraTransform = cameraObject.transform;
                var camera = cameraObject.AddComponent<Camera>();
                camera.allowHDR = true;
                cameraObject.AddComponent<AudioListener>();
                cameraObject.AddComponent<CinematicPortalGlow>();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            var mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            var mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            transform.Rotate(Vector3.up * mouseX);
            _pitch = Mathf.Clamp(_pitch - mouseY, -80f, 80f);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            var direction = (transform.right * horizontal + transform.forward * vertical).normalized;
            _controller.Move(direction * moveSpeed * Time.deltaTime);
        }
    }
}

