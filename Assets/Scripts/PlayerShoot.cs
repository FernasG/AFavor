using System;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Configurações")]
    public float speed = 20f;
    public float lifeTime = 0.2f;
    public int damage = 10;

    private Rigidbody2D _rb;
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Launch(bool isFacingRight)
    {
        float spriteRotation = isFacingRight ? -270 : 90;

        _renderer.transform.rotation.Set(0, 0, 0, spriteRotation);

        _rb.linearVelocity = isFacingRight ? transform.right * speed : -transform.right * speed;

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Verifica se atingiu o inimigo    
        Debug.Log("entrou no trigger de tiro do inimigo ");
        AssaultEnemy enemy = hitInfo.GetComponent<AssaultEnemy>();

        if (enemy != null)
        {
            enemy.TakeHit(damage);  // Chama seu método no inimigo
            Destroy(gameObject);
            return;
        }
    }
}
