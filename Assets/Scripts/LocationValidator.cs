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
    [Header("Configuración de ubicación")]
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
        // Verificar si los servicios de ubicación están habilitados
        if (!Input.location.isEnabledByUser)
        {
            // Solicitar permisos según la plataforma
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
                Debug.LogWarning("El usuario no ha habilitado los servicios de ubicación");
                if (locationStatusText != null)
                    locationStatusText.text = "Ubicación: Desactivada";
                yield break;
            }
        }

        // Iniciar el servicio de ubicación
        Input.location.Start(5f, 10f); // Precisión de 5 metros, actualización cada 10 metros

        // Esperar a que el servicio se inicie
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            if (locationStatusText != null)
                locationStatusText.text = "Ubicación: Iniciando...";
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        // Verificar si el servicio se inició correctamente
        if (maxWait <= 0)
        {
            Debug.LogWarning("Tiempo de espera agotado para iniciar el servicio de ubicación");
            if (locationStatusText != null)
                locationStatusText.text = "Ubicación: Error de tiempo";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning("No se pudo determinar la ubicación del dispositivo");
            if (locationStatusText != null)
                locationStatusText.text = "Ubicación: Error";
            yield break;
        }

        isLocationServiceRunning = true;
        Debug.Log("Servicio de ubicación iniciado correctamente");

        // Empezar a verificar la ubicación
        StartCoroutine(UpdateLocationStatus());
    }

    private IEnumerator UpdateLocationStatus()
    {
        while (isLocationServiceRunning)
        {
            // Obtener la ubicación actual
            float currentLatitude = Input.location.lastData.latitude;
            float currentLongitude = Input.location.lastData.longitude;
            float currentAccuracy = Input.location.lastData.horizontalAccuracy;

            // Calcular la distancia al objetivo
            float distanceToTarget = CalculateDistance(currentLatitude, currentLongitude, targetLatitude, targetLongitude);

            // Verificar si está dentro del radio aceptable
            isLocationValid = distanceToTarget <= acceptableDistanceInMeters;

            // Actualizar UI si existe
            if (locationStatusText != null)
            {
                string statusText = isLocationValid ?
                    $"Ubicación válida ({distanceToTarget:F0}m)" :
                    $"Ubicación inválida ({distanceToTarget:F0}m)";
                locationStatusText.text = statusText;
            }

            yield return new WaitForSeconds(3f);
        }
    }

    // Método público para validar la ubicación actual
    public bool IsLocationValid()
    {
        if (!isLocationServiceRunning)
        {
            Debug.LogWarning("El servicio de ubicación no está funcionando");
            return false;
        }

        // Actualizar validación antes de devolver el resultado
        float currentLatitude = Input.location.lastData.latitude;
        float currentLongitude = Input.location.lastData.longitude;
        float distanceToTarget = CalculateDistance(currentLatitude, currentLongitude, targetLatitude, targetLongitude);
        isLocationValid = distanceToTarget <= acceptableDistanceInMeters;

        return isLocationValid;
    }

    // Obtener la información actual de ubicación
    public LocationInfo GetCurrentLocation()
    {
        if (!isLocationServiceRunning)
            return default;

        return Input.location.lastData;
    }

    // Calcular la distancia entre dos puntos geográficos (fórmula de Haversine)
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
        // Detener el servicio de ubicación cuando se destruye el objeto
        if (isLocationServiceRunning)
        {
            Input.location.Stop();
            isLocationServiceRunning = false;
        }
    }
}