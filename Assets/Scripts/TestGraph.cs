using UnityEngine;

public class TestGraph : MonoBehaviour
{
    [SerializeField] private ImpactChart chart;

    // A�ade este m�todo para pruebas
    public void GenerateTestData()
    {
        if (chart == null) return;

        // Generar datos de prueba para los �ltimos 10 d�as
        for (int i = 0; i < 10; i++)
        {
            float randomCO2 = Random.Range(0.5f, 5f);
            float randomWater = Random.Range(10f, 50f);
            chart.AddDailyData(randomCO2, randomWater);
        }
    }

    // Para probar desde el Inspector
    [ContextMenu("Generate Test Data")]
    private void TestDataFromInspector()
    {
        GenerateTestData();
    }
}