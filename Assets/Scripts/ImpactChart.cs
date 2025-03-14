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
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text xAxisLabel;
    [SerializeField] private TMP_Text yAxisLabel;
    [SerializeField] private GameObject yLabelPrefab;
    [SerializeField] private GameObject xLabelPrefab;

    [Header("Configuración")]
    [SerializeField] private Color co2LineColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color waterLineColor = new Color(0.2f, 0.4f, 0.8f);
    [SerializeField] private int maxDataPoints = 30; // Número máximo de días a mostrar
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

    // Lista de datos diarios para gráfica
    private List<DailyData> dailyData = new List<DailyData>();

    // Llaves para almacenamiento en PlayFab
    private const string DAILY_DATA_KEY = "DailyImpactData";

    // Referencias de objetos gráficos
    private List<GameObject> graphElements = new List<GameObject>();

    private void Start()
    {
        // Cargar datos históricos
        LoadDailyData();

        // Mostrar la gráfica inicial
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

    // Método para agregar datos diarios (llamar al final del día o cuando sea necesario)
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

        // Guardar y actualizar gráfica
        SaveDailyData();
        ShowGraph();
    }

    // Mostrar la gráfica con los datos actuales
    private void ShowGraph()
    {
        // Limpiar elementos gráficos existentes
        ClearGraph();

        if (dailyData.Count < 2)
        {
            Debug.Log("Se necesitan al menos 2 puntos de datos para mostrar la gráfica");
            return;
        }

        // Establecer título y etiquetas
        if (titleText != null) titleText.text = "Impacto Ambiental";
        if (xAxisLabel != null) xAxisLabel.text = "Days";
        if (yAxisLabel != null) yAxisLabel.text = showWater && showCO2 ? "Kg / Litres" : showCO2 ? "Kg CO2" : "Litres";

        // Encontrar valores máximos para escala
        float maxValue = 0;
        if (showCO2) maxValue = Mathf.Max(maxValue, dailyData.Max(d => d.co2Value));
        if (showWater) maxValue = Mathf.Max(maxValue, dailyData.Max(d => d.waterValue));

        // Asegurar un valor mínimo para evitar división por cero
        maxValue = Mathf.Max(maxValue, 1f);

        // Calcular dimensiones del contenedor
        float graphHeight = graphContainer.rect.height;
        float graphWidth = graphContainer.rect.width;

        // Añadir etiquetas del eje Y (escala)
        CreateYLabels(maxValue, graphHeight);

        // Añadir etiquetas del eje X (fechas)
        CreateXLabels(graphWidth);

        // Dibujar líneas de datos
        if (showCO2) CreateLine(dailyData.Select(d => d.co2Value).ToList(), maxValue, co2LineColor);
        if (showWater) CreateLine(dailyData.Select(d => d.waterValue).ToList(), maxValue, waterLineColor);
    }

    private void CreateYLabels(float maxValue, float graphHeight)
    {
        // Crear 5 etiquetas en el eje Y
        int labelCount = 5;
        for (int i = 0; i <= labelCount; i++)
        {
            if (yLabelPrefab != null)
            {
                float normalizedValue = (float)i / labelCount;
                GameObject labelObj = Instantiate(yLabelPrefab, graphContainer);
                graphElements.Add(labelObj);

                RectTransform labelRT = labelObj.GetComponent<RectTransform>();
                labelRT.anchoredPosition = new Vector2(-300f, normalizedValue * graphHeight);

                TMP_Text labelText = labelObj.GetComponent<TMP_Text>();
                if (labelText != null)
                {
                    labelText.text = Mathf.RoundToInt(maxValue * normalizedValue).ToString();
                }
            }
        }
    }

    private void CreateXLabels(float graphWidth)
    {
        if (xLabelPrefab == null || dailyData.Count == 0) return;

        // Mostrar etiquetas de fecha en el eje X (reducidas para no saturar)
        int step = Mathf.Max(1, dailyData.Count / 5);
        for (int i = 0; i < dailyData.Count; i += step)
        {
            GameObject labelObj = Instantiate(xLabelPrefab, graphContainer);
            graphElements.Add(labelObj);

            float normalizedX = (float)i / (dailyData.Count - 1);
            RectTransform labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(normalizedX * graphWidth, -300f);

            TMP_Text labelText = labelObj.GetComponent<TMP_Text>();
            if (labelText != null)
            {
                labelText.text = dailyData[i].date.ToString("dd/MM");
            }
        }
    }

    private void CreateLine(List<float> values, float maxValue, Color lineColor)
    {
        if (values.Count < 2 || linePrefab == null || pointPrefab == null) return;

        float graphHeight = graphContainer.rect.height;
        float graphWidth = graphContainer.rect.width;

        // Crear puntos y conectarlos con líneas
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < values.Count; i++)
        {
            // Normalizar posición
            float xPosition = ((float)i / (values.Count - 1)) * graphWidth;
            float yPosition = (values[i] / maxValue) * graphHeight;

            // Añadir punto
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

        // Crear líneas entre puntos
        for (int i = 0; i < points.Count - 1; i++)
        {
            // Crear línea entre puntos
            GameObject lineObj = Instantiate(linePrefab, graphContainer);
            graphElements.Add(lineObj);
            RectTransform lineRT = lineObj.GetComponent<RectTransform>();

            // Posicionar y rotar línea
            Vector2 startPoint = points[i];
            Vector2 endPoint = points[i + 1];
            Vector2 direction = (endPoint - startPoint).normalized;
            float distance = Vector2.Distance(startPoint, endPoint);

            lineRT.anchoredPosition = startPoint;
            lineRT.sizeDelta = new Vector2(distance, 2f); // Grosor de línea = 2
            lineRT.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            // Colorear línea
            if (lineObj.TryGetComponent<Image>(out var image))
            {
                image.color = lineColor;
            }
        }
    }

    private void ClearGraph()
    {
        foreach (GameObject element in graphElements)
        {
            Destroy(element);
        }
        graphElements.Clear();
    }

    // Métodos para guardar y cargar datos de PlayFab

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

    // Método para pruebas - simula datos de varios días
    public void GenerateTestData()
    {
        // Limpiar datos existentes
        dailyData.Clear();

        // Generar datos para los últimos 30 días
        DateTime startDate = DateTime.Now.AddDays(-30);

        for (int i = 0; i < 30; i++)
        {
            // Crear tendencia ascendente con algo de variación aleatoria
            float co2Value = i * 0.5f + UnityEngine.Random.Range(-0.3f, 0.3f);
            float waterValue = i * 3.0f + UnityEngine.Random.Range(-2f, 2f);

            // Asegurar valores positivos
            co2Value = Mathf.Max(0.1f, co2Value);
            waterValue = Mathf.Max(1f, waterValue);

            // Añadir a la lista
            dailyData.Add(new DailyData
            {
                date = startDate.AddDays(i),
                co2Value = co2Value,
                waterValue = waterValue
            });
        }

        // Mostrar gráfica actualizada
        SaveDailyData();
        ShowGraph();
    }

    // Clase auxiliar para serialización de listas
    [Serializable]
    private class SerializableList<T>
    {
        public List<T> items;
    }
}