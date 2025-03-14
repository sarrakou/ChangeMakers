using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class ActionLocation
{
    public string actionId;
    public string actionName;
    public float latitude;
    public float longitude;
    public float radiusInMeters = 2000f; 
    public Button actionButton;
}

public class LocationConfigManager : MonoBehaviour
{
    [SerializeField] private List<ActionLocation> actionLocations = new List<ActionLocation>();
    [SerializeField] private LocationValidator locationValidator;
    [SerializeField] private CameraCapture cameraCapture;
    [SerializeField] private TMP_Text debugText;

    private void Start()
    {
        if (locationValidator == null || cameraCapture == null)
        {
            Debug.LogError("Referencias faltantes en ActionLocationManager");
            return;
        }

        foreach (var action in actionLocations)
        {
            if (action.actionButton != null)
            {                
                action.actionButton.onClick.RemoveAllListeners();
                
                action.actionButton.onClick.AddListener(() => SelectAction(action.actionId));
            }
        }
    }

    public void SelectAction(string actionId)
    {
        ActionLocation selectedAction = actionLocations.Find(a => a.actionId == actionId);

        if (selectedAction != null)
        {
            
            SetLocationValidatorTarget(selectedAction.latitude, selectedAction.longitude, selectedAction.radiusInMeters);

            
            cameraCapture.SetActionDetails(actionId, selectedAction.actionName);

            
            Debug.Log($"Acción seleccionada: {selectedAction.actionName} en {selectedAction.latitude}, {selectedAction.longitude}, radio: {selectedAction.radiusInMeters}m");

            if (debugText != null)
            {
                debugText.text = $"Acción: {selectedAction.actionName}\nUbicación: {selectedAction.latitude}, {selectedAction.longitude}\nRadio: {selectedAction.radiusInMeters}m";
            }
        }
        else
        {
            Debug.LogWarning($"No se encontró la acción con ID: {actionId}");
        }
    }

    // establecer ubicacion en el validador
    private void SetLocationValidatorTarget(float latitude, float longitude, float radius)
    {
        
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
        }
        else
        {
            
            locationValidator.SetTargetLocation(latitude, longitude, radius);
        }
    }
}

   