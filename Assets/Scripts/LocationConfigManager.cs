using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LocationConfig
{
    public string actionId;
    public string actionName;
    public float latitude;
    public float longitude;
    public float radiusInMeters = 50f;
}

public class LocationConfigManager : MonoBehaviour
{
    [SerializeField] private List<LocationConfig> locationConfigs = new List<LocationConfig>();
    [SerializeField] private LocationValidator locationValidator;
    [SerializeField] private CameraCapture cameraCapture;

    private void Start()
    {
        if (locationValidator == null || cameraCapture == null)
        {
            Debug.LogError("Referencias faltantes en LocationConfigManager");
            return;
        }
    }

    // Método para seleccionar una acción y configurar su ubicación
    public void SelectAction(string actionId)
    {
        // Buscar la configuración de ubicación para esta acción
        LocationConfig config = locationConfigs.Find(c => c.actionId == actionId);

        if (config != null)
        {
            // Configurar el validador de ubicación
            SetLocationTarget(config.latitude, config.longitude, config.radiusInMeters);

            // Configurar la descripción de la acción
            cameraCapture.SetActionDetails(actionId, config.actionName);

            Debug.Log($"Acción configurada: {config.actionName} en ubicación: {config.latitude}, {config.longitude}");
        }
        else
        {
            Debug.LogWarning($"No se encontró configuración de ubicación para la acción: {actionId}");
        }
    }

    // Configurar manualmente una ubicación objetivo
    public void SetLocationTarget(float latitude, float longitude, float radius)
    {
        // Accedemos a las variables privadas usando reflection
        var latField = locationValidator.GetType().GetField("targetLatitude",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var lonField = locationValidator.GetType().GetField("targetLongitude",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var radiusField = locationValidator.GetType().GetField("acceptableDistanceInMeters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (latField != null && lonField != null && radiusField != null)
        {
            latField.SetValue(locationValidator, latitude);
            lonField.SetValue(locationValidator, longitude);
            radiusField.SetValue(locationValidator, radius);

            Debug.Log($"Ubicación objetivo configurada: {latitude}, {longitude}, radio: {radius}m");
        }
        else
        {
            Debug.LogError("No se pudieron configurar los campos de ubicación objetivo");
        }
    }

    // Editor helper para mostrar ubicaciones en el inspector
#if UNITY_EDITOR
    [ContextMenu("Añadir ubicación de ejemplo")]
    private void AddExampleLocation()
    {
        locationConfigs.Add(new LocationConfig
        {
            actionId = "action_" + locationConfigs.Count,
            actionName = "Acción Eco " + locationConfigs.Count,
            latitude = 48.8476855f,  // París como ejemplo
            longitude = 2.3872314f,
            radiusInMeters = 2000f
        });
    }
#endif
}