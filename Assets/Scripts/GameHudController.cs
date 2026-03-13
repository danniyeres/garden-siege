using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameHudController : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Health playerHealth;
    [SerializeField] private Health cropsHealth;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text cropsHpText;
    [SerializeField] private bool forceHudLayout = true;

    [Header("Result UI")]
    [SerializeField] private bool createResultUiAtRuntime = true;
    [SerializeField] private bool pauseTimeOnResult = true;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private bool gameFinished;

    private void Awake()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        ResolveHealthReferences();

        if (waveText == null)
        {
            waveText = FindTextByName("Wave");
        }

        if (cropsHpText == null)
        {
            cropsHpText = FindTextByName("Crops HP");
        }

        if (forceHudLayout)
        {
            ApplyHudLayout();
        }

        EnsureResultUi();
        HideResultUi();
    }

    private void OnDestroy()
    {
        if (pauseTimeOnResult)
        {
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        ResolveReferencesIfNeeded();
        UpdateHudTexts();

        if (gameFinished)
        {
            return;
        }

        if (IsDefeat())
        {
            ShowResult(false);
            return;
        }

        if (IsVictory())
        {
            ShowResult(true);
        }
    }

    private void ResolveReferencesIfNeeded()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        ResolveHealthReferences();

        if (waveText == null)
        {
            waveText = FindTextByName("Wave");
        }

        if (cropsHpText == null)
        {
            cropsHpText = FindTextByName("Crops HP");
        }

        if (forceHudLayout)
        {
            ApplyHudLayout();
        }

        if (resultPanel == null || resultTitleText == null || restartButton == null || quitButton == null)
        {
            EnsureResultUi();
        }
    }

    private void ResolveHealthReferences()
    {
        if (playerHealth == null)
        {
            var player = GameObject.Find("JellyFishGirl");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }

        if (cropsHealth == null)
        {
            var crops = GameObject.Find("Crops");
            if (crops != null)
            {
                cropsHealth = crops.GetComponent<Health>();
            }
        }
    }

    private void UpdateHudTexts()
    {
        if (waveText != null && enemySpawner != null)
        {
            if (enemySpawner.IsAllWavesComplete)
            {
                waveText.text = $"Wave {enemySpawner.TotalWaves}/{enemySpawner.TotalWaves} Complete";
            }
            else
            {
                waveText.text =
                    $"Wave {enemySpawner.CurrentWaveNumber}/{enemySpawner.TotalWaves}: {enemySpawner.SpawnedInCurrentWave}/{enemySpawner.CurrentWaveEnemyCount}";
            }
        }

        if (cropsHpText != null && cropsHealth != null)
        {
            cropsHpText.text =
                $"Crops HP: {Mathf.CeilToInt(cropsHealth.CurrentHealth)}/{Mathf.CeilToInt(cropsHealth.MaxHealth)}";
        }
    }

    private bool IsDefeat()
    {
        var playerDead = playerHealth != null && !playerHealth.IsAlive;
        var cropsDead = cropsHealth != null && !cropsHealth.IsAlive;
        return playerDead || cropsDead;
    }

    private bool IsVictory()
    {
        if (enemySpawner == null || !enemySpawner.IsAllWavesComplete)
        {
            return false;
        }

        var aliveEnemies = FindObjectsByType<EnemyMoveToCrops>(FindObjectsSortMode.None);
        for (var i = 0; i < aliveEnemies.Length; i++)
        {
            var enemy = aliveEnemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            var health = enemy.GetComponent<Health>();
            if (health == null || health.IsAlive)
            {
                return false;
            }
        }

        return true;
    }

    private void ShowResult(bool victory)
    {
        gameFinished = true;

        if (victory)
        {
            GameAudioController.Instance.PlayVictory();
        }
        else
        {
            GameAudioController.Instance.PlayDefeat();
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = victory ? "VICTORY" : "GAME OVER";
            resultTitleText.color = victory ? new Color(0.55f, 1f, 0.55f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
        }

        if (pauseTimeOnResult)
        {
            Time.timeScale = 0f;
        }
    }

    private void HideResultUi()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void EnsureResultUi()
    {
        if (!createResultUiAtRuntime)
        {
            return;
        }

        var canvas = ResolveCanvas();
        if (canvas == null)
        {
            return;
        }

        if (resultPanel == null)
        {
            resultPanel = new GameObject("ResultPanel");
            resultPanel.transform.SetParent(canvas.transform, false);

            var panelRect = resultPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = resultPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        }

        if (resultTitleText == null)
        {
            var titleGo = new GameObject("ResultTitle");
            titleGo.transform.SetParent(resultPanel.transform, false);

            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(640f, 120f);
            titleRect.anchoredPosition = new Vector2(0f, 80f);

            resultTitleText = titleGo.AddComponent<TextMeshProUGUI>();
            resultTitleText.text = "GAME OVER";
            resultTitleText.fontSize = 72f;
            resultTitleText.alignment = TextAlignmentOptions.Center;
            resultTitleText.color = Color.white;
        }

        if (restartButton == null)
        {
            var buttonGo = new GameObject("RestartButton");
            buttonGo.transform.SetParent(resultPanel.transform, false);

            var buttonRect = buttonGo.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(260f, 70f);
            buttonRect.anchoredPosition = new Vector2(-140f, -30f);

            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0.95f);

            restartButton = buttonGo.AddComponent<Button>();
            var colors = restartButton.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.95f);
            colors.highlightedColor = new Color(0.95f, 1f, 0.95f, 1f);
            colors.pressedColor = new Color(0.82f, 0.92f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            restartButton.colors = colors;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(buttonGo.transform, false);

            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "RESTART";
            label.fontSize = 36f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.07f, 0.1f, 0.09f, 1f);

        }

        if (quitButton == null)
        {
            var buttonGo = new GameObject("QuitButton");
            buttonGo.transform.SetParent(resultPanel.transform, false);

            var buttonRect = buttonGo.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(260f, 70f);
            buttonRect.anchoredPosition = new Vector2(140f, -30f);

            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(1f, 0.92f, 0.92f, 0.95f);

            quitButton = buttonGo.AddComponent<Button>();
            var colors = quitButton.colors;
            colors.normalColor = new Color(1f, 0.92f, 0.92f, 0.95f);
            colors.highlightedColor = new Color(1f, 0.86f, 0.86f, 1f);
            colors.pressedColor = new Color(0.92f, 0.75f, 0.75f, 1f);
            colors.selectedColor = colors.highlightedColor;
            quitButton.colors = colors;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(buttonGo.transform, false);

            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "QUIT";
            label.fontSize = 36f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.2f, 0.06f, 0.06f, 1f);
        }

        if (restartButton != null)
        {
            ApplyResultButtonLayout(restartButton, new Vector2(-140f, -30f));
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartScene);
        }

        if (quitButton != null)
        {
            ApplyResultButtonLayout(quitButton, new Vector2(140f, -30f));
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private static void ApplyResultButtonLayout(Button button, Vector2 anchoredPosition)
    {
        if (button == null)
        {
            return;
        }

        var rect = button.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 70f);
        rect.anchoredPosition = anchoredPosition;
    }

    private Canvas ResolveCanvas()
    {
        if (waveText != null)
        {
            var waveCanvas = waveText.GetComponentInParent<Canvas>();
            if (waveCanvas != null)
            {
                return waveCanvas;
            }
        }

        if (cropsHpText != null)
        {
            var hpCanvas = cropsHpText.GetComponentInParent<Canvas>();
            if (hpCanvas != null)
            {
                return hpCanvas;
            }
        }

        return FindFirstObjectByType<Canvas>();
    }

    private static TMP_Text FindTextByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go == null)
        {
            return null;
        }

        return go.GetComponent<TMP_Text>();
    }

    private void ApplyHudLayout()
    {
        SetTopLeft(waveText, 20f, -80f, 420f, 60f);
        SetTopLeft(cropsHpText, 20f, -20f, 420f, 60f);
    }

    private static void SetTopLeft(TMP_Text text, float x, float y, float width, float height)
    {
        if (text == null)
        {
            return;
        }

        var rt = text.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
