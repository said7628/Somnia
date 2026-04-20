using UnityEngine;

public class PlayerCustomizationManager : MonoBehaviour
{
    public static PlayerCustomizationManager Instance;

    [Header("Selección actual")]
    public int selectedFaceColor = 0;
    public int selectedEyes = 0;
    public int selectedOutfit = 0;
    [SerializeField] private bool usarValoresDelInspector = true;
    private bool hasBeenInitialized = false;

    

    void Awake()
        {
            if (usarValoresDelInspector)
            {
                Debug.Log("Usando valores del Inspector");
                return;
            }

            LoadData();
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
        ApplyToPlayer();
    }

    public void SetEyes(int index)
    {
        selectedEyes = index;
        ApplyToPlayer();
    }

    public void SetOutfit(int index)
    {
        selectedOutfit = index;
        ApplyToPlayer();
    }
    void ApplyToPlayer()
    {
        PlayerVisual player = FindObjectOfType<PlayerVisual>();

        if (player != null)
        {
            player.ApplyCustomization();
        }
    }
}