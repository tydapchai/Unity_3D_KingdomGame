using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Loader"); 
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}