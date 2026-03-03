using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    Rigidbody rb;

    [Header("Move settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float linearDrag = 3f;

    [Header("Aim Settings")]
    [SerializeField] float rotationSpeed = 3f;
    private Vector2 aimValue;

    Transform lookAtTarget;

    [SerializeField] CinemachineCamera camera;
    [SerializeField] Camera cam;


    Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = linearDrag;

        if (lookAtTarget == null)
        {
            GameObject p2 = GameObject.FindGameObjectWithTag("P2");
            if (p2 != null)
                lookAtTarget = p2.transform;
        }
    }

    private void Update()
    {
        HandleRotation();
    }

    private void FixedUpdate()
    {
        HandleMovement(moveInput);
        
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void HandleMovement(Vector2 inputVector)
    {
        if (camera == null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (inputVector.sqrMagnitude > 0.001f)
        {
            Vector3 camForward = camera.transform.forward;
            Vector3 camRight = camera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = (camForward * inputVector.y + camRight * inputVector.x).normalized;
            Vector3 moveVelocity = moveDirection * moveSpeed;
            rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
        }
    }

    private void HandleRotation()
    {
        if (camera == null) return;

        Vector3 screenPosition = new Vector3(aimValue.x * Screen.width, aimValue.y * Screen.height, 0f);

        Ray raycast = cam.ScreenPointToRay(screenPosition);
        float playerHeight = transform.position.y;
        Plane plane = new Plane(Vector3.up, new Vector3(0f, playerHeight, 0f));

        if (!plane.Raycast(raycast, out float distance)) return;

        Vector3 hitPoint = raycast.GetPoint(distance);
        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;
        direction.Normalize();
        transform.rotation = Quaternion.LookRotation(direction);
    }

    public void OnAim(InputAction.CallbackContext context)
    {

        aimValue = context.ReadValue<Vector2>();

    }
}
