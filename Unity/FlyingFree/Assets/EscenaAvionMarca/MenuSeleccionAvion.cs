using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Añade el botón SELECCIONAR AVIÓN debajo de JUGAR, con el mismo estilo del menú.
/// </summary>
public static class MenuSeleccionAvion
{
    const string NombreBoton = "SELECCIONAR AVIÓN";
    const string Escena = "EscenaAvionMarca";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AlCargarEscena()
    {
        SceneManager.sceneLoaded -= AlEntrarEnEscena;
        SceneManager.sceneLoaded += AlEntrarEnEscena;
        CrearSiHayMenu();
    }

    static void AlEntrarEnEscena(Scene escena, LoadSceneMode modo)
    {
        CrearSiHayMenu();
    }

    public static void Asegurar()
    {
        CrearSiHayMenu();
    }

    static void CrearSiHayMenu()
    {
        FlyingFree.UI.FlyingFreeMenu menu = Object.FindFirstObjectByType<FlyingFree.UI.FlyingFreeMenu>();
        if (menu == null)
            return;

        Transform jugar = Buscar(menu.transform, "JUGAR");
        Transform trailer = Buscar(menu.transform, "TRÁILER");
        Transform config = Buscar(menu.transform, "CONFIGURACIÓN");
        Transform salir = Buscar(menu.transform, "SALIR");
        if (jugar == null || config == null)
            return;

        Transform existente = Buscar(menu.transform, NombreBoton);
        if (existente != null)
        {
            Conectar(existente.GetComponent<Button>());
            return;
        }

        GameObject copia = Object.Instantiate(config.gameObject, config.parent);
        copia.name = NombreBoton;
        copia.transform.SetSiblingIndex(jugar.GetSiblingIndex() + 1);

        Conectar(copia.GetComponent<Button>());

        Text etiqueta = copia.GetComponentInChildren<Text>();
        if (etiqueta != null)
            etiqueta.text = NombreBoton;

        FlyingFree.UI.MenuIconGraphic icono = copia.GetComponentInChildren<FlyingFree.UI.MenuIconGraphic>();
        if (icono != null)
        {
            icono.symbol = FlyingFree.UI.MenuIconGraphic.Symbol.Play;
            icono.color = new Color(0.09019608f, 0.24313726f, 0.33333334f, 1f);
        }

        // El layout final lo ajusta MenuInstrucciones al meter su botón.
        Colocar(jugar, 370f, 780f, 145f);
        Colocar(copia.transform, 220f, 740f, 135f);
        if (trailer != null)
            Colocar(trailer, -70f, 740f, 135f);
        Colocar(config, -215f, 740f, 135f);
        if (salir != null)
            Colocar(salir, -360f, 740f, 135f);
    }

    static void Conectar(Button boton)
    {
        if (boton == null)
            return;

        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(AbrirEscena);
    }

    static void AbrirEscena()
    {
        SceneManager.LoadScene(Escena);
    }

    static Transform Buscar(Transform raiz, string nombre)
    {
        Transform[] todos = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == nombre)
                return todos[i];
        }

        return null;
    }

    static void Colocar(Transform objetivo, float y, float ancho, float alto)
    {
        RectTransform rect = objetivo as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        Vector2 posicion = new Vector2(0f, y);
        Vector2 tamano = new Vector2(ancho, alto);
        if (rect.anchoredPosition != posicion)
            rect.anchoredPosition = posicion;
        if (rect.sizeDelta != tamano)
            rect.sizeDelta = tamano;
    }
}
