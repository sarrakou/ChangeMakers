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

    private const string CO2_KEY = "CO2Saved";
    private const string WATER_KEY = "WaterSaved";

    private float co2Value = 0;
    private float waterValue = 0;
    [SerializeField] private ImpactChart chart;
    [SerializeField] private ImpactChart chart2;

    public void AddCO2(float amount)
    {
        co2Value += amount;
        chart2.AddDailyData(amount, 0);
        UpdateCO2Display();
        SaveValues();
        
    }

    public void AddWater(float amount)
    {
        waterValue += amount;
        chart.AddDailyData(0, amount);
        UpdateWaterDisplay();
        SaveValues();
        
    }
    private void Start()
    {
        LoadValues();
    }
    private void UpdateCO2Display()
    {
        if (co2Text != null)
        {
            co2Text.text = co2Value.ToString();
        }
    }
    private void UpdateWaterDisplay()
    {
        if (waterText != null)
        {
            waterText.text = waterValue.ToString();
        }
    }
    private void LoadValues()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result => {
                if (result.Data != null)
                {
                    if (result.Data.ContainsKey(CO2_KEY))
                    {
                        float.TryParse(result.Data[CO2_KEY].Value, out co2Value);
                    }

                    if (result.Data.ContainsKey(WATER_KEY))
                    {
                        float.TryParse(result.Data[WATER_KEY].Value, out waterValue);
                    }

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