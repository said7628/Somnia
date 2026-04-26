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

    [Header("Daño")]
    [SerializeField] private int dano = 1;

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

        MoverBolita();
        RevisarLimites();
        RotarPelota();
    }

    private void MoverBolita()
    {
        float nuevaX = transform.position.x + direccion * velocidadHorizontal * Time.deltaTime;

        float rebote = Mathf.Abs(Mathf.Sin(tiempo * frecuenciaRebote)) * alturaRebote;
        float nuevaY = posicionYBase + rebote;

        transform.position = new Vector3(
            nuevaX,
            nuevaY,
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

    private void OnTriggerEnter2D(Collider2D otro)
    {
        RevisarDanoJugador(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        RevisarDanoJugador(otro);
    }

    private void RevisarDanoJugador(Collider2D otro)
    {
        if (otro.CompareTag("Jugador"))
        {
            VidaPersonaje vidaPersonaje = otro.GetComponentInParent<VidaPersonaje>();

            if (vidaPersonaje == null)
            {
                vidaPersonaje = otro.GetComponentInChildren<VidaPersonaje>();
            }

            if (vidaPersonaje != null)
            {
                vidaPersonaje.RecibirDano(dano);
                Debug.Log("La bolita hizo daño al jugador");
            }
        }
    }
}