using UnityEngine;
using UnityEngine.UI;
using System.IO.Ports;
[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text livesText;
    [SerializeField] private float gameOverRestartDelay = 5f; // Time in seconds before auto-restart

    private Player player;
    private Invaders invaders;
    private MysteryShip mysteryShip;
    private Bunker[] bunkers;
    private bool isGameOver = false;
    public int score { get; private set; } = 0;
    public int lives { get; private set; } = 3;

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        player = FindObjectOfType<Player>();
        invaders = FindObjectOfType<Invaders>();
        mysteryShip = FindObjectOfType<MysteryShip>();
        bunkers = FindObjectsOfType<Bunker>();
        NewGame();
    }

    private void Update()
    {
        // Handle manual game restart with keyboard (optional, can be removed)
        if (isGameOver && Input.GetKeyDown(KeyCode.Return))
        {
            CancelInvoke(nameof(NewGame)); // Cancel the auto-restart timer
            NewGame();
        }
    }

    private void NewGame()
    {
        isGameOver = false;
        gameOverUI.SetActive(false);
        SetScore(0);
        SetLives(3);
        NewRound();
    }

    private void NewRound()
    {
        invaders.ResetInvaders();
        invaders.gameObject.SetActive(true);
        for (int i = 0; i < bunkers.Length; i++)
        {
            bunkers[i].ResetBunker();
        }
        Respawn();
    }

    private void Respawn()
    {
        Vector3 position = player.transform.position;
        position.x = 0f;
        player.transform.position = position;
        player.gameObject.SetActive(true);
    }

    private void GameOver()
    {
        isGameOver = true;
        gameOverUI.SetActive(true);
        invaders.gameObject.SetActive(false);

        // Schedule automatic restart after the delay
        Invoke(nameof(NewGame), gameOverRestartDelay);
    }

    private void SetScore(int score)
    {
        this.score = score;
        scoreText.text = score.ToString().PadLeft(4, '0');
    }

    private void SetLives(int lives)
    {
        this.lives = Mathf.Max(lives, 0);
        livesText.text = this.lives.ToString();
    }

    public void OnPlayerKilled(Player player)
    {
        SetLives(lives - 1);
        player.gameObject.SetActive(false);
        if (lives > 0)
        {
            Invoke(nameof(NewRound), 1f);
        }
        else
        {
            GameOver();
        }
    }

    public void OnInvaderKilled(Invader invader)
    {
        invader.gameObject.SetActive(false);
        SetScore(score + invader.score);

        // Send serial command to light up green LED - FIXED VERSION
        if (player != null && player.enabled)
        {
            player.SendCommand("INVADER_KILLED");
        }

        if (invaders.GetAliveCount() == 0)
        {
            NewRound();
        }
    }

    public void OnMysteryShipKilled(MysteryShip mysteryShip)
    {
        SetScore(score + mysteryShip.score);
    }

    public void OnBoundaryReached()
    {
        if (invaders.gameObject.activeSelf)
        {
            invaders.gameObject.SetActive(false);
            OnPlayerKilled(player);
        }
    }
}