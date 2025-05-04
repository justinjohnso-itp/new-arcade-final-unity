using UnityEngine;
using System.Collections;

/// <summary>
/// Manages game difficulty parameters that ramp up over time.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    private float elapsedTime = 0f;

    [Header("Logarithmic Scaling Settings")]
    [Tooltip("Base forward movement speed at time 0")]
    public float baseForwardSpeed = 4f;
    [Tooltip("Forward speed growth per log(second) increase in elapsed time")]
    public float forwardSpeedGrowthRate = 1f;

    [Tooltip("Base horizontal movement speed at time 0")]
    public float baseHorizontalSpeed = 3f;
    [Tooltip("Horizontal speed growth per log(second) increase in elapsed time")]
    public float horizontalSpeedGrowthRate = 0.5f;

    [Tooltip("Base number of package types available at time 0")]
    public int baseAvailableTypes = 2;
    [Tooltip("Growth in available types per log(second) increase in elapsed time")]
    public float availableTypesGrowthRate = 0.1f;

    [Tooltip("Base package spawn delay at time 0")]
    public float baseRandomAddDelay = 2f;
    [Tooltip("Reduction in spawn delay per log(second) increase in elapsed time")]
    public float randomAddDelayReductionRate = 0.1f;
    [Tooltip("Minimum spawn delay cap")]
    public float minRandomAddDelay = 0.2f;

    [Tooltip("Base obstacles per chunk at time 0")]
    public int baseMaxObstacles = 1;
    [Tooltip("Growth in max obstacles per log(second) increase in elapsed time")]
    public float obstaclesGrowthRate = 0.5f;

    [Tooltip("Base delivery zone activation chance at time 0")]
    [Range(0f,1f)] public float baseDeliveryChance = 0.3f;
    [Tooltip("Increase in delivery chance per log(second) increase in elapsed time")]
    public float deliveryChanceGrowthRate = 0.01f;

    [Tooltip("Base max zones per group at time 0")]
    public int baseMaxZonesPerGroup = 1;
    [Tooltip("Growth in max zones per group per log(second) increase in elapsed time")]
    public float zonesGrowthRate = 0.1f;

    // Compute a continuously scaling difficulty factor
    private float DifficultyFactor => Mathf.Log(1f + elapsedTime);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Reset elapsed time explicitly on start, just in case
        elapsedTime = 0f; 
        StartCoroutine(LogDifficulty());
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
    }

    private IEnumerator LogDifficulty()
    {
        while (true)
        {
            Debug.Log($"[Difficulty] Time: {elapsedTime:F1}s | ForwardSpeed: {GetForwardSpeed():F2} | HorizontalSpeed: {GetHorizontalSpeed():F2} | Types: {GetAvailableTypesCount()} | SpawnDelay: {GetRandomAddDelay():F2} | MaxObstacles: {GetMaxObstaclesPerChunk()} | DeliveryChance: {GetDeliveryChance():P1} | MaxZones: {GetMaxZonesPerGroup()}");
            yield return new WaitForSeconds(5f);
        }
    }

    public float GetForwardSpeed()
    {
        return baseForwardSpeed + forwardSpeedGrowthRate * DifficultyFactor;
    }

    public float GetHorizontalSpeed()
    {
        return baseHorizontalSpeed + horizontalSpeedGrowthRate * DifficultyFactor;
    }

    public int GetAvailableTypesCount()
    {
        int count = Mathf.FloorToInt(baseAvailableTypes + availableTypesGrowthRate * DifficultyFactor);
        return Mathf.Max(1, count);
    }

    public float GetRandomAddDelay()
    {
        float delay = baseRandomAddDelay - randomAddDelayReductionRate * DifficultyFactor;
        return Mathf.Max(delay, minRandomAddDelay);
    }

    public int GetMaxObstaclesPerChunk()
    {
        int maxObs = Mathf.FloorToInt(baseMaxObstacles + obstaclesGrowthRate * DifficultyFactor);
        return Mathf.Max(0, maxObs);
    }

    public float GetDeliveryChance()
    {
        float chance = baseDeliveryChance + deliveryChanceGrowthRate * DifficultyFactor;
        return Mathf.Clamp01(chance);
    }

    public int GetMaxZonesPerGroup()
    {
        int zones = Mathf.FloorToInt(baseMaxZonesPerGroup + zonesGrowthRate * DifficultyFactor);
        return Mathf.Max(1, zones);
    }
}
