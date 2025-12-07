using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossTank : MonoBehaviour
{
    public enum BossState { Patrol, Repositioning, SpecialAttack }
    
    [Header("Configurações Gerais")]
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
    
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _currentLife = maxLife;
        _currentDestination = pointA.position;
        currentState = BossState.Patrol;
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
        transform.position = Vector2.MoveTowards(transform.position, _currentDestination, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, _currentDestination) < 0.2f)
        {
            if (_currentDestination == pointA.position) _currentDestination = pointB.position;
            else _currentDestination = pointA.position;
            
            FlipSprite();
        }

        _nextFire += Time.deltaTime;
        if (_nextFire >= fireRate)
        {
            Shoot();
            _nextFire = 0;
        }
    }

    void RepositionBehavior()
    {
        float destA = Vector2.Distance(transform.position, pointA.position);
        float destB = Vector2.Distance(transform.position, pointB.position);
        Vector3 rageAim = (destA < destB) ? pointA.position : pointB.position;

        transform.position = Vector2.MoveTowards(transform.position, rageAim, fastSpeed * Time.deltaTime);
        
        if(rageAim.x > transform.position.x && transform.localScale.x > 0) FlipSprite();
        else if(rageAim.x < transform.position.x && transform.localScale.x < 0) FlipSprite();

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
        currentState = BossState.Patrol;
        
        _currentDestination = (Vector2.Distance(transform.position, pointA.position) < 1f) ? pointB.position : pointA.position;

        if (
            (_currentDestination.x > transform.position.x && transform.localScale.x > 0) ||
            (_currentDestination.x < transform.position.x && transform.localScale.x < 0)
            ) {
            FlipSprite();  
        }
    }
    
    void Shoot()
    {
        Instantiate(heavyShellPrefab, firePoint.position, firePoint.rotation);
    }

    void ShootPlayer ()
    {
        GameObject heavyShell = Instantiate(heavyShellPrefab, firePoint.position, Quaternion.identity);
        Vector2 direction = (player.position - firePoint.position).normalized;
        // Se a bala tiver Rigidbody2D, aplique força aqui, ou passe a direção para o script da bala
        
        heavyShell.GetComponent<Rigidbody2D>().velocity = direction * 10f; // Exemplo simples
        
        // Rotaciona a bala para olhar pro player (opcional)
        float angulo = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        heavyShell.transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    public void TakeDamage(int damage)
    {
        if (_rageMode) return; // Opcional: Invencível enquanto faz o especial?

        _currentLife -= damage;
        
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
            Destroy(gameObject);
        }
    }

    void EnableSpecialPhase()
    {
        _rageMode = true;
        currentState = BossState.Repositioning;
    }
    
    void FlipSprite()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
