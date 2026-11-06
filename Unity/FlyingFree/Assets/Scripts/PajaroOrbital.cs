using UnityEngine;

/// <summary>Patrulla; cede todo el movimiento durante el robo.</summary>
[DefaultExecutionOrder(50)]
public class PajaroOrbital : MonoBehaviour
{
    Camera camara;
    ControlBandada gestor;
    PajaroInteraccion interaccion;
    Vector3 centro;
    Vector2 radios;
    float velocidad, oscilacion, angulo, recorrido, vida;
    float variacion = .2f, frecuenciaVelocidad = .3f, frecuenciaAltura = .35f, semilla;
    int vueltas, sentido;
    bool iniciado;
    public void ConfigurarVariaciones(float v, float f, float h)
    { variacion = Mathf.Clamp(v, 0, .5f); frecuenciaVelocidad = Mathf.Max(.01f, f); frecuenciaAltura = Mathf.Max(.01f, h); }
    public void Iniciar(Camera cam, Vector3 centroLocal, float radioVuelo, float velocidadVuelo,
        float alturaVuelo, float oscilacionVuelo, float anguloInicial, int numeroVueltas, int direccion, ControlBandada gestor = null)
    {
        camara = cam; this.gestor = gestor; centro = centroLocal + Vector3.up * alturaVuelo;
        radios = gestor != null ? gestor.GetOrbitRadii() : Vector2.one * Mathf.Max(.2f, radioVuelo);
        velocidad = Mathf.Max(.05f, velocidadVuelo); oscilacion = oscilacionVuelo;
        angulo = anguloInicial * Mathf.Deg2Rad; vueltas = Mathf.Max(1, numeroVueltas); sentido = direccion < 0 ? -1 : 1;
        recorrido = vida = 0; semilla = Random.Range(0, 1000f); iniciado = true;
        interaccion = GetComponent<PajaroInteraccion>(); AplicarPose(true);
    }
    void LateUpdate()
    {
        if (!iniciado || (gestor != null && !gestor.PuedeSimular) || (interaccion != null && interaccion.OcupaMovimiento)) return;
        float dt = Time.deltaTime; vida += dt;
        float speed = velocidad * (1 + (Mathf.PerlinNoise(semilla, vida * frecuenciaVelocidad) * 2 - 1) * variacion);
        float derivative = new Vector2(radios.x * Mathf.Sin(angulo), radios.y * Mathf.Cos(angulo)).magnitude;
        float advance = speed / Mathf.Max(.1f, derivative) * dt;
        angulo += sentido * advance; recorrido += advance; AplicarPose(false);
        if ((recorrido >= vueltas * Mathf.PI * 2 || vida > 45) && OcultoDetras(camara, transform.position)) Destroy(gameObject);
        if (vida > 60) Destroy(gameObject);
    }
    void AplicarPose(bool instant)
    {
        float noise = (Mathf.PerlinNoise(semilla + 15, vida * frecuenciaAltura) * 2 - 1) * oscilacion;
        transform.localPosition = centro + new Vector3(Mathf.Cos(angulo) * radios.x, noise, Mathf.Sin(angulo) * radios.y);
        Vector3 tangent = new Vector3(-Mathf.Sin(angulo) * radios.x * sentido, 0, Mathf.Cos(angulo) * radios.y * sentido);
        if (transform.parent != null) tangent = transform.parent.TransformDirection(tangent);
        Quaternion target = Quaternion.LookRotation(tangent, Vector3.up);
        transform.rotation = instant ? target : Quaternion.Slerp(transform.rotation, target, 1 - Mathf.Exp(-8 * Time.deltaTime));
    }
    public static bool OcultoDetras(Camera camara, Vector3 posicion)
    {
        return camara != null && Vector3.Dot(camara.transform.forward, posicion - camara.transform.position) < -.5f;
    }
}
