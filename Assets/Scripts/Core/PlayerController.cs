using UnityEngine;
using UnityEngine.InputSystem;

namespace VoidBound.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private InputActionReference moveAction;

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private Transform cameraTransform;
        private VoidBound.Combat.CharacterAnimation anim;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            anim = GetComponent<VoidBound.Combat.CharacterAnimation>();
        }

        private void Start()
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (moveAction != null && moveAction.action != null)
                moveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null && moveAction.action != null)
                moveAction.action.Disable();
        }

        private void Update()
        {
            Vector2 input = Vector2.zero;
            if (moveAction != null && moveAction.action != null)
                input = moveAction.action.ReadValue<Vector2>();

            Vector3 moveDirection = GetIsometricDirection(input);

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (controller.isGrounded)
            {
                if (verticalVelocity.y < 0f)
                    verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
                verticalVelocity.y = Mathf.Max(verticalVelocity.y, -20f);
            }

            Vector3 finalMove = moveDirection * moveSpeed + verticalVelocity;
            controller.Move(finalMove * Time.deltaTime);

            anim?.SetSpeed(moveDirection.magnitude);
        }

        private Vector3 GetIsometricDirection(Vector2 input)
        {
            if (cameraTransform == null)
                return new Vector3(input.x, 0f, input.y);

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            return (camRight * input.x + camForward * input.y).normalized;
        }
    }
}
