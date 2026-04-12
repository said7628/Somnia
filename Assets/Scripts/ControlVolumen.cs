using UnityEngine;
using UnityEngine.UIElements;

public class ControlVolumen : MonoBehaviour
{
    [SerializeField] private int nivelAudioGeneral = 5;
    [SerializeField] private int nivelMusica = 5;

    private UIDocument uiDocument;

    private VisualElement[] barrasAudio = new VisualElement[5];
    private VisualElement[] barrasMusica = new VisualElement[5];

    private Button[] botonesAudio = new Button[6];
    private Button[] botonesMusica = new Button[6];

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();

        var root = uiDocument.rootVisualElement;

        for (int i = 0; i < 5; i++)
        {
            barrasAudio[i] = root.Q<VisualElement>("ALleno" + (i + 1));
            barrasMusica[i] = root.Q<VisualElement>("Lleno" + (i + 1));
        }

        for (int i = 0; i < 6; i++)
        {
            botonesAudio[i] = root.Q<Button>("BAudio" + i);
            int nivelA = i;
            botonesAudio[i].clicked += () => SeleccionarNivelAudio(nivelA);

            botonesMusica[i] = root.Q<Button>("BMusica" + i);
            int nivelM = i;
            botonesMusica[i].clicked += () => SeleccionarNivelMusica(nivelM);
        }

        ActualizarVisualAudio();
        ActualizarVisualMusica();
    }

    public void SeleccionarNivelAudio(int nuevoNivel)
    {
        nivelAudioGeneral = Mathf.Clamp(nuevoNivel, 0, 5);
        ActualizarVisualAudio();
    }

    public void SeleccionarNivelMusica(int nuevoNivel)
    {
        nivelMusica = Mathf.Clamp(nuevoNivel, 0, 5);
        ActualizarVisualMusica();
    }

    private void ActualizarVisualAudio()
    {
        for (int i = 0; i < 5; i++)
        {
            barrasAudio[i].style.display = i < nivelAudioGeneral
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }

    private void ActualizarVisualMusica()
    {
        for (int i = 0; i < 5; i++)
        {
            barrasMusica[i].style.display = i < nivelMusica
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}