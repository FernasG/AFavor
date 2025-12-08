using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelLoader : MonoBehaviour
{
    [Header("Settings")]
    public string nextLevel;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerPrefs.SetString("CurrentLevel", nextLevel);
            PlayerPrefs.Save();

            SceneManager.LoadScene(nextLevel);
        }
    }
}
