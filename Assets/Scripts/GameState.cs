using UnityEngine;
using UnityEngine.SceneManagement;

public class GameState : MonoBehaviour
{
    public const int Title = 0;
    public const int GamePlay = 1;
    public const int GameOver = 2;
    public const int LevelComplete = 3;
    public static int state;
    public static int level = 1;
    public static string lastLevelScene = "";
    private static GameState instance;
    private float stateTimer = 0f;
    private bool initializedStateTimer = false;
    public AudioClip bgmGameOver;
    [Range(0f, 1f)]
    public float bgmGameOverVolume = 1f;
    public AudioClip guardDetectedSound;
    public AudioSource audioSource;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        state = (currentScene == "Title" || currentScene == "Game Over") ? Title : GamePlay;
    }

    void Update()
    {
        if (state == GameOver)
        {
            if (!initializedStateTimer)
            {
                stateTimer = 30f;
                initializedStateTimer = true;
            }
            else
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                    ReturnToMainMenu();
            }
        }
        else if (state == LevelComplete)
        {
            if (!initializedStateTimer)
            {
                stateTimer = 30f;
                initializedStateTimer = true;
            }
            else
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                    NextLevel();
            }
        }
    }

    void OnGUI()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Title")
            return;

        if (state == GameOver)
        {
            float boxWidth = 400f;
            float boxHeight = 80f;
            float centerX = Screen.width * 0.5f - boxWidth * 0.5f;
            float centerY = Screen.height * 0.5f - boxHeight * 1.5f;

            GUI.skin.box.fontSize = 50;
            GUI.Box(new Rect(centerX, centerY, boxWidth, boxHeight), "GAME OVER!");

            GUI.skin.box.fontSize = 30;
            GUI.Box(new Rect(centerX + 50, centerY + boxHeight + 10, boxWidth - 100, 50),
                "Main Menu in " + Mathf.CeilToInt(stateTimer) + "s");

            if (GUI.Button(new Rect(centerX - 10, centerY + boxHeight + 80, 150, 50), "Retry"))
            {
                RetryLevel();
            }
            if (GUI.Button(new Rect(centerX + boxWidth - 140, centerY + boxHeight + 80, 150, 50), "Main Menu"))
            {
                ReturnToMainMenu();
            }
        }
        else if (state == LevelComplete)
        {
            float boxWidth = 500f;
            float boxHeight = 70f;
            float centerX = Screen.width * 0.5f - boxWidth * 0.5f;
            float centerY = Screen.height * 0.5f - boxHeight * 1.5f;

            GUI.skin.box.fontSize = 45;
            GUI.Box(new Rect(centerX, centerY, boxWidth, boxHeight), "MISSION COMPLETE!");

            GUI.skin.box.fontSize = 30;
            GUI.Box(new Rect(centerX + 50, centerY + boxHeight + 10, boxWidth - 100, 50),
                "Next Level in " + Mathf.CeilToInt(stateTimer) + "s");

            float buttonWidth = 150f;
            float buttonHeight = 50f;
            float spacing = 20f;
            float totalWidth = buttonWidth * 3 + spacing * 2;
            float startX = Screen.width * 0.5f - totalWidth * 0.5f;
            float buttonsY = centerY + boxHeight + 80;

            if (GUI.Button(new Rect(startX, buttonsY, buttonWidth, buttonHeight), "Next Level"))
            {
                NextLevel();
            }
            if (GUI.Button(new Rect(startX + buttonWidth + spacing, buttonsY, buttonWidth, buttonHeight), "Retry"))
            {
                RetryLevel();
            }
            if (GUI.Button(new Rect(startX + (buttonWidth + spacing) * 2, buttonsY, buttonWidth, buttonHeight), "Main Menu"))
            {
                ReturnToMainMenu();
            }
        }
    }

    public void RetryLevel()
    {
        initializedStateTimer = false;
        state = GamePlay;
        GuardPatrol.freezeAllGuards = false;
        GuardPatrol.alertTriggered = false;
        if (audioSource != null)
            audioSource.Stop();
        if (!string.IsNullOrEmpty(lastLevelScene))
            SceneManager.LoadScene(lastLevelScene);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        initializedStateTimer = false;
        state = GamePlay;
        GuardPatrol.freezeAllGuards = false;
        GuardPatrol.alertTriggered = false;
        if (audioSource != null)
            audioSource.Stop();
        if (SceneManager.GetActiveScene().buildIndex >= 3)
            SceneManager.LoadScene("Title");
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void ReturnToMainMenu()
    {
        initializedStateTimer = false;
        state = Title;
        GuardPatrol.freezeAllGuards = false;
        GuardPatrol.alertTriggered = false;
        if (audioSource != null)
            audioSource.Stop();
        SceneManager.LoadScene("Title");
    }

    public static void TriggerGameOver()
    {
        if (instance != null && state != GameOver)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "Title" && currentScene != "Game Over")
                lastLevelScene = currentScene;

            state = GameOver;
            instance.initializedStateTimer = false;

            if (instance.guardDetectedSound != null)
                instance.audioSource.PlayOneShot(instance.guardDetectedSound);
            if (instance.bgmGameOver != null)
            {
                instance.audioSource.clip = instance.bgmGameOver;
                instance.audioSource.loop = false;
                instance.audioSource.volume = instance.bgmGameOverVolume;
                instance.audioSource.Play();
            }
            SceneManager.LoadScene("Game Over");
        }
    }


    public static void TriggerLevelComplete()
    {
        if (instance != null && state != LevelComplete)
        {
            state = LevelComplete;
            instance.initializedStateTimer = false;
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                Player player = playerObj.GetComponent<Player>();
                if (player != null)
                    player.PerformLevelComplete();
            }
        }
    }
}
