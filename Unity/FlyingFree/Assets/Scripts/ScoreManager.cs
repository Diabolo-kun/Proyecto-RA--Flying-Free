using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Administrador de puntuación para el juego (Globos explotados).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Puntuación")]
    [SerializeField] private int currentScore = 0;

    [Header("Interfaz UI")]
    [Tooltip("Texto uGUI donde se mostrará el marcador (opcional)")]
    [SerializeField] private Text scoreText;
    [SerializeField] private string textPrefix = "Globos: ";

    public static event Action<int> OnScoreUpdated;

    public int CurrentScore => currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        UpdateUI();
    }

    /// <summary>
    /// Suma puntos al marcador y actualiza el texto de la pantalla.
    /// </summary>
    public void AddScore(int points = 1)
    {
        currentScore += points;
        UpdateUI();
        OnScoreUpdated?.Invoke(currentScore);
        Debug.Log($"[ScoreManager] ¡Globo explotado! Puntuación actual: {currentScore}");
    }

    /// <summary>
    /// Reinicia el marcador a cero.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateUI();
        OnScoreUpdated?.Invoke(currentScore);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{textPrefix}{currentScore}";
        }
    }
}
