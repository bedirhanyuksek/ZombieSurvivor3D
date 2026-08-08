using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public sealed class ZGHud : MonoBehaviour
{
    [SerializeField] private ZGHealth playerHealth;
    [SerializeField] private ZGWeaponController weapon;
    [SerializeField] private Text healthText;
    [SerializeField] private Text ammoText;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text centerText;
    [SerializeField] private Text hitMarkerText;
    [SerializeField] private Image damageOverlay;
    [SerializeField] private Image endOverlay;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Button restartButton;
    [SerializeField] private ZGGameManager gameManager;

    private bool gameStarted;
    private bool ended;

    private void Start()
    {
        ZGSettings.Apply();
        if (endOverlay != null)
        {
            endOverlay.gameObject.SetActive(false);
        }
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
        }
        if (centerText != null)
        {
            centerText.text = string.Empty;
        }
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(ZGSettings.MasterVolume);
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(ZGSettings.AimSensitivity);
        }
        playerHealth.HealthChanged += UpdateHealth;
        playerHealth.Damaged += ShowDamageFlash;
        weapon.AmmoChanged += UpdateAmmo;
        weapon.HitConfirmed += ShowHitMarker;
        UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        ShowMainMenu();
    }

    private void Update()
    {
        if (!ended && ((mainMenuPanel != null && mainMenuPanel.activeSelf) || (settingsPanel != null && settingsPanel.activeSelf)))
        {
            Time.timeScale = 0f;
            ZGSettings.InputLockedByMenu = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (restartButton != null && restartButton.gameObject.activeSelf && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Restart();
        }
        if (gameStarted && !ended && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealth;
            playerHealth.Damaged -= ShowDamageFlash;
        }
        if (weapon != null)
        {
            weapon.AmmoChanged -= UpdateAmmo;
            weapon.HitConfirmed -= ShowHitMarker;
        }
    }

    public void SetObjective(int kills, int required)
    {
        objectiveText.text = $"Zombi: {kills}/{required}";
    }

    public void ShowMessage(string message)
    {
        centerText.text = message;
    }

    public void ShowGameOver()
    {
        ended = true;
        ZGSettings.InputLockedByMenu = false;
        if (endOverlay != null)
        {
            endOverlay.gameObject.SetActive(true);
            endOverlay.color = new Color(0.18f, 0f, 0f, 0.68f);
        }
        centerText.text = "ÖLDÜN";
        centerText.color = new Color(1f, 0.12f, 0.08f, 1f);
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }

    public void ShowWin()
    {
        ended = true;
        ZGSettings.InputLockedByMenu = false;
        if (endOverlay != null)
        {
            endOverlay.gameObject.SetActive(true);
            endOverlay.color = new Color(0f, 0.12f, 0.18f, 0.62f);
        }
        centerText.text = "KURTULDUN";
        centerText.color = new Color(0.35f, 0.95f, 1f, 1f);
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }

    public void StartGame()
    {
        gameStarted = true;
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        Time.timeScale = 1f;
        ZGSettings.InputLockedByMenu = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        Time.timeScale = 0f;
        ZGSettings.InputLockedByMenu = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (gameStarted && (mainMenuPanel == null || !mainMenuPanel.activeSelf))
        {
            Time.timeScale = 1f;
            ZGSettings.InputLockedByMenu = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnVolumeChanged(float value)
    {
        ZGSettings.MasterVolume = value;
    }

    public void OnSensitivityChanged(float value)
    {
        ZGSettings.AimSensitivity = value;
    }

    public void Restart()
    {
        ZGSettings.InputLockedByMenu = false;
        if (gameManager != null)
        {
            gameManager.Restart();
        }
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void UpdateHealth(int current, int max)
    {
        healthText.text = $"CAN  {current}/{max}";
    }

    private void UpdateAmmo(int current, int max, bool reloading)
    {
        ammoText.text = reloading ? "ŞARJÖR" : $"MERMİ  {current}/{max}";
    }

    private void ShowMainMenu()
    {
        gameStarted = false;
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        Time.timeScale = 0f;
        ZGSettings.InputLockedByMenu = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowHitMarker(bool headshot)
    {
        if (hitMarkerText == null)
        {
            return;
        }

        StopCoroutine(nameof(HitMarkerRoutine));
        StartCoroutine(HitMarkerRoutine(headshot));
    }

    private IEnumerator HitMarkerRoutine(bool headshot)
    {
        hitMarkerText.text = headshot ? "X" : "+";
        hitMarkerText.color = headshot ? new Color(1f, 0.08f, 0.04f, 1f) : new Color(1f, 1f, 1f, 1f);
        hitMarkerText.fontSize = headshot ? 42 : 34;
        yield return new WaitForSeconds(0.12f);
        hitMarkerText.text = string.Empty;
    }

    private void ShowDamageFlash(int amount)
    {
        if (damageOverlay == null)
        {
            return;
        }

        StopCoroutine(nameof(DamageFlashRoutine));
        StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        var color = damageOverlay.color;
        color.a = 0.45f;
        damageOverlay.color = color;
        var elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0.45f, 0f, elapsed / 0.35f);
            damageOverlay.color = color;
            yield return null;
        }
    }
}
