using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    
    [Header("Interface")]
    public Slider healthSlider;

    private bool _isDead = false;
    private Animator _animator;

    private void Start()
    {
        currentHealth = maxHealth;
        _animator = transform.Find("Sprite").GetComponent<Animator>();

        if(_animator == null)
        {
            Debug.LogError("Animator not found on PlayerHealth!");
        }
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        currentHealth -= amount;
        
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
        
        if (currentHealth <= 0)
        {
            Die();

            return;
        }
        
        _animator.SetTrigger("Hitted");
    }

    public void Die()
    {
        if (_isDead) return;
        
        _isDead = true;
        _animator.SetTrigger("Dead");
        
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Destroy(gameObject, 1.2f);
        
        string currentLevel = SceneManager.GetActiveScene().name;
        
        PlayerPrefs.SetString("CurrentLevel", currentLevel);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("GameOver");
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