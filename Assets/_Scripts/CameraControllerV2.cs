using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControllerV2 : MonoBehaviour
{
  [Header("Pan")]
  [SerializeField] float panSpeed = 1f;

  [Header("Zoom (clamped by distance)")]
  [SerializeField] float scrollSpeed = 3f;
  [SerializeField] static float minZoom = 150f;
  [SerializeField] static float maxZoom = 500f;

  [Header("Orbit")]
  [SerializeField] float orbitSensitivity = 0.2f;
  [SerializeField] float minPitchDeg = 5f;
  [SerializeField] float maxPitchDeg = 80f;
  [SerializeField] bool orbitAroundTerrainCenter = false; // turn ON to get center orbit

  [Header("Ground / Fallback")]
  [SerializeField] LayerMask groundMask = ~0;   // include TerrainCollider layer
  [SerializeField] float rayMaxDistance = 10000f;
  [SerializeField] float fallbackPlaneY = 0f;   // used if raycast misses

  [SerializeField] float currentZoom;

  // orbit state
  bool orbiting = false;
  Vector3 orbitPivot;
  float orbitDistance;
  float yawDeg;
  float pitchDeg;

  public delegate void CameraZoomChangeEvent(float currentZoom);
  public static event CameraZoomChangeEvent OnCameraZoomChanged;

  void Update()
  {
    OrbitCamera();  // takes priority while Alt+RMB is held
    PanCamera();
    ZoomCamera();
  }

  void PanCamera()
  {
    if (!Mouse.current.rightButton.isPressed) return;
    if (IsAltDown()) return; // don't pan while orbiting

    Vector2 md = Mouse.current.delta.ReadValue();

    // Pan along ground plane (keeps height steady)
    Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
    Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

    Vector3 move = (-md.x * panSpeed * right) + (-md.y * panSpeed * fwd);
    transform.position += move;
  }

  void ZoomCamera()
  {
    if (!Mouse.current.scroll.IsActuated()) return;

    float scroll = Mouse.current.scroll.ReadValue().y;
    if (Mathf.Approximately(scroll, 0f)) return;

    // If orbiting, clamp relative to current orbit pivot; otherwise use look focus.
    Vector3 focus = orbiting ? orbitPivot : GetFocusPoint();

    Vector3 toCam = transform.position - focus;
    float dist = toCam.magnitude;

    float desiredDist = Mathf.Clamp(dist - (scroll * scrollSpeed), minZoom, maxZoom);

    if (toCam.sqrMagnitude > 0.0001f)
    {
      Vector3 dir = toCam / dist;
      transform.position = focus + dir * desiredDist;

      if (orbiting) transform.LookAt(orbitPivot, Vector3.up);
      currentZoom = desiredDist;
      OnCameraZoomChanged(currentZoom);
    }
    //Debug.Log("zoom = " + desiredDist);
  }

  void OrbitCamera()
  {
    bool wantOrbit = Mouse.current.rightButton.isPressed && IsAltDown();
    if (!wantOrbit) { orbiting = false; return; }

    Vector2 md = Mouse.current.delta.ReadValue();

    if (!orbiting)
    {
      orbiting = true;

      // KEY CHANGE: choose pivot at orbit start
      orbitPivot = orbitAroundTerrainCenter ? GetTerrainCenter() : GetFocusPoint();

      Vector3 toCam = transform.position - orbitPivot;
      orbitDistance = Mathf.Max(0.01f, toCam.magnitude);

      // derive yaw/pitch from current position
      Vector2 flat = new Vector2(toCam.x, toCam.z);
      yawDeg = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
      pitchDeg = Mathf.Atan2(toCam.y, flat.magnitude) * Mathf.Rad2Deg;
      return; // no snap on first frame
    }

    // update angles
    yawDeg += md.x * orbitSensitivity;
    pitchDeg -= md.y * orbitSensitivity;
    pitchDeg = Mathf.Clamp(pitchDeg, minPitchDeg, maxPitchDeg);

    // rebuild position from yaw/pitch/distance
    float yawRad = yawDeg * Mathf.Deg2Rad;
    float pitRad = pitchDeg * Mathf.Deg2Rad;

    float xz = orbitDistance * Mathf.Cos(pitRad);
    Vector3 newPos = new Vector3(
        orbitPivot.x + xz * Mathf.Sin(yawRad),
        orbitPivot.y + orbitDistance * Mathf.Sin(pitRad),
        orbitPivot.z + xz * Mathf.Cos(yawRad)
    );

    transform.position = newPos;
    transform.LookAt(orbitPivot, Vector3.up);
  }

  // --- Helpers -------------------------------------------------------------

  bool IsAltDown()
  {
    var kb = Keyboard.current;
    return kb != null && (kb.altKey.isPressed || kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
  }

  Vector3 GetFocusPoint()
  {
    Ray ray = new Ray(transform.position, transform.forward);
    if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
      return hit.point;

    // Fallback to infinite horizontal plane at fallbackPlaneY
    Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackPlaneY, 0f));
    if (plane.Raycast(ray, out float enter)) return ray.GetPoint(enter);

    // Last resort: some point ahead
    return transform.position + transform.forward * Mathf.Max(minZoom, 10f);
  }

  Vector3 GetTerrainCenter()
  {
    Terrain t = Terrain.activeTerrain;
    if (t != null && t.terrainData != null)
    {
      Vector3 pos = t.GetPosition();
      Vector3 size = t.terrainData.size;
      Vector3 center = new Vector3(pos.x + size.x * 0.5f, 0f, pos.z + size.z * 0.5f);
      center.y = t.SampleHeight(center) + pos.y;
      return center;
    }
    // fallback: use focus instead
    return GetFocusPoint();
  }

  public static (float,float) GetZoom()
  {
    return (minZoom, maxZoom);
  }
}
