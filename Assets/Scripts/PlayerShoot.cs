using System;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Configurações")]
    public float speed = 20f;
    public float lifeTime = 2f;
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
        
        _renderer.transform.rotation.Set(0,0,0,spriteRotation);
        
        _rb.linearVelocity = isFacingRight ? transform.right * speed :  -transform.right * speed;
        
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Enemy enemy = hitInfo.GetComponent<Enemy>();
        // if (enemy != null)
        // {
        //     enemy.TakeDamage(damage);
        // }
        //
        // if (!hitInfo.CompareTag("Player")) 
        // {
        //     Destroy(gameObject);
        // }
    }
}
