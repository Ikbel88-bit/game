using UnityEngine;

// Keeps the sky sphere centred on the active camera so the world has no visible edge.
public class XLAtmosphereFollow : MonoBehaviour
{
    Camera cam;

    void LateUpdate()
    {
        if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
        if (cam == null) return;
        transform.position = cam.transform.position;
        if (cam.farClipPlane < 1500f) cam.farClipPlane = 1500f;
    }
}
