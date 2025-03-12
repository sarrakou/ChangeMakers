using UnityEngine;
using UnityEngine.UI;
using System.IO;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Android;

public class LocationValidator : MonoBehaviour
{
    [Header("Configuraci�n de ubicaci�n")]
    [SerializeField] private float targetLatitude = 0f;
    [SerializeField] private float targetLongitude = 0f;
    [SerializeField] private float acceptableDistanceInMeters = 50f;

    [Header("UI")]
    [SerializeField] private TMP_Text locationStatusText;

    private bool isLocationServiceRunning = false;
    private bool isLocationValid = false;

    private void Start()
    {
        StartCoroutine(StartLocationServices());
    }

    private IEnumerator StartLocationServices()
    {
        // Verificar si los servicios de ubicaci�n est�n habilitados
        if (!Input.location.isEnabledByUser)
        {
            // Solicitar permisos seg�n la plataforma
#if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
                yield return new WaitForSeconds(0.5f);
            }
#endif

            // Esperar a que el usuario conceda los permisos
            yield return new WaitForSeconds(1f);

            // Verificar nuevamente si los permisos fueron concedidos
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("El usuario no ha habilitado los servicios de ubicaci�n");
                if (locationStatusText != null)
                    locationStatusText.text = "Ubicaci�n: Desactivada";
                yield break;
            }
        }

        // Iniciar el servicio de ubicaci�n
        Input.location.Start(5f, 10f); // Precisi�n de 5 metros, actualizaci�n cada 10 metros

        // Esperar a que el servicio se inicie
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            if (locationStatusText != null)
                locationStatusText.text = "Ubicaci�n: Iniciando...";
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        // Verificar si el servicio se inici� correctamente
        if (maxWait <= 0)
        {
            Debug.LogWarning("Tiempo de espera agotado para iniciar el servicio de ubicaci�n");
            if (locationStatusText != null)
                locationStatusText.text = "Ubicaci�n: Error de tiempo";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning("No se pudo determinar la ubicaci�n del dispositivo");
            if (locationStatusText != null)
                locationStatusText.text = "Ubicaci�n: Error";
            yield break;
        }

        isLocationServiceRunning = true;
        Debug.Log("Servicio de ubicaci�n iniciado correctamente");

        // Empezar a verificar la ubicaci�n
        StartCoroutine(UpdateLocationStatus());
    }

    private IEnumerator UpdateLocationStatus()
    {
        while (isLocationServiceRunning)
        {
            // Obtener la ubicaci�n actual
            float currentLatitude = Input.location.lastData.latitude;
            float currentLongitude = Input.location.lastData.longitude;
            float currentAccuracy = Input.location.lastData.horizontalAccuracy;

            // Calcular la distancia al objetivo
            float distanceToTarget = CalculateDistance(currentLatitude, currentLongitude, targetLatitude, targetLongitude);

            // Verificar si est� dentro del radio aceptable
            isLocationValid = distanceToTarget <= acceptableDistanceInMeters;

            // Actualizar UI si existe
            if (locationStatusText != null)
            {
                string statusText = isLocationValid ?
                    $"Ubicaci�n v�lida ({distanceToTarget:F0}m)" :
                    $"Ubicaci�n inv�lida ({distanceToTarget:F0}m)";
                locationStatusText.text = statusText;
            }

            yield return new WaitForSeconds(3f);
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
        float currentLatitude = Input.location.lastData.latitude;
        float currentLongitude = Input.location.lastData.longitude;
        float distanceToTarget = CalculateDistance(currentLatitude, currentLongitude, targetLatitude, targetLongitude);
        isLocationValid = distanceToTarget <= acceptableDistanceInMeters;

        return isLocationValid;
    }

    // Obtener la informaci�n actual de ubicaci�n
    public LocationInfo GetCurrentLocation()
    {
        if (!isLocationServiceRunning)
            return default;

        return Input.location.lastData;
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