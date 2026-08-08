using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ZGGameManager : MonoBehaviour
{
    [SerializeField] private ZGHealth playerHealth;
    [SerializeField] private ZGSpawner spawner;
    [SerializeField] private ZGHelicopterLanding helicopter;
    [SerializeField] private ZGHud hud;
    [SerializeField] private GameObject winZone;

    private int kills;
    private bool ended;

    public int Kills => kills;
    public int RequiredKills => spawner != null ? spawner.TotalToSpawn : 0;

    private void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += GameOver;
        }

        if (winZone != null)
        {
            winZone.SetActive(false);
        }

        hud?.SetObjective(kills, RequiredKills);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= GameOver;
        }
    }

    public void RegisterZombieKilled()
    {
        if (ended)
        {
            return;
        }

        kills++;
        hud?.SetObjective(kills, RequiredKills);
        if (kills >= RequiredKills)
        {
            spawner?.Stop();
            helicopter?.BeginLanding();
            if (winZone != null)
            {
                winZone.SetActive(true);
            }
            hud?.ShowMessage("Helikoptere ulaş!");
        }
    }

    public void Win()
    {
        if (ended)
        {
            return;
        }

        ended = true;
        spawner?.Stop();
        hud?.ShowWin();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void GameOver()
    {
        if (ended)
        {
            return;
        }

        ended = true;
        spawner?.Stop();
        hud?.ShowGameOver();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
