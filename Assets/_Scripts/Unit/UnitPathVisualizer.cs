using UnityEngine;
using UnityEngine.AI;

public class UnitPathVisualizer : MonoBehaviour
{
  LineRenderer line;
  Transform target;

  void Start()
  {
    line = GetComponent<LineRenderer>();
  }

  public void SetOrigin(Vector3 origin)
  {
    line.SetPosition(0, transform.position);
  }
  public void ClearPath()
  {
    line.positionCount = 0;
  }

  public void DrawPath(NavMeshPath path)
  {
    if (path.corners.Length < 2) return;

    line.positionCount = path.corners.Length;

    for (int i = 0; i < path.corners.Length; i++)
    {
      line.SetPosition(i, path.corners[i]);
    }
  }
}
