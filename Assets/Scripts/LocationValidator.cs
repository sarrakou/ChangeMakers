using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LocationValidator : MonoBehaviour
{
    [Header("Configuración de ubicación")]
    [SerializeField] private float targetLatitude = 48.84769f; // Paris
    [SerializeField] private float targetLongitude = 2.387231f;
    [SerializeField] private float acceptableDistanceInMeters = 2000f;

    [Header("UI")]
    [SerializeField] private TMP_Text locationStatusText;
    [SerializeField] private TMP_Text debugInfoText;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] public Button buttonTakePicture;

    [SerializeField] private InfoActionsChallenges infoActions;


    private bool isLocationServiceRunning = false;
    private bool isLocationValid = false;
    private float lastDistanceToTarget = 0f;

    private void Start()
    {
        StartCoroutine(StartLocationServices());
    }

    private IEnumerator StartLocationServices()
    {
        UpdateStatusText("Iniciando servicios de ubicación...");

        // Verificar si los servicios de ubicación están habilitados
        if (!Input.location.isEnabledByUser)
        {
#if PLATFORM_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
            {
                UpdateStatusText("Solicitando permisos de ubicación...");
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
                yield return new WaitForSeconds(1f);
            }
#endif

            if (!Input.location.isEnabledByUser)
            {
                UpdateStatusText("ERROR: Permisos de ubicación denegados");
                yield break;
            }
        }

        UpdateStatusText("Iniciando servicio GPS...");
        Input.location.Start(1f, 0.1f); 

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            UpdateStatusText($"Inicializando GPS... ({maxWait}s)");
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        if (maxWait <= 0)
        {
            UpdateStatusText("ERROR: Tiempo de espera agotado");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            UpdateStatusText("ERROR: Fallo en servicios de ubicación");
            yield break;
        }

        isLocationServiceRunning = true;
        UpdateStatusText("GPS activado correctamente");

        StartCoroutine(UpdateLocationStatus());
    }

    private IEnumerator UpdateLocationStatus()
    {
        while (isLocationServiceRunning)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                float currentLatitude = Input.location.lastData.latitude;
                float currentLongitude = Input.location.lastData.longitude;
                float currentAccuracy = Input.location.lastData.horizontalAccuracy;

                // Calcular la distancia al objetivo
                lastDistanceToTarget = CalculateDistance(currentLatitude, currentLongitude, targetLatitude, targetLongitude);

                isLocationValid = lastDistanceToTarget <= acceptableDistanceInMeters;

                // Actualizar texto de estado
                string statusText = isLocationValid ?
                    $"Ubicación VÁLIDA ({lastDistanceToTarget:F0}m)" :
                    $"Ubicación INVÁLIDA ({lastDistanceToTarget:F0}m)";
                UpdateStatusText(statusText);

                if(isLocationValid || !infoActions.requireLocationValidation)
                {
                    buttonTakePicture.enabled = true;
                }
                else 
                {
                    buttonTakePicture.enabled= false;
                }

                if (showDebugInfo && debugInfoText != null)
                {
                    string debugText = $"Ubicación actual: {currentLatitude:F6}, {currentLongitude:F6}\n" +
                                      $"Precisión: {currentAccuracy:F1}m\n" +
                                      $"Objetivo: {targetLatitude:F6}, {targetLongitude:F6}\n" +
                                      $"Distancia: {lastDistanceToTarget:F1}m\n" +
                                      $"Radio aceptable: {acceptableDistanceInMeters:F0}m\n" +
                                      $"Estado: {(isLocationValid ? "VÁLIDO" : "INVÁLIDO")}";
                    debugInfoText.text = debugText;
                }
            }
            else
            {
                UpdateStatusText($"Estado GPS: {Input.location.status}");
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateStatusText(string text)
    {
        if (infoActions.requireLocationValidation)
        {
            if (locationStatusText != null)
            {
                locationStatusText.text = text;
                Debug.Log("[LocationValidator] " + text);
            }
        }
        
    }
    public bool IsLocationValid()
    {
        if (!isLocationServiceRunning)
        {
            Debug.LogWarning("El servicio de ubicación no está funcionando");
            return false;
        }

        // Actualizar validación antes de devolver el resultado
        if (Input.location.status == LocationServiceStatus.Running)
        {
            float currentLatitude = Input.location.lastData.latitude;
            float currentLongitude = Input.location.lastData.longitude;
            lastDistanceToTarget = CalculateDistance(currentLatitude, currentLongitude, targetLatitude, targetLongitude);
            isLocationValid = lastDistanceToTarget <= acceptableDistanceInMeters;
        }

        return isLocationValid;
    }

    public LocationInfo GetCurrentLocation()
    {
        if (!isLocationServiceRunning || Input.location.status != LocationServiceStatus.Running)
            return default;

        return Input.location.lastData;
    }

    public void SetTargetLocation(float latitude, float longitude, float radius)
    {
        targetLatitude = latitude;
        targetLongitude = longitude;
        acceptableDistanceInMeters = radius;

        Debug.Log($"Nueva ubicación objetivo: {latitude:F6}, {longitude:F6}, radio: {radius:F0}m");
    }

    public float GetDistanceToTarget()
    {
        return lastDistanceToTarget;
    }

    private float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
    {
        float R = 6371000;
        float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        float dLon = (lon2 - lon1) * Mathf.Deg2Rad;

        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) *
                Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        float d = R * c;

        return d;
    }

    void OnDestroy()
    {
        if (isLocationServiceRunning)
        {
            Input.location.Stop();
            isLocationServiceRunning = false;
        }
    }
}