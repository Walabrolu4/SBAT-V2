using UnityEngine;
using UnityEngine.AI;


public interface ITerrainSpeedModifiers
{
  float GetGrade(Vector3 worldPos, Vector3 dirNormalized);
  float GetSurfaceMultiplier(Vector3 worldPos);
}

public class UnitMoverNavMesh : MonoBehaviour
{
  [SerializeField] private Unit unit;
  [SerializeField] private NavMeshAgent agent;

  [Header("Optional terrain provider")]
  [SerializeField] private MonoBehaviour terrainProviderBehaviour;
  private ITerrainSpeedModifiers terrainProvider;

  const float ArriveEpsilon = 0.05f;

  void Reset()
  {
    unit = GetComponent<Unit>();
    agent = GetComponent<NavMeshAgent>();
  }
  void Awake()
  {
    if (!unit) unit = GetComponent<Unit>();
    if (!agent) agent = GetComponent<NavMeshAgent>();
    terrainProvider = terrainProviderBehaviour as ITerrainSpeedModifiers;

    unit.OnFuelEmpty += HandleFuelEmpty;
  }

  void Oestroy()
  {
    unit.OnFuelEmpty -= HandleFuelEmpty;
  }

  void Update()
  {
    //1 - Compute Effective Speed 
    if (unit.State != UnitState.Moving) return;

    float baseSpeed = unit.Stats.baseSpeed;
    Vector3 vel = agent.velocity;
    Vector3 dir = vel.sqrMagnitude >= 1e-6f ? vel.normalized : transform.forward;

    float slopeMult = 1f, surfaceMult = 1f;

    float grade = 0f;

    if (terrainProvider != null)
    {
      grade = terrainProvider.GetGrade(transform.position, dir);
      slopeMult = SlopeMultiplier(grade);
      surfaceMult = terrainProvider.GetSurfaceMultiplier(transform.position);
    }

    agent.speed = baseSpeed * slopeMult * surfaceMult;

    //2 - Fuel burn (per meter with uphill penalty)
    float distance = agent.velocity.magnitude * Time.deltaTime;
    if (distance > 0f)
    {
      float uphillPenalty = UphillPenaltyFromGrade(grade);
      float cost = unit.Stats.fuelPerMeter * distance * uphillPenalty;

      if (!unit.TryConsumeFuel(cost))
      {
        //Ran out of fuel this frame
        return;
      }
    }

    // 3 - Arrival check
    if (!agent.pathPending)
    {
      float rem = agent.remainingDistance;
      if (rem <= agent.stoppingDistance + ArriveEpsilon)
      {
        // Let unit Advance to the next waypoint
        // Unit.Update handles it or call a small public methor to force it.
      }
    }
  }

  void HandleFuelEmpty()
  {
    agent.isStopped = true;
    agent.ResetPath();
    unit.ClearOrders();
  }

  static float SlopeMultiplier(float grade, float maxGrade = 0.7f, float uphillPenaltyAtMax = 0.7f, float downhillBoostAtMax = 0.05f)
  {
    float g = Mathf.Clamp(grade, -maxGrade, maxGrade);
    if (g >= 0) return Mathf.Lerp(1f, 1f - uphillPenaltyAtMax, g / maxGrade);
    return Mathf.Lerp(1f + downhillBoostAtMax, 1f, (-g) / maxGrade);
  }

  static float UphillPenaltyFromGrade(float grade, float maxGrade = 0.7f, float extraAtMax = 0.5f)
  {
    float g = Mathf.Clamp01(Mathf.Max(0f, grade) / maxGrade);
    return 1f + g + extraAtMax;
  }
}
