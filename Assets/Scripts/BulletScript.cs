using UnityEngine;

// Garante que o Rigidbody2D existe no objeto da bala
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private Rigidbody2D rb;
    public float lifetime = 3f; // Tempo de vida da bala

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Garante que a bala não seja afetada pela gravidade para ter uma trajetória perfeitamente reta
        rb.gravityScale = 0;
        // Garante que a bala não é cinemática para podermos usar rb.velocity
        rb.isKinematic = false;
    }

    // Esta função é chamada SOMENTE UMA VEZ pelo inimigo no momento do disparo.
    public void Launch(Vector2 direction, float speed)
    {
        // Aplica a velocidade imediatamente. Como só é chamada uma vez, 
        // a direção (direction * speed) será fixa e a trajetória será linear.
        rb.linearVelocity = direction.normalized * speed;

        // Define a rotação (opcional, apenas visual)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Start()
    {
        // A bala se autodestrói após 3 segundos
        Destroy(gameObject, lifetime);
    }

    // Usaremos a colisão sólida (OnCollisionEnter2D) para garantir que funcione
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // A colisão só funciona se a bala e o Player tiverem Rigidbody2D e Collider2D

        // 1. Atingiu o Player?
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player Atingido!");
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(15); // coloque o valor de dano da bala
            }

            // Implemente aqui a lógica de dano ao Player
            Destroy(gameObject);
        }
        // 2. Atingiu qualquer outra coisa que não seja outro inimigo
        else if (!collision.gameObject.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}