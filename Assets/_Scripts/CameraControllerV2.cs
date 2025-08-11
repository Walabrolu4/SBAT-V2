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

  [Header("Focus Settings")]

  [SerializeField] LayerMask groundMask = ~0;
  [SerializeField] float rayMaxDistance = 10000f;
  [SerializeField] float fallbackPlaneY = 0f;

  void Update()
  {
    PanCamera();
    ZoomCamera();
  }

  void PanCamera()
  {
    if (Mouse.current.rightButton.isPressed)
    {
      Vector2 mouseDelta = Mouse.current.delta.ReadValue();

      Vector3 move = (
        (-mouseDelta.x * panSpeed * Vector3.right) +
        (-mouseDelta.y * panSpeed * Vector3.forward)
      );

      transform.position += move;
    }
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

  Vector3 GetFocusPoint()
  {
    Ray ray = new Ray(transform.position, transform.forward);
    if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
    {
      return hit.point;
    }

    // Fallback to infinite horizontal plane at fallbackPlaneY
    Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackPlaneY, 0f));
    if (plane.Raycast(ray, out float enter))
    {
      return ray.GetPoint(enter);
    }

    // If everything fails, just use a point ahead
    return transform.position + transform.forward * Mathf.Max(minZoom, 10f);
  }

}
