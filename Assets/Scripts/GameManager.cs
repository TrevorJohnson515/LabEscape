using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneID // these should be in the same order as the scenes are in the build manager
{
    title,
    game
}

public enum PlayState
{
    playing,
    paused,
    gameOver,
    loading
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public delegate void GameEvent();
    public event GameEvent initializeLevel;
    public event GameEvent initializeOthers;
    public event GameEvent gameUpdate;

    public SceneID currentScene;
    public SettingsPreset settingsPreset;
    public PlayState playState;

    private GameUI gameUI;

    public GameSettings settings { get; private set; }
    public float gameTime { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        SetSettings(settingsPreset);
        InitializeScene(currentScene);
    }

    public void SetSettings(SettingsPreset preset)
    {
        settings = GameSettings.presets[preset];
        if (settingsPreset != preset)
        {
            if (currentScene == SceneID.title)
            {
                initializeLevel();
            }
        }
        switch (preset)
        {
            case SettingsPreset.easy:
                SoundManager.instance.SwitchMusic(0);
                break;
            case SettingsPreset.medium:
                SoundManager.instance.SwitchMusic(1);
                break;
            case SettingsPreset.hard:
                SoundManager.instance.SwitchMusic(2);
                break;
            default:
                SoundManager.instance.SwitchMusic(-1);
                break;
        }
        settingsPreset = preset;
    }

    public void LoadScene(SceneID scene)
    {
        AsyncOperation asyncLoad;
        playState = PlayState.loading;
        asyncLoad = SceneManager.LoadSceneAsync((int) scene);
        // Loading bar here???
        asyncLoad.completed += operation =>
        {
            InitializeScene(scene);
        };
    }
    private void InitializeScene(SceneID scene)
    {
        currentScene = scene;
        switch (scene)
        {
            case SceneID.title:
                gameTime = 0;
                LevelController.instance.levelWidth = 10;
                LevelController.instance.transform.position = Camera.main.ScreenToWorldPoint(Vector3.zero) + Vector3.forward * 10;
                if (initializeLevel != null)
                {
                    initializeLevel();
                }
                break;
            case SceneID.game:
                gameTime = 0;
                gameUI = GameObject.Find("UI").GetComponent<GameUI>();
                if (gameUI == null)
                {
                    Debug.LogError("Failed to find the GameUI script", transform);
                }
                ResumeGame();
                settings = GameSettings.presets[settingsPreset];
                LevelController.instance.levelWidth = settings.levelWidth;
                if (initializeLevel != null)
                {
                    initializeLevel();
                }
                if (initializeOthers != null)
                {
                    initializeOthers();
                }
                break;
        }
    }

    private void Update()
    {
        switch (currentScene)
        {
            case SceneID.title:
                gameTime += Time.deltaTime;
                if (gameUpdate != null)
                {
                    gameUpdate();
                }
                LevelController.instance.transform.position = Camera.main.ScreenToWorldPoint(Vector3.zero) + Vector3.forward * 10;
                if (Random.Range(0f, 1f) < 0.01)
                {
                    LevelController.instance.RandomShift();
                }
                break;
            case SceneID.game:
                switch (playState)
                {
                    case PlayState.playing:
                        gameTime += Time.deltaTime;
                        if (gameUpdate != null)
                        {
                            gameUpdate();
                        }
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            PauseGame();
                        }
                        break;
                    case PlayState.paused:
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            ResumeGame();
                        }
                        if (Input.GetKeyDown(KeyCode.M))
                        {
                            LoadScene(SceneID.title);
                        }
                        break;
                    case PlayState.gameOver:
                        if (Input.GetKeyDown(KeyCode.R))
                        {
                            LoadScene(SceneID.game);
                        }
                        else if (Input.GetKeyDown(KeyCode.M))
                        {
                            LoadScene(SceneID.title);
                        }
                        break;
                }
                break;
        }
    }

    public void GameOver()
    {
        if (currentScene == SceneID.game)
        {
            gameUI.GameOverScreen();
            playState = PlayState.gameOver;
            SoundManager.instance.GameOver();
        }
    }
    public void PauseGame()
    {
        if (currentScene == SceneID.game)
        {
            gameUI.PausedScreen();
            playState = PlayState.paused;
        }
    }
    public void ResumeGame()
    {
        if (currentScene == SceneID.game)
        {
            gameUI.PlayingScreen();
            playState = PlayState.playing;
        }
    }
}

