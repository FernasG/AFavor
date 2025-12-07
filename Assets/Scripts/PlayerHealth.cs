using UnityEngine;
using UnityEditor;
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    public Animator animator;

    private bool isDead = false;

    private void Awake()
    {
    }

    private void Start()
    {
        currentHealth = maxHealth;
        animator = transform.Find("Sprite").GetComponent<Animator>();

        if(animator == null)
        {
            Debug.LogError("Animator not found on PlayerHealth!");
        }
    }

    private void Update()
    {
        
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        animator.SetTrigger("Hitted");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        animator.SetTrigger("Dead");

        // desativa controles mas NÃO destrói imediatamente
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // destrói depois
        Destroy(gameObject, 1.2f);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerHealth))]
public class PlayerHealthEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerHealth ph = (PlayerHealth)target;

        if (GUILayout.Button("Simular Hit"))
        {
            ph.TakeDamage(10);
        }

        if (GUILayout.Button("Simular Morte"))
        {
            ph.Die();
        }
    }
}
#endif