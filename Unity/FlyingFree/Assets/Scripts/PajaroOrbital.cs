
using UnityEngine;

public class PajaroOrbital : MonoBehaviour
{
    private Camera camaraAR;
    private Vector3 centroLocal;

    private float radio;
    private float velocidad;
    private float altura;
    private float oscilacion;
    private float anguloInicial;
    private float gradosRecorridos;

    private int vueltas;
    private int sentido;
    private bool iniciado;

    // NUEVO: variaciones naturales
    private float variacionVelocidad = 0.20f;
    private float frecuenciaVelocidad = 0.30f;
    private float frecuenciaAltura = 0.35f;

    // Cada pajaro tiene un patron diferente
    private float semillaVelocidad;
    private float semillaAltura;

    public void ConfigurarVariaciones(
        float variacion,
        float frecuenciaCambioVelocidad,
        float frecuenciaCambioAltura)
    {
        variacionVelocidad = Mathf.Clamp(
            variacion, 0f, 0.5f);

        frecuenciaVelocidad = Mathf.Max(
            0.01f, frecuenciaCambioVelocidad);

        frecuenciaAltura = Mathf.Max(
            0.01f, frecuenciaCambioAltura);
    }

    public void Iniciar(
        Camera camara,
        Vector3 centro,
        float radioVuelo,
        float velocidadVuelo,
        float alturaVuelo,
        float oscilacionVuelo,
        float angulo,
        int numeroVueltas,
        int direccion)
    {
        camaraAR = camara;
        centroLocal = centro;

        radio = Mathf.Max(0.2f, radioVuelo);
        velocidad = Mathf.Max(0.05f, velocidadVuelo);
        altura = alturaVuelo;
        oscilacion = oscilacionVuelo;

        anguloInicial = angulo;
        vueltas = Mathf.Max(1, numeroVueltas);
        sentido = direccion < 0 ? -1 : 1;

        gradosRecorridos = 0f;

        // Valores aleatorios independientes
        semillaVelocidad = Random.Range(0f, 1000f);
        semillaAltura = Random.Range(1000f, 2000f);

        transform.localPosition =
            CalcularPosicion(anguloInicial);

        iniciado = true;
    }

    private Vector3 CalcularPosicion(float angulo)
    {
        float rad = angulo * Mathf.Deg2Rad;

        // NUEVO: subida y bajada suaves
        float ruidoAltura = Mathf.PerlinNoise(
            semillaAltura,
            Time.time * frecuenciaAltura
        );

        // Convertimos el valor 0..1 a -1..1
        float desplazamientoVertical =
            (ruidoAltura * 2f - 1f) * oscilacion;

        return centroLocal + new Vector3(
            Mathf.Cos(rad) * radio,
            altura + desplazamientoVertical,
            Mathf.Sin(rad) * radio
        );
    }

    private void Update()
    {
        if (!iniciado)
            return;

        // NUEVO: velocidad que cambia suavemente
        float ruidoVelocidad = Mathf.PerlinNoise(
            semillaVelocidad,
            Time.time * frecuenciaVelocidad
        );

        float factorVelocidad = 1f +
            (ruidoVelocidad * 2f - 1f) *
            variacionVelocidad;

        float velocidadActual =
            velocidad * factorVelocidad;

        // Avanzar usando la velocidad variable
        float incremento =
            velocidadActual / radio *
            Mathf.Rad2Deg * Time.deltaTime;

        gradosRecorridos += incremento;

        float angulo =
            anguloInicial +
            sentido * gradosRecorridos;

        // Posicion con altura variable
        transform.localPosition =
            CalcularPosicion(angulo);

        // Orientacion del pajaro
        float rad = angulo * Mathf.Deg2Rad;

        Vector3 tangenteLocal = new Vector3(
            -Mathf.Sin(rad) * sentido,
            0f,
            Mathf.Cos(rad) * sentido
        );

        Vector3 direccionMundo = transform.parent != null
            ? transform.parent.TransformDirection(tangenteLocal)
            : tangenteLocal;

        if (direccionMundo.sqrMagnitude > 0.001f)
        {
            Quaternion objetivo = Quaternion.LookRotation(
                direccionMundo,
                Vector3.up
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                objetivo,
                8f * Time.deltaTime
            );
        }

        // Completar las vueltas asignadas
        if (gradosRecorridos < vueltas * 360f)
            return;

        // Desaparecer SOLAMENTE detras del jugador
        if (OcultoDetras(
                camaraAR, transform.position))
        {
            Destroy(gameObject);
        }
    }

    public static bool OcultoDetras(
        Camera camara, Vector3 posicion)
    {
        if (camara == null)
            return false;

        Vector3 haciaPajaro =
            posicion - camara.transform.position;

        Vector3 direccionHorizontal =
            Vector3.ProjectOnPlane(
                haciaPajaro, Vector3.up);

        Vector3 miradaHorizontal =
            Vector3.ProjectOnPlane(
                camara.transform.forward, Vector3.up);

        if (direccionHorizontal.sqrMagnitude < 0.001f ||
            miradaHorizontal.sqrMagnitude < 0.001f)
            return false;

        float producto = Vector3.Dot(
            direccionHorizontal.normalized,
            miradaHorizontal.normalized
        );

        // Tiene que estar detras
        if (producto > -0.3f)
            return false;

        Vector3 pantalla =
            camara.WorldToViewportPoint(posicion);

        const float margen = 0.12f;

        bool fueraPantalla =
            pantalla.z <= 0f ||
            pantalla.x < -margen ||
            pantalla.x > 1f + margen ||
            pantalla.y < -margen ||
            pantalla.y > 1f + margen;

        return fueraPantalla;
    }
}
