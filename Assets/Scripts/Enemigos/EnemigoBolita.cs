using UnityEngine;

public class EnemigoBolita : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadHorizontal = 3f;
    [SerializeField] private float alturaRebote = 1f;
    [SerializeField] private float frecuenciaRebote = 5f;

    [Header("Limites")]
    [SerializeField] private Transform limiteIzquierdo;
    [SerializeField] private Transform limiteDerecho;

    [Header("Rotacion visual")]
    [SerializeField] private float velocidadRotacion = 360f;

    private int direccion = 1;
    private float posicionYBase;
    private float tiempo;

    private void Start()
    {
        posicionYBase = transform.position.y;
    }

    private void Update()
    {
        tiempo += Time.deltaTime;

        MoverHorizontalmente();
        MoverVerticalmente();
        RevisarLimites();
        RotarPelota();
    }

    private void MoverHorizontalmente()
    {
        float nuevaX = transform.position.x + direccion * velocidadHorizontal * Time.deltaTime;

        transform.position = new Vector3(
            nuevaX,
            transform.position.y,
            transform.position.z
        );
    }

    private void MoverVerticalmente()
    {
        float rebote = Mathf.Abs(Mathf.Sin(tiempo * frecuenciaRebote)) * alturaRebote;

        transform.position = new Vector3(
            transform.position.x,
            posicionYBase + rebote,
            transform.position.z
        );
    }

    private void RevisarLimites()
    {
        if (transform.position.x >= limiteDerecho.position.x)
        {
            direccion = -1;
        }

        if (transform.position.x <= limiteIzquierdo.position.x)
        {
            direccion = 1;
        }
    }

    private void RotarPelota()
    {
        transform.Rotate(0f, 0f, -direccion * velocidadRotacion * Time.deltaTime);
    }
}