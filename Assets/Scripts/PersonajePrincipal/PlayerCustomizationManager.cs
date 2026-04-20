using UnityEngine;

public class PlayerCustomizationManager : MonoBehaviour
{
    public static PlayerCustomizationManager Instance;

    [Header("Selección actual")]
    public int selectedFaceColor;
    public int selectedEyes;
    public int selectedOutfit;

    private bool hasBeenInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadData()
    {
        if (PlayerPrefs.HasKey("HasCustomization"))
        {
            selectedFaceColor = PlayerPrefs.GetInt("Face");
            selectedEyes = PlayerPrefs.GetInt("Eyes");
            selectedOutfit = PlayerPrefs.GetInt("Outfit");
        }
        else
        {
            SetDefaults();
        }
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt("Face", selectedFaceColor);
        PlayerPrefs.SetInt("Eyes", selectedEyes);
        PlayerPrefs.SetInt("Outfit", selectedOutfit);
        PlayerPrefs.SetInt("HasCustomization", 1);
    }

    void SetDefaults()
    {
        Debug.Log("Aplicando apariencia por defecto");

        selectedFaceColor = 0;
        selectedEyes = 0;
        selectedOutfit = 0;
    }

    public void SetFace(int index)
    {
        selectedFaceColor = index;
    }

    public void SetEyes(int index)
    {
        selectedEyes = index;
    }

    public void SetOutfit(int index)
    {
        selectedOutfit = index;
    }
}