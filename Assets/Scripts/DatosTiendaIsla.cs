using UnityEngine;

// Esta línea mops va a permitir crear archivos desde el menú de Unity
[CreateAssetMenu(fileName = "NuevaTiendaIsla", menuName = "Somnia/Datos de Tienda")]
public class DatosTiendaIsla : ScriptableObject
{
    [Header("Información de la Isla")]
    public string nombreIsla;
    
    // Aquí guardaremos la clase de CSS (uss) que le dará el color diferente a cada tienda
    public string claseEstiloUSS; 

    [Header("Inventario Disponible")]
    // Aquí están los arreglos que van a guardar las difernetes imagenes de las islas
    public Sprite[] opcionesRopa;
    public Sprite[] opcionesOjos;
    public Sprite[] opcionesColor;
}