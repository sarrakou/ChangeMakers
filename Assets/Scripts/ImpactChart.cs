using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class ImpactChart : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private RectTransform graphContainer;
    [SerializeField] private GameObject pointPrefab;

    [Header("Configuraci�n")]
    [SerializeField] private Color co2LineColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color waterLineColor = new Color(0.2f, 0.4f, 0.8f);
    [SerializeField] private int maxDataPoints = 30; // N�mero m�ximo de d�as a mostrar
    [SerializeField] private bool showCO2 = true;
    [SerializeField] private bool showWater = true;

    // Estructura para almacenar los datos diarios
    [System.Serializable]
    public class DailyData
    {
        public DateTime date;
        public float co2Value;
        public float waterValue;
    }

    // Lista de datos diarios para gr�fica
    private List<DailyData> dailyData = new List<DailyData>();

    // Llaves para almacenamiento en PlayFab
    private const string DAILY_DATA_KEY = "DailyImpactData";

    // Referencias de objetos gr�ficos
    private List<GameObject> graphElements = new List<GameObject>();

    private void Start()
    {
        // Cargar datos hist�ricos
        LoadDailyData();

        // Mostrar la gr�fica inicial
        ShowGraph();
    }

    public void ToggleCO2Display(bool show)
    {
        showCO2 = show;
        ShowGraph();
    }

    public void ToggleWaterDisplay(bool show)
    {
        showWater = show;
        ShowGraph();
    }

    // M�todo para agregar datos diarios (llamar al final del d�a o cuando sea necesario)
    public void AddDailyData(float co2Added, float waterAdded)
    {
        // Buscar si ya existe un registro para hoy
        DateTime today = DateTime.Now.Date;
        DailyData todayData = dailyData.FirstOrDefault(d => d.date.Date == today);

        if (todayData != null)
        {
            // Actualizar datos existentes
            todayData.co2Value += co2Added;
            todayData.waterValue += waterAdded;
        }
        else
        {
            // Crear nuevo registro
            todayData = new DailyData
            {
                date = today,
                co2Value = co2Added,
                waterValue = waterAdded
            };
            dailyData.Add(todayData);

            // Limitar cantidad de puntos de datos
            if (dailyData.Count > maxDataPoints)
            {
                dailyData = dailyData.OrderByDescending(d => d.date).Take(maxDataPoints).ToList();
            }
        }

        // Ordenar por fecha
        dailyData = dailyData.OrderBy(d => d.date).ToList();

        // Guardar y actualizar gr�fica
        SaveDailyData();
        ShowGraph();
    }

    // Mostrar la gr�fica con los datos actuales
    private void ShowGraph()
    {
        // Limpiar elementos gr�ficos existentes
        ClearGraph();

        if (dailyData.Count < 2)
        {
            Debug.Log("Se necesitan al menos 2 puntos de datos para mostrar la gr�fica");
            return;
        }

        // Encontrar valores m�ximos para escala
        float maxValue = 0;
        if (showCO2) maxValue = Mathf.Max(maxValue, dailyData.Max(d => d.co2Value));
        if (showWater) maxValue = Mathf.Max(maxValue, dailyData.Max(d => d.waterValue));

        // Asegurar un valor m�nimo para evitar divisi�n por cero
        maxValue = Mathf.Max(maxValue, 1f);

        // Calcular dimensiones del contenedor
        float graphHeight = graphContainer.rect.height;
        float graphWidth = graphContainer.rect.width;


        // Dibujar l�neas de datos
        if (showCO2) CreateLine(dailyData.Select(d => d.co2Value).ToList(), maxValue, co2LineColor);
        if (showWater) CreateLine(dailyData.Select(d => d.waterValue).ToList(), maxValue, waterLineColor);
    }

    private void CreateLine(List<float> values, float maxValue, Color lineColor)
    {
        if (values.Count < 2 || pointPrefab == null) return;

        float graphHeight = graphContainer.rect.height;
        float graphWidth = graphContainer.rect.width;

        // Crear puntos
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < values.Count; i++)
        {
            // Normalizar posici�n
            float xPosition = ((float)i / (values.Count - 1)) * graphWidth;
            float yPosition = (values[i] / maxValue) * graphHeight;

            // A�adir punto
            Vector2 point = new Vector2(xPosition, yPosition);
            points.Add(point);

            // Crear visual del punto
            GameObject pointObj = Instantiate(pointPrefab, graphContainer);
            graphElements.Add(pointObj);
            RectTransform pointRT = pointObj.GetComponent<RectTransform>();
            pointRT.anchoredPosition = point;

            // Colorear punto
            if (pointObj.TryGetComponent<Image>(out var image))
            {
                image.color = lineColor;
            }
        }

    }

    public void ClearGraph()
    {
        foreach (GameObject element in graphElements)
        {
            Destroy(element);
        }
        graphElements.Clear();
    }

    // M�todos para guardar y cargar datos de PlayFab

    private void SaveDailyData()
    {
        try
        {
            // Serializar datos para almacenamiento
            string serializedData = JsonUtility.ToJson(new SerializableList<DailyData> { items = dailyData });

            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { DAILY_DATA_KEY, serializedData }
                }
            };

            PlayFabClientAPI.UpdateUserData(request,
                result => Debug.Log("Datos de impacto diario guardados correctamente"),
                error => Debug.LogError($"Error al guardar datos diarios: {error.ErrorMessage}")
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al serializar datos diarios: {e.Message}");
        }
    }

    private void LoadDailyData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result => {
                if (result.Data != null && result.Data.ContainsKey(DAILY_DATA_KEY))
                {
                    try
                    {
                        string serializedData = result.Data[DAILY_DATA_KEY].Value;
                        var data = JsonUtility.FromJson<SerializableList<DailyData>>(serializedData);
                        if (data != null && data.items != null)
                        {
                            dailyData = data.items;
                            ShowGraph();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error al deserializar datos diarios: {e.Message}");
                    }
                }
            },
            error => Debug.LogError($"Error al obtener datos diarios: {error.ErrorMessage}")
        );
    }

    // M�todo para pruebas - simula datos de varios d�as
    public void GenerateTestData()
    {
        // Limpiar datos existentes
        dailyData.Clear();

        // Generar datos para los �ltimos 30 d�as
        DateTime startDate = DateTime.Now.AddDays(-30);

        for (int i = 0; i < 30; i++)
        {
            // Crear tendencia ascendente con algo de variaci�n aleatoria
            float co2Value = i + UnityEngine.Random.Range(-2f, 2f);
            float waterValue = i + UnityEngine.Random.Range(-2f, 2f);

            // Asegurar valores positivos
            co2Value = Mathf.Max(0.1f, co2Value);
            waterValue = Mathf.Max(1f, waterValue);

            // A�adir a la lista
            dailyData.Add(new DailyData
            {
                date = startDate.AddDays(i),
                co2Value = co2Value,
                waterValue = waterValue
            });
        }

        // Mostrar gr�fica actualizada
        SaveDailyData();
        ShowGraph();
    }

    // Clase auxiliar para serializaci�n de listas
    [Serializable]
    private class SerializableList<T>
    {
        public List<T> items;
    }
}