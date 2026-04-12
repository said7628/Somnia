using UnityEngine;
using UnityEngine.UIElements;

public class PalpitarContinuar : MonoBehaviour
{
    private UIDocument uiDocument;
    private Button continuar;
    private bool grande = false;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("No hay UIDocument en este objeto.");
            return;
        }

        continuar = uiDocument.rootVisualElement.Q<Button>("continuar");

        if (continuar == null)
        {
            Debug.LogError("No se encontró el botón 'continuar'.");
            return;
        }

        InvokeRepeating(nameof(CambiarEscala), 0.2f, 0.5f);
    }

    private void CambiarEscala()
    {
        grande = !grande;

        if (grande)
            continuar.style.scale = new Scale(new Vector3(1.2f, 1.2f, 1f));
        else
            continuar.style.scale = new Scale(new Vector3(1f, 1f, 1f));
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}