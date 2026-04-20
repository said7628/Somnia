using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuardadoBeta : MonoBehaviour
{
    [Header("Ranuras visuales")]
    [SerializeField] private SaveRanura[] ranuras;

    [Header("Toast opcional")]
    [SerializeField] private Somnia.UnityClient.ToastMessage toastMessage;

    [Header("Escenas")]
    [SerializeField] private string escenaMapa = "Mapa";
    [SerializeField] private string escenaMenuPrincipal = "Pantalla_principal";

    [Header("Comportamiento")]
    [SerializeField] private bool permitirEntrarASlotOcupado = false;

    private void Start()
    {
        InicializarRanuras();
    }

    private void InicializarRanuras()
    {
        if (ranuras == null || ranuras.Length == 0)
        {
            Debug.LogWarning("GuardadoBeta: no hay ranuras asignadas.");
            return;
        }

        for (int i = 0; i < ranuras.Length; i++)
        {
            if (ranuras[i] == null)
            {
                continue;
            }

            int slotNumber = i + 1;
            SaveSlotData data = LoadSlot(slotNumber);

            ranuras[i].SetSlotNumber(slotNumber);

            if (data.hasData)
            {
                ranuras[i].ShowData(data.highestUnlockedIsland);
            }
            else
            {
                ranuras[i].ShowEmpty();
            }

            int capturedSlot = slotNumber;
            ranuras[i].ConfigureButton(() => OnSlotClicked(capturedSlot));
        }
    }

    public void OnSlotClicked(int slotNumber)
    {
        SaveSlotData data = LoadSlot(slotNumber);

        if (!data.hasData)
        {
            CrearNuevaPartidaLocal(slotNumber);
            RefreshSlots();

            PlayerPrefs.SetInt("current_slot", slotNumber);
            PlayerPrefs.Save();

            SceneManager.LoadScene(escenaMapa);
            return;
        }

        if (!permitirEntrarASlotOcupado)
        {
            MostrarToast("Ese slot ya esta ocupado.");
            return;
        }

        PlayerPrefs.SetInt("current_slot", slotNumber);
        PlayerPrefs.Save();

        SceneManager.LoadScene(escenaMapa);
    }

    private void CrearNuevaPartidaLocal(int slotNumber)
    {
        int islaInicial = 1;
        SaveSlot(slotNumber, islaInicial);
    }

    public void RefreshSlots()
    {
        InicializarRanuras();
    }

    public void IrMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenuPrincipal);
    }

    private void MostrarToast(string mensaje)
    {
        if (toastMessage == null)
        {
            toastMessage = CrearToastAutomatico();
        }

        if (toastMessage != null)
        {
            toastMessage.Show(mensaje);
        }
        else
        {
            Debug.LogWarning(mensaje);
        }
    }

    private Somnia.UnityClient.ToastMessage CrearToastAutomatico()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("ToastCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("ToastPanel");
        panel.transform.SetParent(canvas.transform, false);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.78f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 80f);
        rect.sizeDelta = new Vector2(520f, 85f);

        GameObject textGO = new GameObject("ToastText");
        textGO.transform.SetParent(panel.transform, false);

        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 24;
        text.text = "";

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        Somnia.UnityClient.ToastMessage toast = panel.AddComponent<Somnia.UnityClient.ToastMessage>();

        var toastFieldRoot = typeof(Somnia.UnityClient.ToastMessage)
            .GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var toastFieldText = typeof(Somnia.UnityClient.ToastMessage)
            .GetField("messageText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (toastFieldRoot != null)
        {
            toastFieldRoot.SetValue(toast, panel);
        }

        if (toastFieldText != null)
        {
            toastFieldText.SetValue(toast, text);
        }

        return toast;
    }

    public static void SaveSlot(int slotNumber, int highestUnlockedIsland)
    {
        PlayerPrefs.SetInt($"slot_{slotNumber}_hasData", 1);
        PlayerPrefs.SetInt($"slot_{slotNumber}_highestIsland", highestUnlockedIsland);
        PlayerPrefs.Save();
    }

    public static SaveSlotData LoadSlot(int slotNumber)
    {
        SaveSlotData data = new SaveSlotData
        {
            hasData = PlayerPrefs.GetInt($"slot_{slotNumber}_hasData", 0) == 1,
            highestUnlockedIsland = PlayerPrefs.GetInt($"slot_{slotNumber}_highestIsland", 1)
        };

        return data;
    }

    public static void ClearSlot(int slotNumber)
    {
        PlayerPrefs.DeleteKey($"slot_{slotNumber}_hasData");
        PlayerPrefs.DeleteKey($"slot_{slotNumber}_highestIsland");
        PlayerPrefs.Save();
    }
}

[System.Serializable]
public class SaveSlotData
{
    public bool hasData;
    public int highestUnlockedIsland;
}