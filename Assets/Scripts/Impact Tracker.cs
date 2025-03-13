using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ImpactTracker : MonoBehaviour
{
    // UI References
    [SerializeField] private TMP_Text co2Text;
    [SerializeField] private TMP_Text waterText;

    // Storage keys
    private const string CO2_KEY = "CO2Saved";
    private const string WATER_KEY = "WaterSaved";

    // Current values
    private float co2Value = 0;
    private float waterValue = 0;

    private void Start()
    {
        // Load saved values when starting
        LoadValues();
    }

    // Call this to add CO2 saved (in kg)
    public void AddCO2(float amount)
    {
        co2Value += amount;
        UpdateCO2Display();
        SaveValues();
    }

    // Call this to add water saved (in liters)
    public void AddWater(float amount)
    {
        waterValue += amount;
        UpdateWaterDisplay();
        SaveValues();
    }

    // Updates the CO2 text display
    private void UpdateCO2Display()
    {
        if (co2Text != null)
        {
            co2Text.text = co2Value.ToString();
        }
    }

    // Updates the water text display
    private void UpdateWaterDisplay()
    {
        if (waterText != null)
        {
            waterText.text = waterValue.ToString();
        }
    }

    // Loads values from PlayFab
    private void LoadValues()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result => {
                if (result.Data != null)
                {
                    // Get CO2 value
                    if (result.Data.ContainsKey(CO2_KEY))
                    {
                        float.TryParse(result.Data[CO2_KEY].Value, out co2Value);
                    }

                    // Get water value
                    if (result.Data.ContainsKey(WATER_KEY))
                    {
                        float.TryParse(result.Data[WATER_KEY].Value, out waterValue);
                    }

                    // Update displays
                    UpdateCO2Display();
                    UpdateWaterDisplay();
                }
            },
            error => {
                Debug.LogError("Error getting user data: " + error.ErrorMessage);
            }
        );
    }

    // Saves values to PlayFab
    private void SaveValues()
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { CO2_KEY, co2Value.ToString() },
                { WATER_KEY, waterValue.ToString() }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => {
                Debug.Log("Impact values saved successfully");
            },
            error => {
                Debug.LogError("Error saving impact values: " + error.ErrorMessage);
            }
        );
    }
}