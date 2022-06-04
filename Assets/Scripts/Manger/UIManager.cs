using UnityEngine;
using UnityEngine.UI;

namespace Manger
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("HEALTH")]
        public GameObject[] hearts;
        public Slider bossHealthSlider;

        [Header("Menu")] public GameObject pauseMenu;

        #region 血条控制

        public void UpdateHealth(float currentHealth)
        {
            for (var i = 0; i < 3 - currentHealth; i++)
            {
                hearts[i].SetActive(false);
            }
        }

        #endregion

        #region UI事件

        public void PauseGame()
        {
            pauseMenu.SetActive(true);
            Time.timeScale = 0;
        }
        
        public void ResumeGame()
        {
            pauseMenu.SetActive(false);
            Time.timeScale = 1;
        }

        #endregion
    }
}