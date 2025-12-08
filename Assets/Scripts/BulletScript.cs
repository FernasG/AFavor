using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private Rigidbody2D _rb;
    public float lifetime = 3f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void Launch(Vector2 direction, float speed)
    {
        _rb.linearVelocity = direction.normalized * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Enemy"))
        {
            return;
        }
        
        if (hitInfo.CompareTag("Player"))
        {
            PlayerHealth playerHealth = hitInfo.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(15);
            }

            Destroy(gameObject);
        }
        else if (!hitInfo.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}