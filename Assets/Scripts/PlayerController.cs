using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;

    [Header("Crouch")]
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchCenter = 0.5f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float standCenter = 1f;

    [Header("Stumble / Fall")]
    [SerializeField] private float stumbleThreshold = 3f;
    [SerializeField] private float autoRecoveryTime = 2.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float footstepInterval = 0.4f;
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip[] hurtSounds;

    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider col;

    private Vector3 moveDir;
    private bool isGrounded;
    private bool isCrouching;
    private bool isRecovering;
    private float recoveryTimer;
    private float footstepTimer;

    private float speedMultiplier = 1f;
    private float modifierTimer;

    // Hashes su efikasniji od string lookupa u Animatoru
    private static readonly int HashSpeed    = Animator.StringToHash("Speed");
    private static readonly int HashGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashCrouch   = Animator.StringToHash("IsCrouching");
    private static readonly int HashJump     = Animator.StringToHash("Jump");
    private static readonly int HashStumble  = Animator.StringToHash("Stumble");
    private static readonly int HashFallDown = Animator.StringToHash("FallDown");
    private static readonly int HashGetUp    = Animator.StringToHash("GetUp");

    public float CurrentSpeed    { get; private set; }
    public float SpeedMultiplier => speedMultiplier;
    public float ModifierTimeLeft => modifierTimer;
    public bool  HasModifier     => modifierTimer > 0f;
    public bool  IsBoost         => speedMultiplier > 1f;

    private void Awake()
    {
        rb       = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        col      = GetComponent<CapsuleCollider>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        CheckGround();

        if (isRecovering)
        {
            recoveryTimer -= Time.deltaTime;
            // Automatski oporavak ili na pritisak Space
            if (recoveryTimer <= 0f || Input.GetKeyDown(KeyCode.Space))
                Recover();
            return;
        }

        GatherInput();
        HandleCrouch();
        HandleJump();
        TickModifier();
        HandleFootstepAudio();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isRecovering) return;
        ApplyMovement();
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void GatherInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Kretanje relativno prema kameri - forward kamere bez Y komponente
        Transform cam = Camera.main.transform;
        Vector3 fwd   = cam.forward; fwd.y   = 0f; fwd.Normalize();
        Vector3 right = cam.right;   right.y = 0f; right.Normalize();

        moveDir = (fwd * v + right * h).normalized;
    }

    private void ApplyMovement()
    {
        bool sprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && moveDir.magnitude > 0.1f;
        float baseSpeed  = isCrouching ? crouchSpeed : (sprinting ? runSpeed : walkSpeed);
        float finalSpeed = baseSpeed * speedMultiplier;

        // Čuvamo Y brzinu da fizika skoka i gravitacija rade normalno
        Vector3 velocity = moveDir * finalSpeed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;

        if (moveDir != Vector3.zero)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime);
        }

        CurrentSpeed = moveDir.magnitude > 0.1f ? finalSpeed : 0f;
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            // Resetujemo Y brzinu pre impulsa da skok uvek ima konzistentnu visinu
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetTrigger(HashJump);

            if (jumpSounds != null && jumpSounds.Length > 0 && SoundManager.instance != null)
                SoundManager.instance.PlaySoundFX(jumpSounds, 1f);
        }
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded)
        {
            isCrouching = true;
            col.height = crouchHeight;
            col.center = new Vector3(0f, crouchCenter, 0f);
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            col.height = standHeight;
            col.center = new Vector3(0f, standCenter, 0f);
        }
    }

    private void UpdateAnimator()
    {
        // Normalizujemo po runSpeed (bez multiplikatora) da blend tree uvek radi 0-1
        float normSpeed = runSpeed > 0f ? CurrentSpeed / (runSpeed * speedMultiplier) : 0f;
        animator.SetFloat(HashSpeed, normSpeed, 0.1f, Time.deltaTime);
        animator.SetBool(HashGrounded, isGrounded);
        animator.SetBool(HashCrouch, isCrouching);

        // Brzina reprodukcije animacija prati brzinu kretanja - sinhronizacija koraka
        animator.speed = (CurrentSpeed > 0.1f && !isRecovering) ? speedMultiplier : 1f;
    }

    private void HandleFootstepAudio()
    {
        if (CurrentSpeed > 0.1f && isGrounded)
        {
            footstepTimer += Time.deltaTime;
            // Korak zvuci su češći na većoj brzini
            float interval = footstepInterval / Mathf.Max(speedMultiplier, 0.1f);
            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                if (footstepSounds != null && footstepSounds.Length > 0 && SoundManager.instance != null)
                    SoundManager.instance.PlaySoundFX(footstepSounds, 0.7f);
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    public void TriggerStumble(float impactSpeed)
    {
        if (isRecovering) return;

        isRecovering   = true;
        rb.velocity    = Vector3.zero;
        recoveryTimer  = autoRecoveryTime;
        animator.speed = 1f;

        // Teži pad ako je brzina udara bila veća od praga, lako zapinjanje inače
        if (impactSpeed >= stumbleThreshold)
            animator.SetTrigger(HashFallDown);
        else
            animator.SetTrigger(HashStumble);

        if (hurtSounds != null && hurtSounds.Length > 0 && SoundManager.instance != null)
            SoundManager.instance.PlaySoundFX(hurtSounds, 1f);
    }

    private void Recover()
    {
        isRecovering = false;
        animator.SetTrigger(HashGetUp);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isRecovering) return;
        if (!collision.gameObject.CompareTag("Obstacle")) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact > 1.5f)
            TriggerStumble(impact);
    }

    public void ApplyModifier(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        modifierTimer   = duration;
    }

    private void TickModifier()
    {
        if (modifierTimer <= 0f) return;

        modifierTimer -= Time.deltaTime;
        if (modifierTimer <= 0f)
        {
            modifierTimer   = 0f;
            speedMultiplier = 1f;
        }
    }
}
