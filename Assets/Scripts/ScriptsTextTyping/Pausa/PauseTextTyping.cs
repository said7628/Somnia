using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject configuracionPausa;
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pauseTextTypingPrefab;

    private bool isPaused;
    private GameObject pausePrefabInstance;

    private void Start()
    {
        HidePauseUi();
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseButton == null)
        {
            pauseButton = GetComponentInChildren<Button>(true);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                Continuar();
            }
            else
            {
                AbrirPausa();
            }
        }
    }

    public void AbrirPausa()
    {
        Debug.Log("[TextTypingPause] Pause requested");

        if (!TryResolvePauseRoot(out GameObject pauseRoot))
        {
            return;
        }

        ForcePauseUiVisible(pauseRoot);

        Time.timeScale = 0f;
        isPaused = true;
        Debug.Log("[TextTypingPause] Pause UI shown");
    }

    private void OnPauseButtonClicked()
    {
        if (isPaused)
        {
            return;
        }

        AbrirPausa();
    }

    private bool TryResolvePauseRoot(out GameObject pauseRoot)
    {
        pauseRoot = pausePanel != null ? pausePanel : pausePrefabInstance;
        if (pauseRoot != null)
        {
            Debug.Log($"[TextTypingPause] Found existing pause root: {pauseRoot.name} activeSelf={pauseRoot.activeSelf} activeInHierarchy={pauseRoot.activeInHierarchy}");
            pausePanel = pauseRoot;
            return true;
        }

        if (pauseTextTypingPrefab == null)
        {
            Debug.LogError("[TextTypingPause][ERROR] Missing pause prefab/reference/parent: pauseTextTypingPrefab not assigned");
            return false;
        }

        Transform parent = ResolvePauseParent();
        if (parent == null)
        {
            Debug.LogError("[TextTypingPause][ERROR] Missing pause prefab/reference/parent: cannot resolve a Canvas parent");
            return false;
        }

        pausePrefabInstance = Instantiate(pauseTextTypingPrefab, parent);
        pausePanel = pausePrefabInstance;
        pauseRoot = pausePanel;

        RectTransform pauseRt = pauseRoot.GetComponent<RectTransform>();
        if (pauseRt != null)
        {
            pauseRt.anchorMin = Vector2.zero;
            pauseRt.anchorMax = Vector2.one;
            pauseRt.offsetMin = Vector2.zero;
            pauseRt.offsetMax = Vector2.zero;
            pauseRt.localScale = Vector3.one;
            pauseRt.SetAsLastSibling();
        }

        Debug.Log($"[TextTypingPause] Instantiated pause prefab under: {parent.name}");
        return true;
    }

    private Transform ResolvePauseParent()
    {
        Canvas ownCanvas = GetComponentInParent<Canvas>();
        if (ownCanvas != null)
        {
            return ownCanvas.transform;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].isRootCanvas)
            {
                return canvases[i].transform;
            }
        }

        return null;
    }

    private void ForcePauseUiVisible(GameObject pauseRoot)
    {
        pauseRoot.SetActive(true);

        if (pauseRoot.TryGetComponent(out CanvasGroup canvasGroup))
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Canvas rootCanvas = pauseRoot.GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            Canvas pauseCanvas = pauseRoot.GetComponent<Canvas>();
            if (pauseCanvas == null)
            {
                pauseCanvas = pauseRoot.AddComponent<Canvas>();
                pauseCanvas.overrideSorting = true;
            }

            pauseCanvas.sortingOrder = Mathf.Max(pauseCanvas.sortingOrder, rootCanvas.sortingOrder + 20);
            pauseCanvas.overrideSorting = true;

            if (pauseRoot.GetComponent<GraphicRaycaster>() == null)
            {
                pauseRoot.AddComponent<GraphicRaycaster>();
            }
        }

        if (configuracionPausa != null)
        {
            configuracionPausa.SetActive(false);
        }
    }

    public void Continuar()
    {
        Debug.Log("[TextTypingPause] Resume requested");
        HidePauseUi();

        Time.timeScale = 1f;
        isPaused = false;
        Debug.Log("[TextTypingPause] Pause UI hidden");
    }

    private void HidePauseUi()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (pausePrefabInstance != null)
        {
            pausePrefabInstance.SetActive(false);
        }

        if (configuracionPausa != null)
        {
            configuracionPausa.SetActive(false);
        }
    }

    public void Reintentar()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(currentScene);
    }

    public void IrAMapa()
    {
        string returnScene = TextTypingSession.ResolveReturnScene();

        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log(
            $"[TextTypingPause] Map return sourceIslandScene={TextTypingSession.SourceIslandSceneName} " +
            $"sourceLevelScene={SceneManager.GetActiveScene().name} returnPosition={TextTypingSession.ReturnPosition} " +
            $"returnRotation={TextTypingSession.ReturnRotation.eulerAngles} sceneLoadedByMapReturn={returnScene}"
        );

        SceneManager.LoadScene(returnScene);
    }

    public void IrAConfiguracion()
    {
        if (configuracionPausa != null)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            configuracionPausa.SetActive(true);
        }
    }

    public void VolverDesdeConfiguracion()
    {
        if (configuracionPausa != null)
        {
            configuracionPausa.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void IrAMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("Pantalla_principal");
    }
}