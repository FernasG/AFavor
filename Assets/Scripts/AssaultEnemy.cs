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

    [Header("Sons")]
    [SerializeField] AudioClip shootSound;

    [Header("Debug / Test")]
    [SerializeField] private bool simulateDie = false;
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
            simulateDie = false; // reset
            Die();
        }

        float distance = player != null ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;

        // se player sumiu, retoma patrulha
        if (player == null)
        {
            currentState = EnemyState.Patrol;
            waitingPatrol = false;
        }
        else if (!isHitted)
        {
            if (currentState == EnemyState.Chase)
            {
                if (distance > activationDistance + activationHysteresis)
                {
                    currentState = EnemyState.Patrol;
                    waitingPatrol = false; // importante: libera a patrulha quando sair do chase
                }
            }
            else
            {
                if (distance < activationDistance - activationHysteresis)
                {
                    currentState = EnemyState.Chase;
                }
                else
                {
                    currentState = EnemyState.Patrol;
                    // se já estava em espera por algum motivo, libera pra retomar patrulha
                    // (mas não forçamos cancelar invocations)
                    waitingPatrol = false;
                }
            }
        }
        else
        {
            if (currentState != EnemyState.Hitted)
                currentState = EnemyState.Hitted;
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

        // Se estiver esperando, só segura
        if (waitingPatrol)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
            return;
        }

        if (hasPatrolBounds)
        {
            float x = transform.position.x;
            float dir = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * moveSpeed, 0f);
            animator.SetBool("isWalking", true);

            // Corrige virada inicial baseada na posição relativa ao centro do patrol
            float centerX = (patrolMinX + patrolMaxX) / 2f;
            if (!waitingPatrol && Mathf.Abs(x - centerX) < 0.01f)
            {
                facingRight = player != null ? (player.position.x > transform.position.x) : true;
            }

            // Checagem de limites
            if (facingRight && x >= patrolMaxX - patrolArriveThreshold)
            {
                StopAndFlip();
            }
            else if (!facingRight && x <= patrolMinX + patrolArriveThreshold)
            {
                StopAndFlip();
            }
        }
        else
        {
            // Waypoint patrol (mantido)
            if (patrolIndex < 0 || patrolIndex >= patrolPoints.Length) patrolIndex = 0;
            Transform target = patrolPoints[patrolIndex];

            Vector2 direction = (Vector2)(target.position - transform.position);
            HandleFlip(direction.x);

            rb.linearVelocity = direction.normalized * moveSpeed;
            animator.SetBool("isWalking", true);

            if (Vector2.Distance(transform.position, target.position) < patrolArriveThreshold)
            {
                waitingPatrol = true;
                rb.linearVelocity = Vector2.zero;
                animator.SetBool("isWalking", false);
                Invoke(nameof(GoToNextPatrolPoint), patrolWaitTime);
            }
        }
    }


    void StopAndFlip()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isWalking", false);
        waitingPatrol = true;
        Invoke(nameof(FlipAndResume), patrolWaitTime);
    }

    void FlipAndResume()
    {
        Flip();
        waitingPatrol = false;
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        waitingPatrol = false;
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
    public void TakeHit()
    {
        if (isDead) return;
        isHitted = true;
        currentState = EnemyState.Hitted;
        Invoke(nameof(ResetHit), 0.4f);
    }

    void ResetHit()
    {
        isHitted = false;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("isDie");
        Destroy(gameObject, 2f);
    }

    // Chamado na animação de shoot
    

}


