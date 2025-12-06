/*using UnityEngine;

public class EnforcerEnemy : EnemyBase
{
    [Header("Tiro do Enforcer")]
    public GameObject bulletPrefab;
    public Transform shootPoint;
    public float bulletSpeed = 10f;
    
    public float maxSpreadAngle = 0f; 

    protected void PerformShoot()
    {
        if (player == null || bulletPrefab == null || shootPoint == null)
        {
            Debug.LogError("Falha ao instanciar: Bullet Prefab ou Shoot Point está nulo. Verifique o Inspector.");
            return;
        }

        float horizontalDirection = facingRight ? 1f : -1f;
        Vector2 baseShootDirection = new Vector2(horizontalDirection, 0f);
        
        float spread = Random.Range(-maxSpreadAngle / 2f, maxSpreadAngle / 2f);
        
        Quaternion spreadRotation = Quaternion.AngleAxis(spread, Vector3.forward);
        Vector2 finalDirection = spreadRotation * baseShootDirection;
        
        GameObject newBullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity); 
        
        Bullet bulletScript = newBullet.GetComponent<Bullet>();
        
        if (bulletScript != null)
        {
            bulletScript.Launch(finalDirection, bulletSpeed); 
        }
        else
        {
            Debug.LogError("O Prefab da bala não possui o componente Bullet.cs!");
        }
    }

    protected override void Attack()
    {
        PerformShoot();
    }

    protected override void OnDeathAnimation()
    {
        if (animator) animator.SetTrigger("Die");
    }
}
*/