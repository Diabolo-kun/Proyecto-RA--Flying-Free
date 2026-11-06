using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Marcador único de la escena; las colisiones y la partida controlan cuándo se aceptan puntos.</summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    [SerializeField] int currentScore;
    [SerializeField] Text scoreText;
    [SerializeField] string textPrefix = "Globos: ";
    public static event Action<int> OnScoreUpdated;
    public int CurrentScore => currentScore;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; OnScoreUpdated = null; }
    void Awake()
    {
        // Nunca destruir el GameObject: puede contener también al gestor de partida.
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this; currentScore = Mathf.Max(0, currentScore); UpdateUI();
    }
    public void AddScore(int points = 1)
    {
        var game = GameManagerRA.Instance;
        if (points <= 0 || (game != null && (!game.PartidaEnCurso || game.EsperandoSeguimiento))) return;
        currentScore = (int)Math.Min(int.MaxValue, (long)currentScore + points);
        UpdateUI(); OnScoreUpdated?.Invoke(currentScore);
    }
    public void ResetScore()
    { currentScore = 0; UpdateUI(); OnScoreUpdated?.Invoke(currentScore); }
    void UpdateUI() { if (scoreText != null) scoreText.text = textPrefix + currentScore; }
    void OnDestroy() { if (Instance == this) Instance = null; }
}
