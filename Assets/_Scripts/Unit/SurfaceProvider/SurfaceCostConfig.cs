using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RTS/Surface Cost Config")]
public sealed class SurfaceCostConfig : ScriptableObject
{
  [System.Serializable]
  public struct Entry { public string areaName; public float speedMultiplier; }

  public Entry[] entries;

  // Built at runtime for quick lookups:
  private Dictionary<int, float> _byAreaIndex;

  public void BuildCache()
  {
    _byAreaIndex = new Dictionary<int, float>(entries?.Length ?? 0);
    if (entries == null) return;
    foreach (var e in entries)
    {
      int idx = UnityEngine.AI.NavMesh.GetAreaFromName(e.areaName);
      if (idx >= 0 && !_byAreaIndex.ContainsKey(idx))
        _byAreaIndex[idx] = Mathf.Max(0.01f, e.speedMultiplier);
    }
  }

  public float MultiplierForAreaIndex(int areaIndex, float fallback = 1f)
      => (_byAreaIndex != null && _byAreaIndex.TryGetValue(areaIndex, out var m)) ? m : fallback;
}
