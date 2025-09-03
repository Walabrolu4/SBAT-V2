using UnityEngine;

public class UnitScaler : MonoBehaviour
{

  CameraControllerV2 cameraController;
  [SerializeField] float minSize = 25;
  [SerializeField] float maxSize = 100;

  float minZoom, maxZoom;

  //float currentZoom;
  void Start()
  {
    cameraController = Camera.main.gameObject.GetComponent<CameraControllerV2>();

    if (!cameraController)
    {
      Debug.LogError("NO SCRIPT FOUND ON CAMERA");
    }
    //currentZoom = camera.GetZoom();
    CameraControllerV2.OnCameraZoomChanged += ChangeScale;

    (minZoom, maxZoom) = CameraControllerV2.GetZoom();
  }

  void ChangeScale(float currentZoom)
  {
    float newScale = Utils.Map(currentZoom, minZoom, maxZoom, minSize, maxSize);
    transform.localScale = new Vector3(newScale, newScale, newScale);
    //Debug.Log(gameObject.name + "Scale Changed to " + newScale);
  }
}
