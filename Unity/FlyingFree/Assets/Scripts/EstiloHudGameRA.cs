using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FlyingFree.UI;

/// <summary>
/// Aplica el estilo del menú al HUD y a las pantallas de victoria/derrota de GameRA.
/// En partida solo deja visibles globos restantes, tiempo y el botón de pausa.
/// </summary>
public sealed class EstiloHudGameRA : MonoBehaviour
{
    void Start()
    {
        LimpiarHudExtra();
        RestylarMarcador();
        RestylarResultado("Victoria");
        RestylarResultado("Derrota");
    }

    void LimpiarHudExtra()
    {
        // El avión se lanza por movimiento y vuela solo: sin joystick ni dirección táctil.
        foreach (var touch in Object.FindObjectsByType<TouchFlightController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            touch.enabled = false;

        OcultarSiExiste("JoystickBase");
        var hudViejo = Buscar("Hud");
        if (hudViejo != null)
        {
            var canvas = hudViejo.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.gameObject.name == "Canvas")
                canvas.gameObject.SetActive(false);
            else
                hudViejo.gameObject.SetActive(false);
        }
    }

    void RestylarMarcador()
    {
        var marcador = Buscar("Marcador");
        if (marcador == null) return;

        var panel = marcador.GetComponent<Image>();
        if (panel != null)
        {
            panel.sprite = EstiloMenuUI.PanelRedondo;
            panel.type = panel.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            panel.color = new Color(EstiloMenuUI.Crema.r, EstiloMenuUI.Crema.g, EstiloMenuUI.Crema.b, 0.94f);
        }

        var rtMarcador = marcador as RectTransform;
        if (rtMarcador != null)
        {
            rtMarcador.anchorMin = new Vector2(0f, 1f);
            rtMarcador.anchorMax = new Vector2(1f, 1f);
            rtMarcador.pivot = new Vector2(0.5f, 1f);
            rtMarcador.anchoredPosition = new Vector2(0f, -175f);
            rtMarcador.sizeDelta = new Vector2(-80f, 120f);
        }

        foreach (Transform hijo in marcador)
        {
            bool visible = hijo.name == "Globos" || hijo.name == "Tiempo";
            hijo.gameObject.SetActive(visible);
        }

        var globos = BuscarHijo(marcador, "Globos") as RectTransform;
        if (globos != null)
        {
            globos.anchorMin = new Vector2(0.05f, 0.12f);
            globos.anchorMax = new Vector2(0.48f, 0.88f);
            globos.offsetMin = globos.offsetMax = Vector2.zero;
            globos.anchoredPosition = Vector2.zero;
            globos.sizeDelta = Vector2.zero;
        }

        var tiempo = BuscarHijo(marcador, "Tiempo") as RectTransform;
        if (tiempo != null)
        {
            tiempo.anchorMin = new Vector2(0.52f, 0.12f);
            tiempo.anchorMax = new Vector2(0.95f, 0.88f);
            tiempo.offsetMin = tiempo.offsetMax = Vector2.zero;
            tiempo.anchoredPosition = Vector2.zero;
            tiempo.sizeDelta = Vector2.zero;
        }

        foreach (var tmp in marcador.GetComponentsInChildren<TMP_Text>(true))
        {
            EstiloMenuUI.PintarTextoTmp(tmp, EstiloMenuUI.Tinta);
            tmp.fontSize = 48f;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }

    void RestylarResultado(string nombreRaiz)
    {
        var raiz = Buscar(nombreRaiz);
        if (raiz == null) return;

        var fondo = raiz.GetComponent<Image>();
        if (fondo != null)
        {
            fondo.sprite = null;
            fondo.type = Image.Type.Simple;
            fondo.color = EstiloMenuUI.Overlay;
        }

        var tarjeta = BuscarHijo(raiz, "Tarjeta");
        if (tarjeta != null)
        {
            var img = tarjeta.GetComponent<Image>();
            if (img != null)
                EstiloMenuUI.AplicarPanelCrema(img, 1f);

            var tarjetaRt = tarjeta as RectTransform;
            if (tarjetaRt != null)
                tarjetaRt.sizeDelta = new Vector2(860f, 920f);

            if (BuscarHijo(tarjeta, "Cabecera") == null)
            {
                var cabecera = EstiloMenuUI.CrearPanel(tarjeta, "Cabecera", EstiloMenuUI.Azul, false);
                var rt = cabecera.rectTransform;
                rt.anchorMin = new Vector2(0.06f, 0.82f);
                rt.anchorMax = new Vector2(0.94f, 0.95f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.SetAsFirstSibling();
            }
        }

        foreach (var tmp in raiz.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.name == "Titulo")
                EstiloMenuUI.PintarTextoTmp(tmp, Color.white);
            else
                EstiloMenuUI.PintarTextoTmp(tmp, EstiloMenuUI.Tinta);
        }

        var titulo = BuscarHijo(tarjeta, "Titulo");
        var cab = BuscarHijo(tarjeta, "Cabecera");
        if (titulo != null && cab != null)
        {
            titulo.SetSiblingIndex(cab.GetSiblingIndex() + 1);
            var tituloRt = titulo as RectTransform;
            if (tituloRt != null)
            {
                tituloRt.anchorMin = new Vector2(0.08f, 0.82f);
                tituloRt.anchorMax = new Vector2(0.92f, 0.95f);
                tituloRt.anchoredPosition = Vector2.zero;
                tituloRt.sizeDelta = Vector2.zero;
                tituloRt.offsetMin = Vector2.zero;
                tituloRt.offsetMax = Vector2.zero;
            }
        }

        var resumen = BuscarHijo(tarjeta, "Resumen") as RectTransform;
        if (resumen != null)
        {
            resumen.anchorMin = new Vector2(0.08f, 0.48f);
            resumen.anchorMax = new Vector2(0.92f, 0.78f);
            resumen.anchoredPosition = Vector2.zero;
            resumen.sizeDelta = Vector2.zero;
            resumen.offsetMin = Vector2.zero;
            resumen.offsetMax = Vector2.zero;
        }

        foreach (var boton in raiz.GetComponentsInChildren<Button>(true))
        {
            bool primario = boton.name.IndexOf("Repetir", System.StringComparison.OrdinalIgnoreCase) >= 0
                || boton.name.IndexOf("Reiniciar", System.StringComparison.OrdinalIgnoreCase) >= 0
                || boton.name.IndexOf("Jugar", System.StringComparison.OrdinalIgnoreCase) >= 0
                || EsBotonPrimarioVictoriaDerrota(boton);
            var icono = primario ? MenuIconGraphic.Symbol.Play : MenuIconGraphic.Symbol.Exit;
            if (boton.name.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) >= 0)
                icono = MenuIconGraphic.Symbol.Exit;
            EstiloMenuUI.AplicarBotonExistente(boton, primario, icono);

            var rt = boton.transform as RectTransform;
            if (rt == null) continue;

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 120f);
            if (boton.name.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) >= 0)
                rt.anchoredPosition = new Vector2(0f, -320f);
            else
                rt.anchoredPosition = new Vector2(0f, -170f);

            foreach (var tmp in boton.GetComponentsInChildren<TMP_Text>(true))
            {
                var tRt = tmp.rectTransform;
                tRt.anchorMin = new Vector2(0.22f, 0.1f);
                tRt.anchorMax = new Vector2(0.96f, 0.9f);
                tRt.offsetMin = tRt.offsetMax = Vector2.zero;
            }
        }
    }

    static bool EsBotonPrimarioVictoriaDerrota(Button boton)
    {
        var tmp = boton.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null) return false;
        string t = tmp.text ?? string.Empty;
        return t.IndexOf("JUGAR", System.StringComparison.OrdinalIgnoreCase) >= 0
            || t.IndexOf("REINICIAR", System.StringComparison.OrdinalIgnoreCase) >= 0
            || t.IndexOf("OTRA", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void OcultarSiExiste(string nombre)
    {
        var t = Buscar(nombre);
        if (t != null) t.gameObject.SetActive(false);
    }

    static Transform Buscar(string nombre)
    {
        var todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < todos.Length; i++)
            if (todos[i].name == nombre)
                return todos[i];
        return null;
    }

    static Transform BuscarHijo(Transform padre, string nombre)
    {
        if (padre == null) return null;
        for (int i = 0; i < padre.childCount; i++)
        {
            var hijo = padre.GetChild(i);
            if (hijo.name == nombre) return hijo;
        }
        return null;
    }
}
