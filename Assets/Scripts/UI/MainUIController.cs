using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI 
{
    public class MainUIController : MonoBehaviour
    {
        [SerializeField]
        public Image soundButtonImage;
        [SerializeField]
        public Sprite iconSoundOn;
        [SerializeField]
        public Sprite iconSoundOff;
        
        private bool _soundOn = true;
        
        public void StartGame()
        {
            SceneManager.LoadScene("Level_01");
        }
    
        public void Exit()
        {
            Application.Quit();
        }

        public void UpdateSound()
        {
            _soundOn = !_soundOn;
            AudioListener.volume = _soundOn ? 0.6f : 0;
            
            soundButtonImage.sprite  = _soundOn ? iconSoundOn : iconSoundOff;
        }
    }
}