using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControllerV2 : MonoBehaviour
{
  [Header("Pan Settings")]
  [SerializeField] float panSpeed = 1f;

  [Header("Zoom Settings")]
  [SerializeField] float scrollSpeed = 1f;
  [SerializeField] float minZoom = 150f;
  [SerializeField] float maxZoom = 500f;


  [Header("Orbit")]
  [SerializeField] float orbitSensitivity = 0.2f; // deg per pixel-ish
  [SerializeField] float minPitchDeg = 5f;
  [SerializeField] float maxPitchDeg = 80f;
  [SerializeField] float fallbackPlaneY = 0f;     // if no Terrain found
  [SerializeField] Transform orbitPivotOverride;  // optional manual pivot

  [Header("Focus Settings")]

  [SerializeField] LayerMask groundMask = ~0;
  [SerializeField] float rayMaxDistance = 10000f;

  bool orbiting = false;
  Vector3 orbitPivot;
  float orbitDistance;
  float yawDeg;
  float pitchDeg;

  void Update()
  {
    OrbitCamera();
    PanCamera();
    ZoomCamera();
  }

  void OrbitCamera()
  {
    bool wantOrbit = Mouse.current.rightButton.isPressed && IsAltDown();
    if (!wantOrbit)
    {
      orbiting = false;
      return;
    }

    Vector2 md = Mouse.current.delta.ReadValue();

    if (!orbiting)
    {
      orbiting = true;
      orbitPivot = GetOrbitPivot();
      Vector3 toCam = transform.position - orbitPivot;
      orbitDistance = Mathf.Max(0.01f, toCam.magnitude);

      // derive yaw/pitch from current position
      Vector2 flat = new Vector2(toCam.x, toCam.z);
      yawDeg = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
      pitchDeg = Mathf.Atan2(toCam.y, flat.magnitude) * Mathf.Rad2Deg;
      return; // avoid a first-frame "snap"
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

  void PanCamera()
  {
    if (!Mouse.current.rightButton.isPressed) return;
    if (IsAltDown()) return; // don't pan while orbiting

    Vector2 md = Mouse.current.delta.ReadValue();

    // Pan along ground plane (keeps height steady)
    Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
    Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

    Vector3 move =
        (-md.x * panSpeed * right) +
        (-md.y * panSpeed * fwd);

    transform.position += move;
  }


  void ZoomCamera()
  {

    if (!Mouse.current.scroll.IsActuated()) return;

    float scroll = Mouse.current.scroll.ReadValue().y;
    if (Mathf.Approximately(scroll, 0f)) return;

    // Find current focus point in front of camera (terrain if possible)
    Vector3 focus = GetFocusPoint();

    Vector3 toCam = transform.position - focus;
    float dist = toCam.magnitude;

    // Scroll direction tweak: flip sign if it feels backwards
    float desiredDist = Mathf.Clamp(dist - (scroll * scrollSpeed), minZoom, maxZoom);

    // Keep direction, set clamped distance
    if (toCam.sqrMagnitude > 0.0001f)
    {
      Vector3 dir = toCam / dist;
      transform.position = focus + dir * desiredDist;
    }
  }

  // --- Helpers -------------------------------------------------------------

  bool IsAltDown()
  {
    var kb = Keyboard.current;
    return kb != null && (kb.altKey.isPressed || kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
  }

  Vector3 GetOrbitPivot()
  {
    if (orbitPivotOverride != null) return orbitPivotOverride.position;

    Terrain t = Terrain.activeTerrain;
    if (t != null && t.terrainData != null)
    {
      Vector3 pos = t.GetPosition();
      Vector3 size = t.terrainData.size;
      Vector3 center = new Vector3(pos.x + size.x * 0.5f, 0f, pos.z + size.z * 0.5f);

      // Terrain.SampleHeight expects world XZ; add terrain base Y to get world Y.
      float h = t.SampleHeight(center) + pos.y;
      center.y = h;
      return center;
    }

    // Fallback: center on an infinite horizontal plane at fallbackPlaneY,
    // directly under current forward ray.
    Ray ray = new Ray(transform.position, transform.forward);
    Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackPlaneY, 0f));
    if (plane.Raycast(ray, out float enter)) return ray.GetPoint(enter);

    // Last resort: current position projected to plane
    return new Vector3(transform.position.x, fallbackPlaneY, transform.position.z);
  }

  Vector3 GetFocusPoint()
  {
    Ray ray = new Ray(transform.position, transform.forward);
    if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
      return hit.point;

    // Fallback to plane at fallbackPlaneY
    Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackPlaneY, 0f));
    if (plane.Raycast(ray, out float enter))
      return ray.GetPoint(enter);

    return transform.position + transform.forward * Mathf.Max(minZoom, 10f);
  }
}
