using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class BossTank : EnemyBase
{
    public enum BossState { Patrol, Repositioning, SpecialAttack }
    
    [Header("General Settings")]
    public BossState currentState;
    public float speed = 5f;
    public float fastSpeed = 8f;
    public int maxLife = 100;
    private int _currentLife;
    
    [Header("Patrol")]
    public Transform pointA;
    public Transform pointB;
    private Vector3 _currentDestination;
    
    [Header("Combat")]
    public GameObject heavyShellPrefab;
    public Transform firePoint;
    public float fireRate = 1.5f;
    private float _nextFire;
    
    [Header("Special Attack")]
    public Transform player;
    private List<int> _lifeTriggers = new List<int> { 80, 60, 40, 20 };
    private bool _rageMode = false;
    
    [Header("Visual Feedback")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;
    private Color _originalColor;
    private Coroutine _flashRoutine;
    public Color intangibleColor = new Color(0.5f, 0.5f, 1f, 0.6f);
    public GameObject explosionPrefab;
    
    [Header("UI")]
    public Slider healthBar;
    
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Collider2D[] _colliders;
    
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _colliders = GetComponents<Collider2D>();
        _currentLife = maxLife;
        _originalColor = _spriteRenderer.color;
        _currentDestination = pointA.position;
        currentState = BossState.Patrol;

        if (healthBar != null)
        {
            healthBar.maxValue = maxLife;
            healthBar.value = _currentLife;
        }
    }
    
    void Update()
    {
        switch (currentState)
        {
            case BossState.Patrol:
                PatrolBehavior();
                break;
            case BossState.Repositioning:
                RepositionBehavior();
                break;
        }
    }

    void PatrolBehavior()
    {
        Vector3 aimLockedY = new Vector3(_currentDestination.x, transform.position.y, transform.position.z);
        transform.position = Vector2.MoveTowards(transform.position, aimLockedY, speed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x - _currentDestination.x) < 0.2f)
        {
            _currentDestination = (_currentDestination == pointA.position) ? pointB.position : pointA.position;
            
            FlipSprite();
        }

        _nextFire += Time.deltaTime;
        if (_nextFire >= fireRate)
        {
            ShootPlayer();
            _nextFire = 0;
        }
    }

    void RepositionBehavior()
    {
        float destA = Vector2.Distance(transform.position, pointA.position);
        float destB = Vector2.Distance(transform.position, pointB.position);
        float positionX = (destA < destB) ? pointA.position.x : pointB.position.x;
        
        Vector3 rageAim = new Vector3(positionX, transform.position.y, transform.position.z);

        transform.position = Vector2.MoveTowards(transform.position, rageAim, fastSpeed * Time.deltaTime);
        
        if (rageAim.x > transform.position.x && transform.localScale.x > 0) FlipSprite();
        else if (rageAim.x < transform.position.x && transform.localScale.x < 0) FlipSprite();

        if (Vector2.Distance(transform.position, rageAim) < 0.2f)
        {
            StartCoroutine(PerformTripleAttack());
        }
    }

    IEnumerator PerformTripleAttack()
    {
        currentState = BossState.SpecialAttack;
        
        if (player.position.x > transform.position.x && transform.localScale.x > 0) FlipSprite();
        if (player.position.x < transform.position.x && transform.localScale.x < 0) FlipSprite();

        for (int i = 0; i < 3; i++)
        {
            ShootPlayer();
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);
        
        _rageMode = false;
        DisableVisualIntangible();
        currentState = BossState.Patrol;
        
        _currentDestination = (Vector2.Distance(transform.position, pointA.position) < 1f) ? pointB.position : pointA.position;

        if (
            (_currentDestination.x > transform.position.x && transform.localScale.x > 0) ||
            (_currentDestination.x < transform.position.x && transform.localScale.x < 0)
            ) {
            FlipSprite();  
        }
    }
    
    IEnumerator RedFlashEffect()
    {
        _spriteRenderer.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        _spriteRenderer.color = _originalColor;
        
        _flashRoutine = null; 
    }
    
    void ShootPlayer ()
    {
        GameObject heavyShell = Instantiate(heavyShellPrefab, firePoint.position, Quaternion.identity);
        Vector2 direction = (transform.localScale.x > 0) ? Vector2.left : Vector2.right;
        
        Bullet bullet = heavyShell.GetComponent<Bullet>(); 

        if (bullet != null)
        {
            float bulletSpeed = 10f;
            bullet.Launch(direction, bulletSpeed);            
        }
    }
    
    void EnableVisualIntangible()
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        
        _spriteRenderer.color = intangibleColor;
    }
    
    void DisableVisualIntangible()
    {
        _spriteRenderer.color = _originalColor;
    }

    public override void TakeHit(int damage)
    {
        if (currentState == BossState.Repositioning || _rageMode) return;

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(RedFlashEffect());
        
        _currentLife -= damage;

        if (healthBar != null)
        {
            healthBar.value = _currentLife;
        }
        
        for (int i = _lifeTriggers.Count - 1; i >= 0; i--)
        {
            if (_currentLife <= _lifeTriggers[i])
            {
                EnableSpecialPhase();
                _lifeTriggers.RemoveAt(i);
                break;
            }
        }

        if (_currentLife <= 0)
        {
            StartCoroutine(EndGameSequence());
        }
    }

    IEnumerator EndGameSequence()
    {
        if (healthBar != null) healthBar.gameObject.SetActive(false);

        _spriteRenderer.enabled = false;
        _rb.sleepMode = RigidbodySleepMode2D.StartAwake;
        
        this.enabled = false;

        foreach (Collider2D collider in _colliders)
        {
            collider.enabled = false;
        }
        
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        
        yield return new WaitForSeconds(3f);
            
        SceneManager.LoadScene("EndGame");
        
        Destroy(gameObject);
    }

    void EnableSpecialPhase()
    {
        _rageMode = true;
        currentState = BossState.Repositioning;
        EnableVisualIntangible();
    }
    
    void FlipSprite()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
