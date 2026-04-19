using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class BotonOpciones : MonoBehaviour
{

    private Button m_button = null;
    private Image m_image = null;
    private Text m_text = null;
    public Opcion Opcion {get; set;}

    private void Awake()
    {
        m_button = GetComponent<Button>();
        m_image = GetComponent<Image>();
        m_text = transform.GetChild(0).GetComponent<Text>();
    }


    public void Construct(Opcion option, Action<BotonOpciones> callback)
    {
        m_text.text = Opcion.text;

        m_button.enabled = true;

        Opcion = option;

        m_button.onClick.AddListener(delegate
        {
            callback(this);
        });
    }
}
