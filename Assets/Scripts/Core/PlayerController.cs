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

        [Header("Dodge roll")]
        [SerializeField] private float dodgeSpeed = 15f;
        [SerializeField] private float dodgeDuration = 0.28f;
        [SerializeField] private float dodgeIFrames = 0.24f;   // invulnerable window
        [SerializeField] private float dodgeCooldown = 0.7f;

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private Transform cameraTransform;
        private VoidBound.Combat.CharacterAnimation anim;
        private VoidBound.Combat.Health health;

        private bool dodging;
        private float dodgeTimer;
        private float dodgeCdTimer;
        private Vector3 dodgeDir;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            anim = GetComponent<VoidBound.Combat.CharacterAnimation>();
            health = GetComponent<VoidBound.Combat.Health>();
        }

        // Start a dodge-roll if off cooldown and not already rolling. Public so a
        // keybind (now) or an on-screen button (mobile, later) can trigger it.
        public bool TryDodge(Vector3 desiredDir)
        {
            if (dodging || dodgeCdTimer > 0f) return false;
            dodgeDir = desiredDir.sqrMagnitude > 0.01f ? desiredDir.normalized : transform.forward;
            dodging = true;
            dodgeTimer = dodgeDuration;
            dodgeCdTimer = dodgeCooldown;
            transform.rotation = Quaternion.LookRotation(dodgeDir, Vector3.up);
            if (health != null) health.Invulnerable = true;
            return true;
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

            dodgeCdTimer -= Time.deltaTime;

            // Dodge input (Space now; a touch button can call TryDodge on mobile).
            if (!dodging && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                TryDodge(moveDirection);

            if (dodging)
            {
                dodgeTimer -= Time.deltaTime;
                if (health != null && dodgeTimer <= dodgeDuration - dodgeIFrames)
                    health.Invulnerable = false;

                if (controller.isGrounded && verticalVelocity.y < 0f) verticalVelocity.y = -2f;
                else verticalVelocity.y = Mathf.Max(verticalVelocity.y + gravity * Time.deltaTime, -20f);

                controller.Move((dodgeDir * dodgeSpeed + verticalVelocity) * Time.deltaTime);
                anim?.SetSpeed(1f);

                if (dodgeTimer <= 0f)
                {
                    dodging = false;
                    if (health != null) health.Invulnerable = false;
                }
                return;
            }

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
