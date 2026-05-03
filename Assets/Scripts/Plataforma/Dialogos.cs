using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class SistemaDialogoCanvas : MonoBehaviour
{
    public enum Hablante
    {
        NPC,
        Jugador
    }

    [System.Serializable]
    public class LineaDialogo
    {
        [Header("Quién habla")]
        public Hablante hablante;

        [Header("Texto")]
        [TextArea(2, 5)]
        public string texto;

        [Header("Pregunta")]
        public bool esPregunta;

        [Tooltip("Escribe aquí la palabra correcta que reemplazará ______")]
        public string respuestaCorrecta;

        [Tooltip("0 = Opcion1, 1 = Opcion2, 2 = Opcion3")]
        public int indiceCorrecto;

        public string opcion1;
        public string opcion2;
        public string opcion3;
    }

    [Header("Activación por cercanía")]
    [SerializeField] private GameObject canvasDialogo;
    [SerializeField] private string tagJugador = "Player";

    [Header("Objetos principales")]
    [SerializeField] private RectTransform jugador;
    [SerializeField] private RectTransform opaco;
    [SerializeField] private RectTransform npc;

    [Header("UI diálogo")]
    [SerializeField] private TMP_Text dialogoText;
    [SerializeField] private GameObject opciones;
    [SerializeField] private Button siguiente;

    [Header("Botones de opciones")]
    [SerializeField] private Button opcion1;
    [SerializeField] private Button opcion2;
    [SerializeField] private Button opcion3;

    [Header("Texto de opciones")]
    [SerializeField] private TMP_Text opcionText1;
    [SerializeField] private TMP_Text opcionText2;
    [SerializeField] private TMP_Text opcionText3;

    [Header("Audio")]
    [SerializeField] private AudioSource audioEscritura;
    [SerializeField] private AudioSource audioError;

    [Header("Escritura")]
    [SerializeField] private float velocidadEscritura = 0.03f;
    [SerializeField] private float pausaDespuesRespuesta = 0.7f;

    [Header("Diálogo")]
    [SerializeField] private LineaDialogo[] lineas;

    [Header("Escena al terminar")]
    [SerializeField] private string escenaAlTerminar;

    private int indiceActual = 0;
    private Coroutine rutinaEscritura;
    private bool escribiendo = false;
    private bool esperandoRespuesta = false;
    private bool dialogoActivo = false;
    private bool autoActivacionHabilitada = true;

    private static readonly Dictionary<string, int> PlataformaSceneToLevelId = new Dictionary<string, int>
    {
        { "Plataforma1", 7 },
        { "Plataforma2", 8 },
        { "Plataforma3", 9 },
        { "Plataforma4", 10 },
        { "Plataforma5", 11 }
    };

    public event Action OnDialogFinished;
    public bool IsDialogActive => dialogoActivo;

    private void Start()
    {
        canvasDialogo.SetActive(false);

        opciones.SetActive(false);
        siguiente.gameObject.SetActive(true);
        dialogoText.text = "";

        siguiente.onClick.RemoveAllListeners();
        siguiente.onClick.AddListener(AlPresionarSiguiente);

        opcion1.onClick.RemoveAllListeners();
        opcion2.onClick.RemoveAllListeners();
        opcion3.onClick.RemoveAllListeners();

        opcion1.onClick.AddListener(() => ElegirOpcion(0));
        opcion2.onClick.AddListener(() => ElegirOpcion(1));
        opcion3.onClick.AddListener(() => ElegirOpcion(2));

        if (audioEscritura != null)
        {
            audioEscritura.loop = true;
            audioEscritura.playOnAwake = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!autoActivacionHabilitada)
        {
            return;
        }

        if (collision.CompareTag(tagJugador) && !dialogoActivo)
        {
            ActivarDialogo();
        }
    }

    public void ActivarDialogo()
    {
        if (dialogoActivo)
        {
            Debug.LogWarning("[SistemaDialogoCanvas][WARN] Dialog start ignored because already active");
            return;
        }

        dialogoActivo = true;
        indiceActual = 0;

        canvasDialogo.SetActive(true);
        PausarJuego();

        opciones.SetActive(false);
        siguiente.gameObject.SetActive(true);
        dialogoText.text = "";

        MostrarLineaActual();
    }

    private void PausarJuego()
    {
        Time.timeScale = 0f;
    }

    private void ReanudarJuego()
    {
        Time.timeScale = 1f;
    }

    private void MostrarLineaActual()
    {
        if (indiceActual >= lineas.Length)
        {
            TerminarDialogo();
            return;
        }

        LineaDialogo linea = lineas[indiceActual];

        CambiarCapas(linea.hablante);

        opciones.SetActive(false);
        siguiente.gameObject.SetActive(true);
        esperandoRespuesta = false;

        if (rutinaEscritura != null)
        {
            StopCoroutine(rutinaEscritura);
        }

        rutinaEscritura = StartCoroutine(EscribirTexto(linea.texto));
    }

    private IEnumerator EscribirTexto(string textoCompleto)
    {
        escribiendo = true;
        dialogoText.text = "";

        if (audioEscritura != null)
        {
            audioEscritura.Stop();
            audioEscritura.Play();
        }

        for (int i = 0; i < textoCompleto.Length; i++)
        {
            dialogoText.text += textoCompleto[i];

            yield return new WaitForSecondsRealtime(velocidadEscritura);
        }

        if (audioEscritura != null)
        {
            audioEscritura.Stop();
        }

        escribiendo = false;
        rutinaEscritura = null;

        LineaDialogo linea = lineas[indiceActual];

        if (linea.esPregunta)
        {
            MostrarOpciones(linea);
            siguiente.gameObject.SetActive(false);
            esperandoRespuesta = true;
        }
    }

    private void MostrarOpciones(LineaDialogo linea)
    {
        opciones.SetActive(true);

        opcionText1.text = linea.opcion1;
        opcionText2.text = linea.opcion2;
        opcionText3.text = linea.opcion3;
    }

    public void AlPresionarSiguiente()
    {
        if (indiceActual >= lineas.Length)
        {
            return;
        }

        if (esperandoRespuesta)
        {
            return;
        }

        if (escribiendo)
        {
            CompletarTextoInstantaneo();
            return;
        }

        indiceActual++;
        MostrarLineaActual();
    }

    private void CompletarTextoInstantaneo()
    {
        if (rutinaEscritura != null)
        {
            StopCoroutine(rutinaEscritura);
            rutinaEscritura = null;
        }

        if (audioEscritura != null)
        {
            audioEscritura.Stop();
        }

        dialogoText.text = lineas[indiceActual].texto;
        escribiendo = false;

        LineaDialogo linea = lineas[indiceActual];

        if (linea.esPregunta)
        {
            MostrarOpciones(linea);
            siguiente.gameObject.SetActive(false);
            esperandoRespuesta = true;
        }
    }

    private void ElegirOpcion(int indiceElegido)
    {
        if (!esperandoRespuesta)
        {
            return;
        }

        LineaDialogo linea = lineas[indiceActual];

        bool esCorrecta = indiceElegido == linea.indiceCorrecto;

        if (!esCorrecta && audioError != null)
        {
            audioError.Play();
            return;
        }

        string textoRellenado = linea.texto.Replace("______", linea.respuestaCorrecta);
        dialogoText.text = textoRellenado;

        opciones.SetActive(false);
        esperandoRespuesta = false;

        StartCoroutine(AvanzarDespuesDeResponder());
    }

    private IEnumerator AvanzarDespuesDeResponder()
    {
        yield return new WaitForSecondsRealtime(pausaDespuesRespuesta);

        indiceActual++;
        MostrarLineaActual();
    }

    private void CambiarCapas(Hablante hablanteActual)
    {
        if (hablanteActual == Hablante.NPC)
        {
            jugador.SetSiblingIndex(0);
            opaco.SetSiblingIndex(1);
            npc.SetSiblingIndex(2);
        }
        else
        {
            npc.SetSiblingIndex(0);
            opaco.SetSiblingIndex(1);
            jugador.SetSiblingIndex(2);
        }
    }



    public void SetAutoActivationEnabled(bool enabled)
    {
        autoActivacionHabilitada = enabled;
    }

    public void StopDialogCompletely()
    {
        Debug.Log("[SistemaDialogoCanvas] StopDialogCompletely called");

        if (rutinaEscritura != null)
        {
            StopCoroutine(rutinaEscritura);
            rutinaEscritura = null;
        }

        if (audioEscritura != null)
        {
            audioEscritura.Stop();
            Debug.Log("[SistemaDialogoCanvas] Typewriter audio stopped");
        }

        escribiendo = false;
        esperandoRespuesta = false;
        dialogoActivo = false;

        if (dialogoText != null)
        {
            dialogoText.text = "";
        }

        if (opciones != null)
        {
            opciones.SetActive(false);
        }

        if (siguiente != null)
        {
            siguiente.gameObject.SetActive(false);
        }

        if (canvasDialogo != null)
        {
            canvasDialogo.SetActive(false);
        }

        ReanudarJuego();
    }

    private void TerminarDialogo()
    {
        if (audioEscritura != null)
        {
            audioEscritura.Stop();
        }

        dialogoText.text = "";
        opciones.SetActive(false);
        siguiente.gameObject.SetActive(false);

        dialogoActivo = false;

        ReanudarJuego();

        canvasDialogo.SetActive(false);

        OnDialogFinished?.Invoke();

        if (!string.IsNullOrWhiteSpace(escenaAlTerminar))
        {
            TryPreparePlataformaCompletionSession();
            SceneManager.LoadScene(escenaAlTerminar);
        }
    }

    private void TryPreparePlataformaCompletionSession()
    {
        if (!string.Equals(escenaAlTerminar, "ScorePlataforma", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string sourceScene = SceneManager.GetActiveScene().name;
        if (!PlataformaSceneToLevelId.TryGetValue(sourceScene, out int levelId))
        {
            Debug.LogWarning($"[PlataformaComplete][WARN] No levelId mapping for scene={sourceScene}");
            return;
        }

        int slot = ResolveCurrentSlotNumber();
        if (slot <= 0)
        {
            Debug.LogError("[PlataformaComplete][ERROR] Could not resolve current slot");
            return;
        }
        VidaPersonaje vida = FindFirstObjectByType<VidaPersonaje>();
        int vidasActuales = vida != null ? Mathf.Max(0, vida.VidasActuales) : 0;
        int vidasMaximas = vida != null ? Mathf.Max(0, vida.VidasMaximas) : 0;

        PlataformaSession.SlotNumber = slot;
        PlataformaSession.LevelId = levelId;
        PlataformaSession.SourceLevelScene = sourceScene;
        PlataformaSession.ReturnScene = "Isla1";
        PlataformaSession.LivesRemaining = vidasActuales;
        PlataformaSession.MaxLives = vidasMaximas;
        PlataformaSession.Completed = true;
        PlataformaSession.RewardAlreadySaved = false;

        Debug.Log($"[PlataformaComplete] sourceScene={sourceScene}");
        Debug.Log($"[PlataformaComplete] levelId={levelId} slot={slot}");
        Debug.Log($"[PlataformaComplete] livesRemaining={vidasActuales} maxLives={vidasMaximas}");
        Debug.Log("[PlataformaComplete] Loading ScorePlataforma");
    }



    private int ResolveCurrentSlotNumber()
    {
        if (TextTypingSession.SlotNumber > 0)
        {
            return TextTypingSession.SlotNumber;
        }

        return 0;
    }

}