using UnityEngine;
using UnityEngine.SceneManagement;

// Este script es el que controla TODO el menú de guardado
// O sea, aquí decido qué ranura está vacía, cuál tiene datos
// y qué pasa cuando le das click a una ranura
public class GuardadoBeta : MonoBehaviour
{
    [Header("Ranuras")]
    // Aquí guardo las referencias a mis 3 ranuras (Ranura1, Ranura2, Ranura3)
    // Estas las arrastro desde el Inspector
    [SerializeField] private SaveRanura[] ranuras;

    [Header("Escena a cargar")]
    // Esta es la escena a la que voy a mandar al jugador después de elegir ranura
    // En mi caso es el mapa
    [SerializeField] private string escenaMapa = "Mapa";

    private void Start()
    {
        // Aquí inicializo todo el menú (o sea, preparo las ranuras)
        InicializarRanuras();
    }

    private void InicializarRanuras()
    {
        // Aquí recorro todas las ranuras (1, 2 y 3)
        for (int i = 0; i < ranuras.Length; i++)
        {
            // El índice empieza en 0, pero yo quiero slots 1, 2, 3
            int slotNumber = i + 1;

            // Aquí cargo los datos guardados de esa ranura
            SaveSlotData data = LoadSlot(slotNumber);

            // Le pongo el número a la ranura (1., 2., 3.)
            ranuras[i].SetSlotNumber(slotNumber);

            // Aquí decido qué mostrar:
            // si hay datos → enseño la imagen de la isla
            // si no hay → enseño "Vacío"
            if (data.hasData)
                ranuras[i].ShowData(data.highestUnlockedIsland);
            else
                ranuras[i].ShowEmpty();

            // Esto es importante:
            // guardo el número del slot para usarlo en el botón
            int capturedSlot = slotNumber;

            // Aquí configuro el click de la ranura
            // o sea, cuando le doy click, llamo a OnSlotClicked con el número correcto
            ranuras[i].ConfigureButton(() => OnSlotClicked(capturedSlot));
        }
    }

    public void OnSlotClicked(int slotNumber)
    {
        // Cuando le doy click a una ranura, primero veo si ya tenía datos
        SaveSlotData data = LoadSlot(slotNumber);

        if (!data.hasData)
        {
            // Si NO tenía datos:
            // creo una nueva partida en ese slot (empieza en isla 1)
            SaveSlot(slotNumber, 1);

            // Guardo cuál slot estoy usando actualmente
            PlayerPrefs.SetInt("current_slot", slotNumber);
            PlayerPrefs.Save();

            // Y me voy al mapa
            SceneManager.LoadScene(escenaMapa);
        }
        else
        {
            // Si ya tenía datos:
            // solo guardo qué slot estoy usando
            PlayerPrefs.SetInt("current_slot", slotNumber);
            PlayerPrefs.Save();

            // y me voy al mapa (pero ahora con progreso cargado)
            SceneManager.LoadScene(escenaMapa);
        }
    }

    public void RefreshSlots()
    {
        // Esto lo puedo usar si quiero actualizar las ranuras en tiempo real
        // (por ejemplo, después de borrar o guardar algo)
        InicializarRanuras();
    }

    public static void SaveSlot(int slotNumber, int highestUnlockedIsland)
    {
        // Aquí guardo datos en PlayerPrefs (una forma simple de guardar cosas)

        // Marco que este slot ya tiene datos
        PlayerPrefs.SetInt($"slot_{slotNumber}_hasData", 1);

        // Guardo cuál es la isla más alta desbloqueada
        PlayerPrefs.SetInt($"slot_{slotNumber}_highestIsland", highestUnlockedIsland);

        PlayerPrefs.Save();
    }

    public static SaveSlotData LoadSlot(int slotNumber)
    {
        // Aquí leo los datos guardados

        SaveSlotData data = new SaveSlotData
        {
            // Veo si el slot tiene datos (1 = sí, 0 = no)
            hasData = PlayerPrefs.GetInt($"slot_{slotNumber}_hasData", 0) == 1,

            // Leo la isla más alta desbloqueada (por default es 1)
            highestUnlockedIsland = PlayerPrefs.GetInt($"slot_{slotNumber}_highestIsland", 1)
        };

        return data;
    }

    public static void ClearSlot(int slotNumber)
    {
        // Esto borra completamente una ranura

        PlayerPrefs.DeleteKey($"slot_{slotNumber}_hasData");
        PlayerPrefs.DeleteKey($"slot_{slotNumber}_highestIsland");

        PlayerPrefs.Save();
    }

    public void IrMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Pantalla_principal");
    }
}

// Esta clase es solo para agrupar los datos de una ranura
// básicamente es como un "paquetito" de info
[System.Serializable]
public class SaveSlotData
{
    public bool hasData;                 // si tiene datos o no
    public int highestUnlockedIsland;   // hasta qué isla llegó
}