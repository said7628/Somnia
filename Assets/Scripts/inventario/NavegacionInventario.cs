using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class NavegacionInventario : MonoBehaviour
{
    private VisualElement root;

    private VisualElement botonCara;
    private VisualElement botonOjos;
    private VisualElement botonOutfit;
    private VisualElement botonConf;
    private VisualElement botonHome;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        ObtenerReferencias();
        RegistrarEventos();
    }

    void ObtenerReferencias()
    {
        botonCara = root.Q<VisualElement>("botoncara");
        botonOjos = root.Q<VisualElement>("botonojos");
        botonOutfit = root.Q<VisualElement>("botonoutfit");
        botonConf = root.Q<VisualElement>("botonconf");
        botonHome = root.Q<VisualElement>("botonHome");
    }

    void RegistrarEventos()
    {
        if (botonCara != null)
            botonCara.RegisterCallback<ClickEvent>(evt => IrAEscena("Inventario"));

        if (botonOjos != null)
            botonOjos.RegisterCallback<ClickEvent>(evt => IrAEscena("InventarioOjos"));

        if (botonOutfit != null)
            botonOutfit.RegisterCallback<ClickEvent>(evt => IrAEscena("InventarioOutfit"));

        if (botonConf != null)
            botonConf.RegisterCallback<ClickEvent>(evt => IrAEscena("InventarioConfiguracion"));

        if (botonHome != null)
            botonHome.RegisterCallback<ClickEvent>(evt => IrAEscena("Pantalla_Principal"));
    }

    void IrAEscena(string escena)
    {
        SceneManager.LoadScene(escena);
    }
}