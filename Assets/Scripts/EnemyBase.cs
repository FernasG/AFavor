using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Movimentação")]
    public float moveSpeed = 3f;
    public float stopDistance = 3f;
    
    [Header("Ativação/Desativação")]
    public float activationDistance = 20f;

    [Header("Configuração de Tiro Rápido")]
    public float timeBetweenShots = 3f;
    public float prepareDuration = 0.5f;
    
    public int burstCount = 5;
    public float fireRateInterval = 0.1f;
    public float shootAnimationDuration = 0.5f;

    protected Transform player;
    protected Rigidbody2D rb;
    protected Animator animator;

    protected bool facingRight = true;
    protected bool isShootingActive = false;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError($"{name}: Animator não encontrado!");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError($"{name}: Nenhum objeto com tag 'Player' encontrado! Verifique a tag 'Player'.");
    }

    protected virtual void FixedUpdate()
    {
        if (player == null || animator == null)
            return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > activationDistance)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
            animator.SetBool("isPreparingShoot", false);
            
            if (isShootingActive)
            {
                StopAllCoroutines();
                isShootingActive = false;
            }
            return;
        }

        float directionX = player.position.x - transform.position.x;

        if (Mathf.Abs(directionX) > stopDistance)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(directionX) * moveSpeed, 0f);

            animator.SetBool("isWalking", true);
            animator.SetBool("isPreparingShoot", false);

            if (directionX > 0 && !facingRight) Flip();
            else if (directionX < 0 && facingRight) Flip();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
            
            if (!isShootingActive)
            {
                if (directionX > 0 && !facingRight) Flip();
                else if (directionX < 0 && facingRight) Flip();

                StartCoroutine(ShootingCycle());
            }
        }
    }

    protected void Flip()
    {
        facingRight = !facingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

   
    protected abstract void PerformShoot();


    protected virtual IEnumerator ShootingCycle()
    {
        isShootingActive = true;

        
        animator.SetBool("isPreparingShoot", true);
        yield return new WaitForSeconds(prepareDuration);

        
        animator.SetBool("isPreparingShoot", false);
        animator.SetTrigger("ShootTrigger");
        
        
        for (int i = 0; i < burstCount; i++)
        {
            PerformShoot();
            
            
            yield return new WaitForSeconds(fireRateInterval); 
        }

        
        float burstDuration = burstCount * fireRateInterval;
        float remainingAnimationTime = shootAnimationDuration - burstDuration;

        if (remainingAnimationTime > 0)
        {
            
            yield return new WaitForSeconds(remainingAnimationTime);
        }
        
       
        if (timeBetweenShots > 0)
        {
            yield return new WaitForSeconds(timeBetweenShots);
        }
        
        isShootingActive = false;
    }
}