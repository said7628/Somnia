using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Este script va en cada ranura (Ranura1, Ranura2, Ranura3)
// Aquí controlo cómo se ve cada slot: si está vacío o si muestra progreso
public class SaveRanura : MonoBehaviour
{
    [Header("UI")]
    // Este es el texto o imagen que dice "Vacío"
    [SerializeField] private GameObject vacioObject;

    // Esta es la imagen donde voy a mostrar la isla desbloqueada
    [SerializeField] private Image previewImage;

    // Este es el numerito del slot (1., 2., 3.)
    [SerializeField] private TMP_Text numText;

    // Este es el botón de toda la ranura (el mismo objeto padre)
    [SerializeField] private Button slotButton;

    [Header("Datos")]
    // Este es el número del slot (1, 2, 3)
    [SerializeField] private int slotIndex;

    [Header("Sprites por isla")]
    // Aquí asigno qué imagen corresponde a cada isla
    [SerializeField] private Sprite isla1Sprite;
    [SerializeField] private Sprite isla2Sprite;
    [SerializeField] private Sprite isla3Sprite;

    // Esto me deja acceder al índice del slot desde otros scripts
    public int SlotIndex => slotIndex;

    private void Reset()
    {
        // Esto solo sirve para autoconectar cosas en Unity (cuando agrego el script)

        // Busca automáticamente el objeto "Vacio"
        if (vacioObject == null) vacioObject = transform.Find("Vacio")?.gameObject;

        // Busca automáticamente la imagen de preview
        if (previewImage == null) previewImage = transform.Find("PreviewImage")?.GetComponent<Image>();

        // Busca el texto del número
        if (numText == null) numText = transform.Find("Num")?.GetComponent<TMP_Text>();

        // Toma el botón del mismo objeto
        if (slotButton == null) slotButton = GetComponent<Button>();
    }

    public void SetSlotNumber(int number)
    {
        // Aquí le pongo el número al slot (1., 2., 3.)
        if (numText != null)
            numText.text = number + ".";
    }

    public void ShowEmpty()
    {
        // Aquí hago que la ranura se vea como VACÍA

        // Activo el texto "Vacío"
        if (vacioObject != null)
            vacioObject.SetActive(true);

        // Apago la imagen de preview
        if (previewImage != null)
            previewImage.gameObject.SetActive(false);
    }

    public void ShowData(int highestUnlockedIsland)
    {
        // Aquí hago que la ranura muestre progreso

        // Apago el texto "Vacío"
        if (vacioObject != null)
            vacioObject.SetActive(false);

        // Activo la imagen de preview
        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(true);

            // Le pongo la imagen correcta dependiendo de la isla
            previewImage.sprite = GetIslandSprite(highestUnlockedIsland);
        }
    }

    private Sprite GetIslandSprite(int island)
    {
        // Aquí decido qué imagen usar según la isla desbloqueada

        switch (island)
        {
            case 1: return isla1Sprite;
            case 2: return isla2Sprite;
            case 3: return isla3Sprite;

            // Si algo falla, uso la isla 1 por defecto
            default: return isla1Sprite;
        }
    }
    
    public void ConfigureButton(UnityEngine.Events.UnityAction onClick)
    {
        // Este método conecta el botón con lo que debe pasar al hacer click

        // Si por alguna razón no tengo referencia al botón, lo agarro del objeto
        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (slotButton != null)
        {
            // Limpio cualquier función vieja del botón
            slotButton.onClick.RemoveAllListeners();

            // Le asigno la nueva función (la que me manda el script principal)
            slotButton.onClick.AddListener(onClick);
        }
    }
}