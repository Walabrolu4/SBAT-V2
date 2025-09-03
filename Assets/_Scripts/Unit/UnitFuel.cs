using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitFuel : MonoBehaviour
{
  [SerializeField] private Unit unit;

  void Awake()
  {
    unit = GetComponent<Unit>();
  }
  void Reset()
  {
    unit = GetComponent<Unit>();
  }

  public void Consume(float amount) => unit?.TryConsumeFuel(amount);
  public void Refuel(float amount) => unit?.AddFuel(amount);
}
