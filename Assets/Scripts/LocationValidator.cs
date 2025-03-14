using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LocationValidator : MonoBehaviour
{
    [Header("Configuraci�n de ubicaci�n")]
    [SerializeField] private float targetLatitude = 48.84769f; // Par�s
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
        // Mostrar mensaje de inicio
        UpdateStatusText("Iniciando servicios de ubicaci�n...");

        // Verificar si los servicios de ubicaci�n est�n habilitados
        if (!Input.location.isEnabledByUser)
        {
            // Solicitar permisos seg�n la plataforma
#if PLATFORM_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
            {
                UpdateStatusText("Solicitando permisos de ubicaci�n...");
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
                yield return new WaitForSeconds(1f);
            }
#endif

            // Verificar nuevamente si los permisos fueron concedidos
            if (!Input.location.isEnabledByUser)
            {
                UpdateStatusText("ERROR: Permisos de ubicaci�n denegados");
                yield break;
            }
        }

        // Iniciar el servicio de ubicaci�n
        UpdateStatusText("Iniciando servicio GPS...");
        Input.location.Start(1f, 0.1f); // Mayor precisi�n y frecuencia de actualizaci�n

        // Esperar a que el servicio se inicie
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            UpdateStatusText($"Inicializando GPS... ({maxWait}s)");
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        // Verificar si el servicio se inici� correctamente
        if (maxWait <= 0)
        {
            UpdateStatusText("ERROR: Tiempo de espera agotado");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            UpdateStatusText("ERROR: Fallo en servicios de ubicaci�n");
            yield break;
        }

        // Servicio iniciado correctamente
        isLocationServiceRunning = true;
        UpdateStatusText("GPS activado correctamente");

        // Empezar a verificar la ubicaci�n
        StartCoroutine(UpdateLocationStatus());
    }

    private IEnumerator UpdateLocationStatus()
    {
        while (isLocationServiceRunning)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                // Obtener la ubicaci�n actual
                float currentLatitude = Input.location.lastData.latitude;
                float currentLongitude = Input.location.lastData.longitude;
                float currentAccuracy = Input.location.lastData.horizontalAccuracy;

                // Calcular la distancia al objetivo
                lastDistanceToTarget = CalculateDistance(currentLatitude, currentLongitude, targetLatitude, targetLongitude);

                // Verificar si est� dentro del radio aceptable
                isLocationValid = lastDistanceToTarget <= acceptableDistanceInMeters;

                // Actualizar texto de estado
                string statusText = isLocationValid ?
                    $"Ubicaci�n V�LIDA ({lastDistanceToTarget:F0}m)" :
                    $"Ubicaci�n INV�LIDA ({lastDistanceToTarget:F0}m)";
                UpdateStatusText(statusText);

                if(isLocationValid || !infoActions.requireLocationValidation)
                {
                    buttonTakePicture.enabled = true;
                }
                else 
                {
                    buttonTakePicture.enabled= false;
                }

                // Mostrar informaci�n de depuraci�n
                if (showDebugInfo && debugInfoText != null)
                {
                    string debugText = $"Ubicaci�n actual: {currentLatitude:F6}, {currentLongitude:F6}\n" +
                                      $"Precisi�n: {currentAccuracy:F1}m\n" +
                                      $"Objetivo: {targetLatitude:F6}, {targetLongitude:F6}\n" +
                                      $"Distancia: {lastDistanceToTarget:F1}m\n" +
                                      $"Radio aceptable: {acceptableDistanceInMeters:F0}m\n" +
                                      $"Estado: {(isLocationValid ? "V�LIDO" : "INV�LIDO")}";
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

    // M�todo p�blico para validar la ubicaci�n actual
    public bool IsLocationValid()
    {
        if (!isLocationServiceRunning)
        {
            Debug.LogWarning("El servicio de ubicaci�n no est� funcionando");
            return false;
        }

        // Actualizar validaci�n antes de devolver el resultado
        if (Input.location.status == LocationServiceStatus.Running)
        {
            float currentLatitude = Input.location.lastData.latitude;
            float currentLongitude = Input.location.lastData.longitude;
            lastDistanceToTarget = CalculateDistance(currentLatitude, currentLongitude, targetLatitude, targetLongitude);
            isLocationValid = lastDistanceToTarget <= acceptableDistanceInMeters;
        }

        return isLocationValid;
    }

    // Obtener la informaci�n actual de ubicaci�n
    public LocationInfo GetCurrentLocation()
    {
        if (!isLocationServiceRunning || Input.location.status != LocationServiceStatus.Running)
            return default;

        return Input.location.lastData;
    }

    // M�todo p�blico para establecer la ubicaci�n objetivo
    public void SetTargetLocation(float latitude, float longitude, float radius)
    {
        targetLatitude = latitude;
        targetLongitude = longitude;
        acceptableDistanceInMeters = radius;

        Debug.Log($"Nueva ubicaci�n objetivo: {latitude:F6}, {longitude:F6}, radio: {radius:F0}m");
    }

    // M�todo p�blico para obtener la distancia actual al objetivo
    public float GetDistanceToTarget()
    {
        return lastDistanceToTarget;
    }

    // Calcular la distancia entre dos puntos geogr�ficos (f�rmula de Haversine)
    private float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
    {
        float R = 6371000; // Radio de la Tierra en metros
        float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        float dLon = (lon2 - lon1) * Mathf.Deg2Rad;

        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) *
                Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        float d = R * c;

        return d; // Distancia en metros
    }

    void OnDestroy()
    {
        // Detener el servicio de ubicaci�n cuando se destruye el objeto
        if (isLocationServiceRunning)
        {
            Input.location.Stop();
            isLocationServiceRunning = false;
        }
    }
}