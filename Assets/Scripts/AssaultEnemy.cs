using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class AssaultEnemy : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Idle,
        Dead,
        Hitted
    }

    private bool isInvoking = false;

    [Header("Estados")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Movimentação")]
    public float moveSpeed = 2f;
    public float stopDistance = 1f;
    public float stopBuffer = 0.2f;
    private Vector3 baseScale;

    [Header("Patrulha")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 1f;
    public float patrolArriveThreshold = 0.3f;
    private int patrolIndex = 0;
    private bool waitingPatrol = false;
    private bool hasPatrolBounds = false;
    private float patrolMinX = 0f;
    private float patrolMaxX = 0f;

    [Header("Ativação do Chase")]
    public float activationDistance = 10f;
    public float activationHysteresis = 1.5f;

    [Header("Ataque")]
    public float attackRange = 20f;
    public float shootCooldown = 1.8f;
    public bool useAnimationEvent = true;
    public float shootFireDelay = 0.05f;

    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Sons")]
    [SerializeField] AudioClip shootSound;

    [Header("Debug / Test")]
    [SerializeField] private bool simulateDie = false;
    [SerializeField] private bool simulateHit = false;

    protected Rigidbody2D rb;
    protected Animator animator;
    protected Transform player;

    protected bool facingRight = true;
    protected bool isDead = false;
    protected bool isHitted = false;
    protected bool isAttacking = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        rb.gravityScale = 0;
        rb.freezeRotation = true;

        baseScale = transform.localScale;

        ComputePatrolBounds();
        currentHealth = maxHealth;


    }

    void ComputePatrolBounds()
    {
        hasPatrolBounds = false;

        if (patrolPoints == null || patrolPoints.Length < 2)
            return;

        patrolMinX = float.PositiveInfinity;
        patrolMaxX = float.NegativeInfinity;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            float x = patrolPoints[i].position.x;
            if (x < patrolMinX) patrolMinX = x;
            if (x > patrolMaxX) patrolMaxX = x;
        }

        if (patrolMaxX > patrolMinX)
            hasPatrolBounds = true;
    }

    protected virtual void FixedUpdate()
    {
        if (isDead) return;

        if (simulateDie)
        {
            simulateDie = false;
            Die();
        }

        if (simulateHit)
        {
            simulateHit = false;
            TakeHit(20);
        }

        float distance = player != null ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;

        // Lógica de estado
        if (player == null)
        {
            currentState = EnemyState.Patrol;
            waitingPatrol = false;
        }
        else if (!isHitted)
        {
            // Se já estiver em Chase, volta pra Patrol apenas se estiver longe
            if (currentState == EnemyState.Chase && distance > activationDistance + activationHysteresis)
            {
                currentState = EnemyState.Patrol;
                waitingPatrol = false;
            }
            // Se estiver patrulhando e o player estiver perto, entra em Chase
            else if (currentState == EnemyState.Patrol && distance < activationDistance - activationHysteresis)
            {
                currentState = EnemyState.Chase;
            }
            // NÃO forçar Patrol se estiver fora do range
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                moveSpeed = 2;
                Patrol();
                break;

            case EnemyState.Chase:
                moveSpeed = 4;
                Chase();
                break;

            case EnemyState.Hitted:
                rb.linearVelocity = Vector2.zero;
                animator.SetBool("isWalking", false);
                break;

            case EnemyState.Dead:
                rb.linearVelocity = Vector2.zero;
                animator.SetBool("isWalking", false);
                break;
        }
    }

    // -------------------------------------------
    // PATRULHA
    // -------------------------------------------
    protected void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
            return;
        }

        if (waitingPatrol)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
            return;
        }

        // Patrulha por limites
        if (hasPatrolBounds)
        {
            float x = transform.position.x;
            float dir = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * moveSpeed, 0f);
            animator.SetBool("isWalking", true);

            if ((facingRight && x >= patrolMaxX - patrolArriveThreshold) ||
                (!facingRight && x <= patrolMinX + patrolArriveThreshold))
            {
                if (!isInvoking)
                {
                    waitingPatrol = true;
                    rb.linearVelocity = Vector2.zero;
                    animator.SetBool("isWalking", false);
                    isInvoking = true;
                    Invoke(nameof(FlipAndResume), patrolWaitTime);
                }
            }
            return;
        }

        // Patrulha por waypoints
        Transform target = patrolPoints[patrolIndex];
        Vector2 dirToTarget = (target.position - transform.position);
        float dist = dirToTarget.magnitude;

        if (Mathf.Abs(dirToTarget.x) > 0.01f)
            HandleFlip(dirToTarget.x);

        rb.linearVelocity = dirToTarget.normalized * moveSpeed;
        animator.SetBool("isWalking", true);

        if (dist < patrolArriveThreshold)
        {
            if (!isInvoking)
            {
                waitingPatrol = true;
                rb.linearVelocity = Vector2.zero;
                animator.SetBool("isWalking", false);
                isInvoking = true;
                Invoke(nameof(GoToNextPatrolPoint), patrolWaitTime);
            }
        }
    }

    // Modifique FlipAndResume e GoToNextPatrolPoint
    void FlipAndResume()
    {
        Flip();
        waitingPatrol = false;
        isInvoking = false;
    }

    void GoToNextPatrolPoint()
    {
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        waitingPatrol = false;
        isInvoking = false;
    }

    // -------------------------------------------
    // CHASE
    // -------------------------------------------
    protected void Chase()
    {
        if (player == null) return;

        float targetX = player.position.x;
        if (hasPatrolBounds)
            targetX = Mathf.Clamp(targetX, patrolMinX, patrolMaxX);

        float dx = targetX - transform.position.x;


        if (currentState == EnemyState.Chase)
            HandleFlip(dx);

        float absDx = Mathf.Abs(dx);

        // ----------------------------------------------
        // 1) LONGE → ANDA
        // ----------------------------------------------
        if (absDx > stopDistance + stopBuffer)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(dx) * moveSpeed, 0f);
            animator.SetBool("isWalking", true);
            return;
        }

        // ----------------------------------------------
        // 2) PERTO (PARA) → ATACA SE TIVER NO RANGE
        // ----------------------------------------------
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isWalking", false);

        // Agora qualquer distância onde ele para, ele pode atacar
        if (!isAttacking && PlayerInSight())
            OnReachStopDistance();
    }

    // -------------------------------------------
    // ATAQUE (mantive sua rotina antiga)
    // -------------------------------------------
    protected void OnReachStopDistance()
    {
        if (isDead || isAttacking) return;
        Attack();
    }

    protected void Attack()
    {
        if (isAttacking) return;
        if (AnimatorBusy()) return;
        StartCoroutine(AttackRoutine());
        Debug.Log("ATTACK ROUTINE STARTED");
    }

    protected IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        Vector2 dir = player ? (Vector2)(player.position - transform.position).normalized : Vector2.right;
        HandleFlip(dir.x);

        animator.ResetTrigger("isShoot");
        animator.SetBool("isWalking", false);

        animator.SetTrigger("isShoot");


        if (!useAnimationEvent)
        {
            float timeoutShoot = 1.5f;
            float ts = 0f;
            while (ts < timeoutShoot)
            {
                var sInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (sInfo.IsName("Shoot")) break;
                ts += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(shootFireDelay);
            OnShoot();
        }

        yield return new WaitForSeconds(shootCooldown);

        isAttacking = false;
    }

    public void OnShoot()
    {
        if (!projectilePrefab || !firePoint) return;
        float bulletDelay = 0.15f; // tempo de delay para coincidir com animação

        // Cria a bala depois do delay
        Invoke(nameof(SpawnBullet), bulletDelay);

    }

    private void SpawnBullet()
    {
        GameObject bulletObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            float bulletSpeed = 10f;
            Vector2 direction = facingRight ? Vector2.right : Vector2.left;
            bullet.Launch(direction, bulletSpeed);
        }
        AudioSource.PlayClipAtPoint(shootSound, transform.position);

    }
    protected bool AnimatorBusy()
    {
        if (!animator) return false;
        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName("PrepareShoot") || info.IsName("Shoot");
    }

    // -------------------------------------------
    // FLIP (mantém somente multiplicação do x — sem set absoluto)
    // -------------------------------------------
    protected void HandleFlip(float dx)
    {
        if (dx > 0 && !facingRight) Flip();
        else if (dx < 0 && facingRight) Flip();
    }

    protected void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = baseScale; // usa escala base
        s.x *= facingRight ? 1 : -1; // aplica inversão absoluta
        transform.localScale = s;
    }


    // -------------------------------------------
    // VISÃO DO PLAYER (corrigida)
    // -------------------------------------------
    protected bool PlayerInSight()
    {
        if (!player) return false;

        // Se estiver dentro do range e virar para o Player → pode atirar
        float dist = Vector2.Distance(transform.position, player.position);
        return dist <= attackRange;
    }


    // -------------------------------------------
    // HIT / DEATH
    // -------------------------------------------
    public void TakeHit(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return; // IMPORTANTÍSSIMO
        }

        isHitted = true;
        currentState = EnemyState.Hitted;

        animator.SetTrigger("isHitted");
        Invoke(nameof(ResetHit), 0.4f);

        // NOVO: se estava patrulhando, vai para chase
        if (currentState == EnemyState.Patrol && player != null)
        {
            currentState = EnemyState.Chase;
            waitingPatrol = false; // libera patrulha caso estivesse parada
        }


    }

    void ResetHit()
    {
        isHitted = false;

        // Ao levar hit, sempre começa a perseguir o player se ele existir
        if (player != null)
        {
            currentState = EnemyState.Chase;
            waitingPatrol = false; // garante que a patrulha não bloqueie o movimento
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("isDie");
        Destroy(gameObject, 1f);
    }

    // Chamado na animação de shoot


}


