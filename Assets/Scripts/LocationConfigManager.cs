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

    // M�todo para seleccionar una acci�n y configurar su ubicaci�n
    public void SelectAction(string actionId)
    {
        // Buscar la configuraci�n de ubicaci�n para esta acci�n
        LocationConfig config = locationConfigs.Find(c => c.actionId == actionId);

        if (config != null)
        {
            // Configurar el validador de ubicaci�n
            SetLocationTarget(config.latitude, config.longitude, config.radiusInMeters);

            // Configurar la descripci�n de la acci�n
            cameraCapture.SetActionDetails(actionId, config.actionName);

            Debug.Log($"Acci�n configurada: {config.actionName} en ubicaci�n: {config.latitude}, {config.longitude}");
        }
        else
        {
            Debug.LogWarning($"No se encontr� configuraci�n de ubicaci�n para la acci�n: {actionId}");
        }
    }

    // Configurar manualmente una ubicaci�n objetivo
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

            Debug.Log($"Ubicaci�n objetivo configurada: {latitude}, {longitude}, radio: {radius}m");
        }
        else
        {
            Debug.LogError("No se pudieron configurar los campos de ubicaci�n objetivo");
        }
    }

    // Editor helper para mostrar ubicaciones en el inspector
#if UNITY_EDITOR
    [ContextMenu("A�adir ubicaci�n de ejemplo")]
    private void AddExampleLocation()
    {
        locationConfigs.Add(new LocationConfig
        {
            actionId = "action_" + locationConfigs.Count,
            actionName = "Acci�n Eco " + locationConfigs.Count,
            latitude = 48.8476855f,  // Par�s como ejemplo
            longitude = 2.3872314f,
            radiusInMeters = 2000f
        });
    }
#endif
}