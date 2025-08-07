using UnityEngine.InputSystem;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] Transform orbitTarget;

    [Header("Pan")]
    [SerializeField] float panSpeed = 1f;     // tune to taste (units per pixel at dist=500 ≈ 1)
    [SerializeField] float panDistanceScale = 0.002f; // pan scales with distance so it’s not 1:1

    [Header("Orbit")]
    [SerializeField] float orbitSpeed = 150f; // deg/sec per pixel
    [SerializeField] float minPitch = 5f;
    [SerializeField] float maxPitch = 80f;

    [Header("Zoom (dolly)")]
    [SerializeField] float zoomSpeed = 10f;   // units per scroll step
    [SerializeField] float minDistance = 2f;
    [SerializeField] float maxDistance = 120f;

    // If no target is set, we’ll fall back to FOV zoom:
    [SerializeField] float minFov = 20f;
    [SerializeField] float maxFov = 90f;

    float yaw;       // world yaw around Y
    float pitch;     // elevation
    float distance;  // radius to target
    bool orbitInit;  // did we sync yaw/pitch/distance from current transform?

    void Start()
    {
        if (orbitTarget != null) InitOrbitFromCurrent();
    }

    void Update()
    {
        HandlePanOrOrbit();
        HandleZoom();
    }

    void InitOrbitFromCurrent()
    {
        if (orbitTarget == null) return;

        Vector3 toCam = transform.position - orbitTarget.position;
        distance = Mathf.Max(0.001f, toCam.magnitude);

        Vector3 dir = toCam.normalized;
        // yaw around world up (Y), pitch from horizontal
        yaw   = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        orbitInit = true;
    }

    void HandlePanOrOrbit()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null) return;

        bool rightDown = mouse.rightButton.isPressed;
        bool altHeld   = kb != null && kb.altKey.isPressed;

        if (!rightDown) return;

        // Use per-frame mouse delta to avoid the "snap" on press.
        Vector2 mDelta = mouse.delta.ReadValue();

        if (altHeld && orbitTarget != null)
        {
            if (!orbitInit) InitOrbitFromCurrent();

            // Update angles
            yaw   += mDelta.x * orbitSpeed * Time.deltaTime;
            pitch -= mDelta.y * orbitSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Rebuild position from spherical coords, keep upright
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = orbitTarget.position + rot * new Vector3(0f, 0f, -distance);
            transform.LookAt(orbitTarget.position, Vector3.up);
        }
        else
        {
            // Pan in camera plane; scale by distance so it feels consistent
            float d = orbitTarget ? distance : 500f; // fallback distance if no target
            float scale = panSpeed * (d * panDistanceScale);
            Vector3 move = (-transform.right * mDelta.x - transform.up * mDelta.y) * scale;

            transform.position += move;
            if (orbitTarget != null)
            {
                orbitTarget.position += move; // keep orbit center under cursor while panning
            }
        }
    }

    void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        if (orbitTarget != null)
        {
            if (!orbitInit) InitOrbitFromCurrent();

            // Dolly (move camera along its forward axis), not FOV
            distance = Mathf.Clamp(distance - scroll * zoomSpeed * Time.deltaTime, minDistance, maxDistance);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = orbitTarget.position + rot * new Vector3(0f, 0f, -distance);
            transform.LookAt(orbitTarget.position, Vector3.up);
        }
        else
        {
            // No target: fall back to FOV zoom
            var cam = Camera.main;
            if (cam != null)
            {
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed * Time.deltaTime, minFov, maxFov);
            }
        }
    }
}



/*
public class CameraController : MonoBehaviour
{

  [Header("Pan Settings")]
  [SerializeField] float panSpeed = 100f;

  [Header("Zoom Settings")]
  [SerializeField] float zoomSpeed = 10f;
  [SerializeField] float minZoom = 2f;
  [SerializeField] float maxZoom = 120;

  [Header("Orbit Settings")]
  [SerializeField] float orbitSpeed = 100f;
  [SerializeField] Transform orbitTarget;

  private Vector2 lastMousePosition;
  private Vector3 dragOriginWorldPos;
  private bool isDragging = false;


  // Update is called once per frame
  void Update()
  {
    HandlePanOrOrbit();
    HandleZoom();
  }

  void HandlePan()
  {
    if (Mouse.current.leftButton.wasPressedThisFrame)
    {
      isDragging = true;
      lastMousePosition = Mouse.current.position.ReadValue();
    }

    if (Mouse.current.leftButton.isPressed)
    {
      Vector2 delta = Mouse.current.position.ReadValue() - lastMousePosition;
      Vector3 move = new Vector3(-delta.x, -delta.y, 0) * panSpeed * Time.deltaTime;
      transform.Translate(move, Space.Self);

      lastMousePosition = Mouse.current.position.ReadValue();
    }

    if (Mouse.current.leftButton.wasReleasedThisFrame)
    {
      isDragging = false;
    }
  }


  void HandlePanOrOrbit()
  {
    // Right mouse button held down
    if (Mouse.current.rightButton.isPressed)
    {
      Vector2 mousePos = Mouse.current.position.ReadValue();
      Vector2 delta = mousePos - lastMousePosition;

      if (Keyboard.current.altKey.isPressed && orbitTarget != null)
      {
        // Orbit mode
        float yaw = delta.x * orbitSpeed * Time.deltaTime;
        float pitch = -delta.y * orbitSpeed * Time.deltaTime;

        // Rotate around the target in world space
        transform.RotateAround(orbitTarget.position, Vector3.up, yaw);
        transform.RotateAround(orbitTarget.position, transform.right, pitch);
      }
      else
      {
        // Pan mode (not 1:1)
        Vector3 move = new Vector3(-delta.x, -delta.y, 0) * panSpeed * Time.deltaTime;
        transform.Translate(move, Space.Self);
      }

      lastMousePosition = mousePos;
    }

    if (Mouse.current.rightButton.wasPressedThisFrame)
    {
      lastMousePosition = Mouse.current.position.ReadValue();
    }
  }

  void HandleZoom()
  {
    float scroll = Mouse.current.scroll.ReadValue().y;
    Camera.main.fieldOfView -= scroll * zoomSpeed;
    Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView, minZoom, maxZoom);
  }
}
*/

