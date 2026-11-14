using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class BinocularZoom : MonoBehaviour
{
    [Header("Камера")]
    public CinemachineCamera cinemachineCamera;

    [Header("Налаштування бінокля")]
    public float normalZoom = 5f;
    public float binocularZoom = 2.5f;
    public float zoomSpeed = 5f;

    private float targetZoom;

    private void Start()
    {
        targetZoom = normalZoom;

        if (cinemachineCamera != null)
            cinemachineCamera.Lens.OrthographicSize = normalZoom;
    }

    private void Update()
    {
        if (cinemachineCamera == null)
            return;

        bool canUseBinoculars =
            GameManager.Instance != null &&
            GameManager.Instance.hasBinoculars;

        if (canUseBinoculars &&
            Mouse.current != null &&
            Mouse.current.rightButton.isPressed)
        {
            targetZoom = binocularZoom;
        }
        else
        {
            targetZoom = normalZoom;
        }

        float currentZoom = cinemachineCamera.Lens.OrthographicSize;

        cinemachineCamera.Lens.OrthographicSize =
            Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSpeed);
    }
}