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

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 6f;
    [SerializeField] private float rollDuration = 0.6f;
    [SerializeField] private float rollCooldown = 1.5f;

    [Header("Stumble / Fall")]
    [SerializeField] private float stumbleThreshold = 3f;
    [SerializeField] private float autoRecoveryTime = 2.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float footstepInterval = 0.4f;
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip[] landSounds;
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioSource footstepSource;

    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider col;

    private Vector3 moveDir;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isCrouching;
    private bool isRecovering;
    private float recoveryTimer;
    private float footstepTimer;

    private float speedMultiplier = 1f;
    private float modifierTimer;

    private bool isRolling;
    private float rollTimer;
    private float rollCooldownTimer;
    private Vector3 rollDirection;
    private Quaternion rollRotation;

    private float jumpCooldownTimer;

    // Hashes su efikasniji od string lookupa u Animatoru
    private static readonly int HashSpeed    = Animator.StringToHash("Speed");
    private static readonly int HashGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashCrouch   = Animator.StringToHash("IsCrouching");
    private static readonly int HashJump     = Animator.StringToHash("Jump");
    private static readonly int HashStumble  = Animator.StringToHash("Stumble");
    private static readonly int HashFallDown = Animator.StringToHash("FallDown");
    private static readonly int HashGetUp    = Animator.StringToHash("GetUp");
    private static readonly int HashRoll     = Animator.StringToHash("Roll");

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

        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
            {
                isRolling = false;
                // Vraćamo collider na normalnu visinu kada se kolut završi
                col.height = standHeight;
                col.center = new Vector3(0f, standCenter, 0f);
            }
            return;
        }

        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;

        GatherInput();
        HandleCrouch();
        HandleJump();
        HandleRoll();
        TickModifier();
        HandleFootstepAudio();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isRecovering) return;

        if (isRolling)
        {
            rb.velocity = new Vector3(rollDirection.x * rollSpeed, rb.velocity.y, rollDirection.z * rollSpeed);
            // Zaključavamo rotaciju na vrednost kada je kolut počeo - sprečava kružno kretanje
            rb.rotation = rollRotation;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        ApplyMovement();
    }

    private void CheckGround()
    {
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
            isGrounded = false;
            wasGrounded = false;
            return;
        }

        wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.25f, groundLayer);

        // Detektujemo trenutak dočekivanja - kada smo bili u vazduhu a sada smo na tlu
        if (isGrounded && !wasGrounded)
        {
            if (landSounds != null && landSounds.Length > 0 && SoundManager.instance != null)
                SoundManager.instance.PlaySoundFX(landSounds, 1f);
        }
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
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching && rb.velocity.y <= 0.05f)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            jumpCooldownTimer = 0.4f;
            animator.SetTrigger(HashJump);

            if (jumpSounds != null && jumpSounds.Length > 0 && SoundManager.instance != null)
                SoundManager.instance.PlaySoundFX(jumpSounds, 1f);
        }
    }

    private void HandleRoll()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;
        if (!isGrounded || isCrouching) return;
        if (rollCooldownTimer > 0f) return;

        // Hvata pravac i rotaciju u trenutku pritiska - ostaju fiksirani tokom celog koluta
        rollDirection = transform.forward;
        rollRotation  = rb.rotation;

        isRolling         = true;
        rollTimer         = rollDuration;
        rollCooldownTimer = rollCooldown;

        // Smanjujemo collider tokom koluta da karakter može proći ispod niske prepreke
        col.height = crouchHeight;
        col.center = new Vector3(0f, crouchCenter, 0f);

        animator.SetTrigger(HashRoll);
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
        if (footstepSource == null || footstepSounds == null || footstepSounds.Length == 0) return;

        if (CurrentSpeed > 0.1f && isGrounded)
        {
            footstepTimer += Time.deltaTime;
            float interval = footstepInterval / Mathf.Max(speedMultiplier, 0.1f);
            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                int index = Random.Range(0, footstepSounds.Length);
                footstepSource.clip = footstepSounds[index];
                footstepSource.Play();
            }
        }
        else
        {
            footstepTimer = 0f;
            // Zaustavljamo korak zvuk čim karakter stane
            if (footstepSource.isPlaying)
                footstepSource.Stop();
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
