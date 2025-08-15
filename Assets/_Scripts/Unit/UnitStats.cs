using UnityEngine;

[CreateAssetMenu(fileName = "UnitStats", menuName = "Units/UnitStats")]
public class UnitStats : ScriptableObject
{
  public string displayName = "Unit";
  public float baseSpeed = 3f;
  public float maxHP = 100f;
  public float maxFuel = 100f;
  public float fuelPerMeter = 0.1f;   
}
