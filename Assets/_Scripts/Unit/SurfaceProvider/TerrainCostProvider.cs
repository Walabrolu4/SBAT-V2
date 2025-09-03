using UnityEngine;
using UnityEngine.AI;

public sealed class TerrainCostProvider : MonoBehaviour, ITerrainSpeedModifiers
{
  [Header("Config")]
  [SerializeField] private SurfaceCostConfig surfaceConfig;
  [SerializeField] private float slopeProbe = 0.5f;          // meters ahead/behind
  [SerializeField] private float navmeshSampleRadius = 1f;   // for SamplePosition

  private Terrain terrain; // assuming one terrain; expand to handle tiles if needed

  void Awake()
  {
    if (surfaceConfig) surfaceConfig.BuildCache();
    terrain = Terrain.activeTerrain; // or FindFirstObjectByType<Terrain>()
  }

  // +grade = uphill, -grade = downhill (rise/run along travel direction)
  public float GetGrade(Vector3 worldPos, Vector3 dirNormalized)
  {
    if (terrain == null || dirNormalized.sqrMagnitude < 1e-6f) return 0f;

    Vector3 ahead = worldPos + dirNormalized * slopeProbe;
    Vector3 behind = worldPos - dirNormalized * slopeProbe;

    float hAhead = terrain.SampleHeight(ahead) + terrain.GetPosition().y;
    float hBehind = terrain.SampleHeight(behind) + terrain.GetPosition().y;

    return (hAhead - hBehind) / (2f * slopeProbe);
  }

  // 1.0 = neutral; >1 faster (e.g., Road), <1 slower (e.g., Rough/Mud)
  public float GetSurfaceMultiplier(Vector3 worldPos)
  {
    // Sample nearest NavMesh polygon and read its area bit
    if (NavMesh.SamplePosition(worldPos, out var hit, navmeshSampleRadius, NavMesh.AllAreas))
    {
      int areaIndex = FirstSetBit(hit.mask); // decode area from bitmask
      return surfaceConfig ? surfaceConfig.MultiplierForAreaIndex(areaIndex, 1f) : 1f;
    }
    return 1f;
  }

  // Utility: extract area index from hit.mask (one bit set for the polygon's area)
  private static int FirstSetBit(int mask)
  {
    if (mask == 0) return 0;
    for (int i = 0; i < 32; i++)
      if ((mask & (1 << i)) != 0) return i;
    return 0;
  }
}
