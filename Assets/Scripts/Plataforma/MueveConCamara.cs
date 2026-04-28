using UnityEngine;

public class CameraFollowDynamicLimits : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] private Transform jugador;

    [Header("Límites iniciales")]
    [SerializeField] private BoxCollider2D limitesIniciales;

    [Header("Movimiento")]
    [SerializeField] private float suavizadoCamara = 5f;
    [SerializeField] private float suavizadoLimites = 5f;

    private Camera camara;
    private Vector3 offset;
    private float zInicial;

    private float minXActual;
    private float maxXActual;
    private float minYActual;
    private float maxYActual;

    private float minXObjetivo;
    private float maxXObjetivo;
    private float minYObjetivo;
    private float maxYObjetivo;

    private void Start()
    {
        camara = GetComponent<Camera>();

        offset = transform.position - jugador.position;
        zInicial = transform.position.z;

        CambiarLimites(limitesIniciales);
        AplicarLimitesInstantaneo();
    }

    private void LateUpdate()
    {
        SuavizarLimites();

        float mitadAlto = camara.orthographicSize;
        float mitadAncho = mitadAlto * camara.aspect;

        float minXCamara = minXActual + mitadAncho;
        float maxXCamara = maxXActual - mitadAncho;
        float minYCamara = minYActual + mitadAlto;
        float maxYCamara = maxYActual - mitadAlto;

        Vector3 objetivo = new Vector3(
            jugador.position.x + offset.x,
            jugador.position.y + offset.y,
            zInicial
        );

        Vector3 posicionSuave = Vector3.Lerp(
            transform.position,
            objetivo,
            suavizadoCamara * Time.deltaTime
        );

        float xFinal = Mathf.Clamp(posicionSuave.x, minXCamara, maxXCamara);
        float yFinal = Mathf.Clamp(posicionSuave.y, minYCamara, maxYCamara);

        transform.position = new Vector3(xFinal, yFinal, zInicial);
    }

    public void CambiarLimites(BoxCollider2D nuevosLimites)
    {
        Bounds bounds = nuevosLimites.bounds;

        minXObjetivo = bounds.min.x;
        maxXObjetivo = bounds.max.x;
        minYObjetivo = bounds.min.y;
        maxYObjetivo = bounds.max.y;
    }

    private void SuavizarLimites()
    {
        minXActual = Mathf.Lerp(minXActual, minXObjetivo, suavizadoLimites * Time.deltaTime);
        maxXActual = Mathf.Lerp(maxXActual, maxXObjetivo, suavizadoLimites * Time.deltaTime);
        minYActual = Mathf.Lerp(minYActual, minYObjetivo, suavizadoLimites * Time.deltaTime);
        maxYActual = Mathf.Lerp(maxYActual, maxYObjetivo, suavizadoLimites * Time.deltaTime);
    }

    private void AplicarLimitesInstantaneo()
    {
        minXActual = minXObjetivo;
        maxXActual = maxXObjetivo;
        minYActual = minYObjetivo;
        maxYActual = maxYObjetivo;
    }
}