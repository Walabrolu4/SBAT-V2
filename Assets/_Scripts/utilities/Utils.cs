using UnityEngine;

public class Utils 
{
  public static float Map(float val, float in_min, float in_max, float out_min, float out_max)
  {
    float slope = (out_max - out_min) / (in_max - in_min);

    float output = out_min + slope * (val - in_min);
    return output;
  }
}
