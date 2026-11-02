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

    static void CrearSiHayMenu()
    {
        FlyingFree.UI.FlyingFreeMenu menu = Object.FindFirstObjectByType<FlyingFree.UI.FlyingFreeMenu>();
        if (menu == null || YaExiste(menu.transform))
            return;

        Transform jugar = Buscar(menu.transform, "JUGAR");
        Transform trailer = Buscar(menu.transform, "TRÁILER");
        Transform config = Buscar(menu.transform, "CONFIGURACIÓN");
        Transform salir = Buscar(menu.transform, "SALIR");
        if (jugar == null || config == null)
            return;

        GameObject copia = Object.Instantiate(config.gameObject, config.parent);
        copia.name = NombreBoton;
        copia.transform.SetSiblingIndex(jugar.GetSiblingIndex() + 1);

        Button boton = copia.GetComponent<Button>();
        if (boton != null)
        {
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(AbrirEscena);
        }

        Text etiqueta = copia.GetComponentInChildren<Text>();
        if (etiqueta != null)
            etiqueta.text = NombreBoton;

        FlyingFree.UI.MenuIconGraphic icono = copia.GetComponentInChildren<FlyingFree.UI.MenuIconGraphic>();
        if (icono != null)
        {
            icono.symbol = FlyingFree.UI.MenuIconGraphic.Symbol.Play;
            icono.color = new Color(0.09019608f, 0.24313726f, 0.33333334f, 1f);
        }

        Colocar(jugar, 330f, 800f, 160f);
        Colocar(copia.transform, 161f, 760f, 150f);
        if (trailer != null)
            Colocar(trailer, -3f, 760f, 150f);
        Colocar(config, -167f, 760f, 150f);
        if (salir != null)
            Colocar(salir, -331f, 760f, 150f);
    }

    static void AbrirEscena()
    {
        SceneManager.LoadScene(Escena);
    }

    static bool YaExiste(Transform raiz)
    {
        return Buscar(raiz, NombreBoton) != null;
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
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(ancho, alto);
    }
}
