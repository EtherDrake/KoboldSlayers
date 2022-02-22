using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndGameController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

    public void MainMenu()
    {
        
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void Settings()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(4);
    }
    

    public void ResetSettings()
    {
        PlayerPrefs.SetFloat("SFXVol", 1f);
        PlayerPrefs.SetFloat("MusicVol", 1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void BunnyLink()
    {
        Application.OpenURL("https://www.patreon.com/necrobunnystudios/");
    }

    public void HastingsLink()
    {
        Application.OpenURL("https://twitter.com/HastingsDev");
    }
}
