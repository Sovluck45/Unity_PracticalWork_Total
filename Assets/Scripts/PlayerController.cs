using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] float speed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] Animator animator;
    [SerializeField] DataPacketHandler dataHandler;
    [SerializeField] LayerMask groundMask = ~0;

    Rigidbody body;
    bool isGrounded;

    static readonly int SpeedParam = Animator.StringToHash("Speed");
    static readonly int IsJumpingParam = Animator.StringToHash("IsJumping");

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (dataHandler == null)
            dataHandler = GetComponent<DataPacketHandler>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 velocity = body.linearVelocity;
        velocity.x = input.x * speed;
        velocity.z = input.z * speed;
        body.linearVelocity = velocity;

        CheckGrounded();

        if (animator != null)
        {
            animator.SetFloat(SpeedParam, input.magnitude);
            if (isGrounded)
                animator.SetBool(IsJumpingParam, false);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            body.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if (animator != null)
                animator.SetBool(IsJumpingParam, true);
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySoundEffect(SoundManager.Instance.JumpClip);
        }

        if (dataHandler != null)
            dataHandler.SendPosition(transform.position);
    }

    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, 1.25f, groundMask);
    }
}
