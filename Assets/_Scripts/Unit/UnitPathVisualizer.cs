using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class UnitPathVisualizer : MonoBehaviour
{
  [Header("Options")]
  [SerializeField] float yOffset = 0.05f;      // lift above ground to avoid z-fighting
  [SerializeField] bool autoFollowAgent = false; // redraw while agent moves

  LineRenderer line;
  NavMeshAgent agent;     // optional (if placed under/with the agent)
  NavMeshPath tempPath;   // initialized in Awake
  Coroutine followRoutine;
  Coroutine waitDrawRoutine;

  void Awake()
  {
    line = GetComponent<LineRenderer>();
    line.useWorldSpace = true;
    if (!line.material) line.material = new Material(Shader.Find("Sprites/Default"));
    agent = GetComponentInParent<NavMeshAgent>();
    tempPath = new NavMeshPath();
  }

  void OnDisable()
  {
    StopAllCoroutines();
  }

  // -------- Public API (names kept similar) --------

  public void SetOrigin(Vector3 origin)
  {
    line.positionCount = 1;
    line.SetPosition(0, Lift(origin));
  }

  public void ClearPath()
  {
    line.positionCount = 0;
  }

  public void DrawPath(NavMeshPath path)
  {
    if (path == null || path.corners == null || path.corners.Length < 2)
    {
      ClearPath();
      return;
    }

    int n = path.corners.Length;
    line.positionCount = n;
    for (int i = 0; i < n; i++)
      line.SetPosition(i, Lift(path.corners[i]));
  }

  /// Draw from an agent. If its path is still pending, wait until it’s ready, then draw.
  public void DrawFromAgent(NavMeshAgent a)
  {
    if (a == null)
    {
      ClearPath();
      return;
    }

    // Stop any previous wait/draw
    if (waitDrawRoutine != null) StopCoroutine(waitDrawRoutine);
    waitDrawRoutine = StartCoroutine(Co_WaitAndDrawAgentPath(a));

    if (autoFollowAgent)
    {
      if (followRoutine != null) StopCoroutine(followRoutine);
      followRoutine = StartCoroutine(Co_FollowAgentPath(a));
    }
  }

  /// Compute immediately (synchronous) and draw, without using the agent’s async path.
  public bool ComputeAndDraw(Vector3 origin, Vector3 destination, int areaMask = NavMesh.AllAreas)
  {
    SetOrigin(origin);

    if (NavMesh.CalculatePath(origin, destination, areaMask, tempPath) &&
        tempPath.status != NavMeshPathStatus.PathInvalid &&
        tempPath.corners.Length >= 2)
    {
      DrawPath(tempPath);
      return true;
    }

    ClearPath();
    return false;
  }

  // -------- Coroutines --------

  IEnumerator Co_WaitAndDrawAgentPath(NavMeshAgent a)
  {
    // Wait until the agent has finished computing its path this frame
    while (a.pathPending) yield return null;

    if (a.hasPath && a.path.corners.Length >= 2)
      DrawPath(a.path);
    else
      ClearPath();
  }

  // Optional: keep updating as the agent moves/replans
  IEnumerator Co_FollowAgentPath(NavMeshAgent a)
  {
    // small interval is enough; per-frame is overkill for visualization
    const float interval = 0.1f;

    while (a != null && a.enabled)
    {
      if (!a.pathPending && a.hasPath && a.path.corners.Length >= 2)
        DrawPath(a.path);
      else if (!a.pathPending && !a.hasPath)
        ClearPath();

      yield return new WaitForSeconds(interval);
    }
  }

  // -------- Utility --------

  Vector3 Lift(Vector3 p) => new Vector3(p.x, p.y + yOffset, p.z);
}
