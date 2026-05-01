using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlCaminito : MonoBehaviour
{
    [Header("Padre que contiene los pasos")]
    public Transform contenedorPasos;

    [Header("Rango de pasos")]
    public int pasoInicial = 18;
    public int pasoFinal = 44;

    [Header("Botón final")]
    public GameObject botonFinal;

    [Header("Tiempos")]
    public float tiempoEntrePasos = 0.2f;
    public float esperaAntesBoton = 0.5f;

    [Header("Animación botón")]
    public float duracionPop = 0.2f;
    public float escalaExtra = 1.2f;

    private List<GameObject> pasos = new List<GameObject>();

    void Start()
    {
        pasos.Clear();

        for (int i = pasoInicial; i <= pasoFinal; i++)
        {
            string nombrePaso = "Paso_" + i.ToString("00");
            Transform pasoEncontrado = contenedorPasos.Find(nombrePaso);

            if (pasoEncontrado != null)
            {
                pasos.Add(pasoEncontrado.gameObject);
                pasoEncontrado.gameObject.SetActive(false);
            }
        }

        botonFinal.SetActive(false);
        botonFinal.transform.localScale = Vector3.zero;

        StartCoroutine(DibujarCamino());
    }

    IEnumerator DibujarCamino()
    {
        for (int i = 0; i < pasos.Count; i++)
        {
            pasos[i].SetActive(true);
            yield return new WaitForSeconds(tiempoEntrePasos);
        }

        yield return new WaitForSeconds(esperaAntesBoton);

        botonFinal.SetActive(true);
        yield return StartCoroutine(AnimacionPopBoton());
    }

    IEnumerator AnimacionPopBoton()
    {
        float tiempo = 0f;

        Vector3 escalaFinal = Vector3.one;
        Vector3 escalaGrande = Vector3.one * escalaExtra;

        while (tiempo < duracionPop)
        {
            float t = tiempo / duracionPop;
            botonFinal.transform.localScale = Vector3.Lerp(Vector3.zero, escalaGrande, t);
            tiempo += Time.deltaTime;
            yield return null;
        }

        tiempo = 0f;

        while (tiempo < duracionPop)
        {
            float t = tiempo / duracionPop;
            botonFinal.transform.localScale = Vector3.Lerp(escalaGrande, escalaFinal, t);
            tiempo += Time.deltaTime;
            yield return null;
        }

        botonFinal.transform.localScale = escalaFinal;

        HoverBotonFinal hover = botonFinal.GetComponent<HoverBotonFinal>();
        if (hover != null)
        {
            hover.ActualizarEscalaOriginal();
        }
    }
}