using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D _rb;
    private Vector2 moveInput;
    private bool isFacingRight = true;
    
    [Header("Jumping")]
    public float jumpForce = 5f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool _isGrounded;
    
    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;
    private float nextFire = 0f;

    [Header("Sons")]
    [SerializeField] AudioClip shootSound;
    
    [Header("Animation")]
    [SerializeField]
    private Animator _animator;

    [Header("Health")]
    public PlayerHealth playerHealth; 
    
    private PlayerControls playerControls;
    
    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        
        if (playerControls.Player.Shoot.IsPressed() && Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;
            Shoot();
        }
        
        UpdateAnimation();
    }
    
    void FixedUpdate()
    {
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();

        _rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, _rb.linearVelocity.y);

        if (!isFacingRight && moveInput.x > 0)
        {
            Flip();
        }
        else if (isFacingRight && moveInput.x < 0)
        {
            Flip();
        }
    }
    
    private void UpdateAnimation()
    {
        float horizontalSpeed = Mathf.Abs(moveInput.x);
        bool isRunning = horizontalSpeed > 0f;
    
        _animator.SetBool("isRunning", isRunning);
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && _isGrounded)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void Shoot()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Shoot");
        }
        
        AudioSource.PlayClipAtPoint(shootSound, transform.position);
        GameObject playerShoot = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        PlayerShoot shootScript = playerShoot.GetComponent<PlayerShoot>();
    
        shootScript.Launch(isFacingRight);
    }
}
