using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip buttonClickAudio;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayGame()
    {
        if (audioSource != null && buttonClickAudio != null)
            audioSource.PlayOneShot(buttonClickAudio);

        GameState.level = 1;
        GameState.state = GameState.GamePlay;
        SceneManager.LoadScene("Level 1");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
