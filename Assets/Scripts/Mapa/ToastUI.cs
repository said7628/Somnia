using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ToastUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float duracion = 2f;

    private Label toastLabel;
    private Coroutine toastCoroutine;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = FindFirstObjectByType<UIDocument>();
        }
    }

    private void Start()
    {
        CrearToast();
    }

    private void CrearToast()
    {
        if (uiDocument == null)
        {
            Debug.LogError("ToastUI: no se encontro UIDocument");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("ToastUI: rootVisualElement es null");
            return;
        }

        if (toastLabel != null)
            return;

        toastLabel = new Label();
        toastLabel.name = "toast-dinamico";
        toastLabel.text = "";

        toastLabel.style.position = Position.Absolute;
        toastLabel.style.left = Length.Percent(50);
        toastLabel.style.bottom = 40;
        toastLabel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);

        toastLabel.style.minWidth = 400;
        toastLabel.style.maxWidth = 800;

        toastLabel.style.paddingLeft = 30;
        toastLabel.style.paddingRight = 30;
        toastLabel.style.paddingTop = 14;
        toastLabel.style.paddingBottom = 14;

        toastLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.88f);
        toastLabel.style.color = Color.white;

        toastLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        toastLabel.style.fontSize = 22;

        toastLabel.style.borderTopLeftRadius = 12;
        toastLabel.style.borderTopRightRadius = 12;
        toastLabel.style.borderBottomLeftRadius = 12;
        toastLabel.style.borderBottomRightRadius = 12;

        toastLabel.style.display = DisplayStyle.None;
        toastLabel.style.opacity = 1f;
        toastLabel.pickingMode = PickingMode.Ignore;

        root.Add(toastLabel);

        Debug.Log("ToastUI: toast creado correctamente");
    }

    public void ShowToast(string mensaje)
    {
        if (toastLabel == null)
        {
            CrearToast();
        }

        if (toastLabel == null)
        {
            Debug.LogError("ToastUI: no se pudo crear el toast");
            return;
        }

        if (toastCoroutine != null)
        {
            StopCoroutine(toastCoroutine);
        }

        toastCoroutine = StartCoroutine(MostrarToastRutina(mensaje));
    }

    private IEnumerator MostrarToastRutina(string mensaje)
    {
        toastLabel.text = mensaje;
        toastLabel.style.display = DisplayStyle.Flex;
        toastLabel.style.opacity = 1f;
        toastLabel.BringToFront();

        Debug.Log("ToastUI: mostrando toast -> " + mensaje);

        yield return new WaitForSeconds(duracion);

        toastLabel.style.display = DisplayStyle.None;
        toastCoroutine = null;
    }
}