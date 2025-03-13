using UnityEngine;
using UnityEngine.UI;
using System.IO;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;
using Unity.Notifications.Android;

public class CameraCapture : MonoBehaviour
{
    [SerializeField] private Button captureButton;
    [SerializeField] private Image photoDisplayImage;
    [SerializeField] private Image SecondPhotoDisplayImage;
    [SerializeField] private TMP_Text actionDescriptionText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text badgesText;
    [SerializeField] private TMP_Text locationStatusText; 

  
    [SerializeField] private LocationValidator locationValidator;     

    [SerializeField]private InfoActionsChallenges infoActions;
    private string currentPhotoPath;
    private Texture2D currentPhotoTexture;

    [SerializeField] private int pointsPerAction = 1;

    void Start()
    {
        captureButton.onClick.AddListener(CaptureAction);

        LoadExistingPhoto();

        UpdateUI();

        // Verificar componente de validaci�n
        if (infoActions.requireLocationValidation && locationValidator == null)
        {
            Debug.LogError("LocationValidator no esta asignado pero se requiere validacion de ubicacion");
            locationStatusText.text = "Error: Validador de ubicaci�n no configurado";
                
        }
        
        
    }


    private void OnEnable()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (PlayFabAuthManager.Instance != null)
        {
            pointsText.text = PlayFabAuthManager.Instance.TotalPoints + " point(s)";
            levelText.text = "Niveau " + PlayFabAuthManager.Instance.Level;

            if (PlayFabAuthManager.Instance.Badges != null && PlayFabAuthManager.Instance.Badges.Count > 0)
            {
                badgesText.text = string.Join(", ", PlayFabAuthManager.Instance.Badges);
            }
            else
            {
                badgesText.text = "Aucun badge";
            }
        }
        else
        {
            Debug.LogWarning("PlayFabAuthManager instance not found!");
        }
    }

    public void SetActionDetails(string id, string description)
    {
        infoActions.actionID = id;
        actionDescriptionText.text = description;
        LoadExistingPhoto();
    }

    public void CaptureAction()
    {
        if (infoActions.requireLocationValidation && locationValidator != null)
        {
            if (!locationValidator.IsLocationValid())
            {
                // Mostrar mensaje de ubicaci�n inv�lida
                Debug.LogWarning("La ubicaci�n actual no es v�lida para esta acci�n");
                if (locationStatusText != null)
                {
                    locationStatusText.text = "�Ubicaci�n inv�lida! Debes estar en el lugar espec�fico.";
                }
                return; // No continuar con la captura
            }
        }

        // Check if NativeCamera is available
#if UNITY_ANDROID || UNITY_IOS
        PermissionStatus permissionStatus = (PermissionStatus)NativeCamera.CheckPermission(true);
        if (permissionStatus == PermissionStatus.Denied)
        {
            NativeCamera.RequestPermission(true);
            return;
        }

        // Open camera to take picture
        NativeCamera.TakePicture((path) => {
            // User canceled taking a picture
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Eco action capture canceled");
                return;
            }

            // Process and save the image
            ProcessActionPhoto(path);
        }, maxSize: 1024, saveAsJPEG: true);
#else
        Debug.Log("Camera capture simulated in Editor");
        SimulateCameraCapture();
#endif
    }

    private void SimulateCameraCapture()
    {
        Debug.Log("Eco action captured (simulated)");

        Texture2D mockTexture = new Texture2D(512, 512);
        Color[] colors = new Color[512 * 512];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color(
                Random.Range(0.0f, 1.0f),
                Random.Range(0.5f, 1.0f),
                Random.Range(0.0f, 0.5f)
            );
        }
        mockTexture.SetPixels(colors);
        mockTexture.Apply();

        currentPhotoTexture = mockTexture;

        Sprite photoSprite = Sprite.Create(
            currentPhotoTexture,
            new Rect(0, 0, currentPhotoTexture.width, currentPhotoTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        photoDisplayImage.sprite = photoSprite;
        SecondPhotoDisplayImage.sprite = photoSprite;

        string mockPath = Path.Combine(Application.persistentDataPath, "mock_eco_action_" + infoActions.actionID + ".jpg");
        File.WriteAllBytes(mockPath, currentPhotoTexture.EncodeToPNG());
        currentPhotoPath = mockPath;

        // Guardar tambi�n la informaci�n de ubicaci�n
        SaveLocationMetadata();

        AwardPointsForAction();
    }

    private void ProcessActionPhoto(string path)
    {
        currentPhotoTexture = NativeCamera.LoadImageAtPath(path, maxSize: 1024);
        if (currentPhotoTexture == null)
        {
            Debug.LogError("Couldn't load eco action photo from path: " + path);
            return;
        }

        Sprite photoSprite = Sprite.Create(
            currentPhotoTexture,
            new Rect(0, 0, currentPhotoTexture.width, currentPhotoTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        photoDisplayImage.sprite = photoSprite;
        SecondPhotoDisplayImage.sprite = photoSprite;

        SavePhotoLocally(path);

        // Guardar tambi�n la informaci�n de ubicaci�n
        SaveLocationMetadata();

        AwardPointsForAction();
    }

    private void SaveLocationMetadata()
    {
        if (locationValidator != null && locationValidator.IsLocationValid())
        {
            LocationInfo locationInfo = locationValidator.GetCurrentLocation();

            // Guardar metadatos de ubicaci�n
            string metadataJson = JsonUtility.ToJson(new PhotoLocationMetadata
            {
                latitude = locationInfo.latitude,
                longitude = locationInfo.longitude,
                accuracy = locationInfo.horizontalAccuracy,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                isValid = true
            });

            string metadataPath = Path.Combine(Application.persistentDataPath, "eco_action_" + infoActions.actionID + "_location.json");
            File.WriteAllText(metadataPath, metadataJson);

            Debug.Log("Metadatos de ubicaci�n guardados en: " + metadataPath);

            if (locationStatusText != null)
            {
                locationStatusText.text = "Ubicaci�n validada correctamente";
                locationStatusText.color = Color.green;
            }
        }
    }

    private void AwardPointsForAction()
    {
        bool alreadyCompleted = PlayerPrefs.HasKey("EcoAction_" + infoActions.actionID + "_Completed");

        if (!alreadyCompleted)
        {
            PlayerPrefs.SetInt("EcoAction_" + infoActions.actionID + "_Completed", 1);
            PlayerPrefs.Save();

            if (PlayFabAuthManager.Instance != null)
            {
                PlayFabAuthManager.Instance.AddPoints(pointsPerAction);
                PlayFabAuthManager.Instance.CompleteChallenge();

                UpdateUI();
            }
            else
            {
                Debug.LogError("PlayFabAuthManager instance not found, cannot award points!");
            }

            Debug.Log("Awarded " + pointsPerAction + " point(s) for eco action " + infoActions.actionID);
        }
        else
        {
            Debug.Log("This eco action was already completed before. No additional points awarded.");
        }
        UpdatePhotoInfoInPlayFab();
    }

    private void SavePhotoLocally(string originalPath)
    {
        string fileName = "eco_action_" + infoActions.actionID + ".jpg";
        string destinationPath = Path.Combine(Application.persistentDataPath, fileName);

        File.Copy(originalPath, destinationPath, true);
        currentPhotoPath = destinationPath;

        PlayerPrefs.SetString("EcoAction_" + infoActions.actionID + "_PhotoPath", currentPhotoPath);
        PlayerPrefs.Save();

        Debug.Log("Eco action photo saved locally at: " + currentPhotoPath);
    }

    private void LoadExistingPhoto()
    {
        string storedPath = PlayerPrefs.GetString("EcoAction_" + infoActions.actionID + "_PhotoPath", "");

        if (!string.IsNullOrEmpty(storedPath) && File.Exists(storedPath))
        {
            currentPhotoPath = storedPath;
            byte[] fileData = File.ReadAllBytes(storedPath);
            currentPhotoTexture = new Texture2D(2, 2);
            currentPhotoTexture.LoadImage(fileData);

            Sprite photoSprite = Sprite.Create(
                currentPhotoTexture,
                new Rect(0, 0, currentPhotoTexture.width, currentPhotoTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            photoDisplayImage.sprite = photoSprite;
            SecondPhotoDisplayImage.sprite = photoSprite;
        }
    }

    private void UpdatePhotoInfoInPlayFab()
    {
        System.DateTime captureTime = System.DateTime.UtcNow;
        string timestamp = captureTime.ToString("yyyy-MM-dd HH:mm:ss");

        // Incluir informaci�n de ubicaci�n si est� disponible
        bool locationValid = false;
        float latitude = 0;
        float longitude = 0;

        if (locationValidator != null)
        {
            locationValid = locationValidator.IsLocationValid();
            LocationInfo locationInfo = locationValidator.GetCurrentLocation();
            latitude = locationInfo.latitude;
            longitude = locationInfo.longitude;
        }

        var updateRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                
                {"EcoAction_" + infoActions.actionID + "_HasPhoto", "true"},
                {"EcoAction_" + infoActions.actionID + "_PhotoTimestamp", timestamp},
                {"EcoAction_" + infoActions.actionID + "_PhotoLocalPath", currentPhotoPath},
                
                {"EcoAction_" + infoActions.actionID + "_LocationValid", locationValid.ToString()},
                {"EcoAction_" + infoActions.actionID + "_Latitude", latitude.ToString()},
                {"EcoAction_" + infoActions.actionID + "_Longitude", longitude.ToString()},
                {"EcoAction_" + infoActions.actionID + "_LocationTimestamp", timestamp}

            }
        };

        PlayFabClientAPI.UpdateUserData(updateRequest,
        result => {
            Debug.Log("Eco action photo info updated in PlayFab with location data");
        },
        error => {
            Debug.LogError("Failed to update eco action photo info: " + error.ErrorMessage);
        });
    }

    // Method to sync all photos if the user reinstalls the app
    public void SyncPhotosFromPlayFab()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
        result => {
            foreach (var item in result.Data)
            {
                if (item.Key.StartsWith("EcoAction_") && item.Key.EndsWith("_HasPhoto"))
                {
                    string actionId = item.Key.Replace("EcoAction_", "").Replace("_HasPhoto", "");
                    Debug.Log("Found photo data for action: " + actionId);
                }
            }
        },
        error => {
            Debug.LogError("Failed to get user data: " + error.ErrorMessage);
        });
    }
}

// Clase para almacenar los metadatos de ubicaci�n de la foto
[System.Serializable]
public class PhotoLocationMetadata
{
    public float latitude;
    public float longitude;
    public float accuracy;
    public string timestamp;
    public bool isValid;
}