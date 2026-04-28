using UnityEngine;

public class CameraFollowEdgeLimits : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] private Transform jugador;

    [Header("Bordes de cámara")]
    [SerializeField] private EdgeCollider2D bordeSuperior;
    [SerializeField] private EdgeCollider2D bordeInferior;
    [SerializeField] private EdgeCollider2D bordeIzquierdo;
    [SerializeField] private EdgeCollider2D bordeDerecho;

    [Header("Movimiento")]
    [SerializeField] private float suavizado = 5f;

    private Camera camara;
    private Vector3 offset;
    private float zInicial;

    private void Start()
    {
        camara = GetComponent<Camera>();

        offset = transform.position - jugador.position;
        zInicial = transform.position.z;
    }

    private void LateUpdate()
    {
        float mitadAlto = camara.orthographicSize;
        float mitadAncho = mitadAlto * camara.aspect;

        float limiteIzquierdo = bordeIzquierdo.bounds.center.x;
        float limiteDerecho = bordeDerecho.bounds.center.x;

        float minX = limiteIzquierdo + mitadAncho;
        float maxX = limiteDerecho - mitadAncho;

        float objetivoX = jugador.position.x + offset.x;
        float objetivoY = jugador.position.y + offset.y;

        float xLimitada = ClampSeguro(objetivoX, minX, maxX);

        float limiteArriba = ObtenerYDelBorde(bordeSuperior, xLimitada, true);
        float limiteAbajo = ObtenerYDelBorde(bordeInferior, xLimitada, false);

        float minY = limiteAbajo + mitadAlto;
        float maxY = limiteArriba - mitadAlto;

        float yLimitada = ClampSeguro(objetivoY, minY, maxY);

        Vector3 posicionObjetivo = new Vector3(
            xLimitada,
            yLimitada,
            zInicial
        );

        transform.position = Vector3.Lerp(
            transform.position,
            posicionObjetivo,
            suavizado * Time.deltaTime
        );
    }

    private float ObtenerYDelBorde(EdgeCollider2D borde, float x, bool esBordeSuperior)
    {
        Vector2[] puntos = borde.points;

        for (int i = 0; i < puntos.Length - 1; i++)
        {
            Vector2 puntoA = borde.transform.TransformPoint(puntos[i]);
            Vector2 puntoB = borde.transform.TransformPoint(puntos[i + 1]);

            float minXSegmento = Mathf.Min(puntoA.x, puntoB.x);
            float maxXSegmento = Mathf.Max(puntoA.x, puntoB.x);

            if (x >= minXSegmento && x <= maxXSegmento)
            {
                if (Mathf.Approximately(puntoA.x, puntoB.x))
                {
                    if (esBordeSuperior)
                    {
                        return Mathf.Max(puntoA.y, puntoB.y);
                    }
                    else
                    {
                        return Mathf.Min(puntoA.y, puntoB.y);
                    }
                }

                float t = Mathf.InverseLerp(puntoA.x, puntoB.x, x);
                return Mathf.Lerp(puntoA.y, puntoB.y, t);
            }
        }

        Vector2 primerPunto = borde.transform.TransformPoint(puntos[0]);
        Vector2 ultimoPunto = borde.transform.TransformPoint(puntos[puntos.Length - 1]);

        if (Mathf.Abs(x - primerPunto.x) < Mathf.Abs(x - ultimoPunto.x))
        {
            return primerPunto.y;
        }
        else
        {
            return ultimoPunto.y;
        }
    }

    private float ClampSeguro(float valor, float minimo, float maximo)
    {
        if (minimo > maximo)
        {
            return (minimo + maximo) / 2f;
        }

        return Mathf.Clamp(valor, minimo, maximo);
    }
}