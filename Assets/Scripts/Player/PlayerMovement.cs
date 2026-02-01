using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    Rigidbody rb;

    [Header("Move settings")]
    [SerializeField] float moveSpeed = 5f;

    [Header("Look at target (e.g. AI for group framing)")]
    [SerializeField] Transform lookAtTarget;

    [SerializeField] CinemachineCamera camera;

    Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (lookAtTarget == null)
        {
            GameObject p2 = GameObject.FindGameObjectWithTag("P2");
            if (p2 != null)
                lookAtTarget = p2.transform;
        }
    }

    private void FixedUpdate()
    {
        HandleMovement(moveInput);
        HandleRotation();
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

    private void HandleRotation()
    {
        if (lookAtTarget == null) return;

        Vector3 toTarget = lookAtTarget.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f) return; 

        transform.rotation = Quaternion.LookRotation(toTarget.normalized);
    }
}
