using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class NavegacionInventario : MonoBehaviour
{
    private VisualElement root;

    private Button botonConf;
    private Button botonHome;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        ObtenerReferencias();
        RegistrarEventos();
    }

    void ObtenerReferencias()
    {
        botonConf = root.Q<Button>("botonconf");
        botonHome = root.Q<Button>("botonHome");
    }

    void RegistrarEventos()
    {
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