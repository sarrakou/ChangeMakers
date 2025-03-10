using UnityEngine;
using UnityEngine.UI;
using System.IO;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Android;
using Unity.Notifications.Android;

public class CameraCapture : MonoBehaviour
{
    [SerializeField] private Button captureButton;
    [SerializeField] private Image photoDisplayImage;
    [SerializeField] private TMP_Text actionDescriptionText;

    private string actionId; // Unique ID for this eco-action
    private string currentPhotoPath; // Local path to the stored photo
    private Texture2D currentPhotoTexture;

    void Start()
    {
        captureButton.onClick.AddListener(CaptureAction);
        // Load existing photo if available
        LoadExistingPhoto();
    }

    public void SetActionDetails(string id, string description)
    {
        actionId = id;
        actionDescriptionText.text = description;
        LoadExistingPhoto();
    }

    private void CaptureAction()
    {
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
    }

    private void ProcessActionPhoto(string path)
    {
        // Load the image from the path
        currentPhotoTexture = NativeCamera.LoadImageAtPath(path, maxSize: 1024);
        if (currentPhotoTexture == null)
        {
            Debug.LogError("Couldn't load eco action photo from path: " + path);
            return;
        }

        // Display the image
        Sprite photoSprite = Sprite.Create(
            currentPhotoTexture,
            new Rect(0, 0, currentPhotoTexture.width, currentPhotoTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        photoDisplayImage.sprite = photoSprite;

        // Save locally with action ID in filename
        SavePhotoLocally(path);

        // Update PlayFab with photo information
        UpdatePhotoInfoInPlayFab();
    }

    private void SavePhotoLocally(string originalPath)
    {
        // Create a permanent local copy with the action ID in the filename
        string fileName = "eco_action_" + actionId + ".jpg";
        string destinationPath = Path.Combine(Application.persistentDataPath, fileName);

        // Copy the file
        File.Copy(originalPath, destinationPath, true);
        currentPhotoPath = destinationPath;

        // Store the path in PlayerPrefs for this action
        PlayerPrefs.SetString("EcoAction_" + actionId + "_PhotoPath", currentPhotoPath);
        PlayerPrefs.Save();

        Debug.Log("Eco action photo saved locally at: " + currentPhotoPath);
    }

    private void LoadExistingPhoto()
    {
        // Check if we have a stored path for this action
        string storedPath = PlayerPrefs.GetString("EcoAction_" + actionId + "_PhotoPath", "");

        if (!string.IsNullOrEmpty(storedPath) && File.Exists(storedPath))
        {
            // Load the existing photo
            currentPhotoPath = storedPath;
            byte[] fileData = File.ReadAllBytes(storedPath);
            currentPhotoTexture = new Texture2D(2, 2);
            currentPhotoTexture.LoadImage(fileData);

            // Create and display sprite
            Sprite photoSprite = Sprite.Create(
                currentPhotoTexture,
                new Rect(0, 0, currentPhotoTexture.width, currentPhotoTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            photoDisplayImage.sprite = photoSprite;
        }
    }

    private void UpdatePhotoInfoInPlayFab()
    {
        // Get the timestamp when the photo was taken
        System.DateTime captureTime = System.DateTime.UtcNow;
        string timestamp = captureTime.ToString("yyyy-MM-dd HH:mm:ss");

        // We'll store metadata about the photo instead of the photo itself
        var updateRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                // Store action metadata
                {"EcoAction_" + actionId + "_HasPhoto", "true"},
                {"EcoAction_" + actionId + "_PhotoTimestamp", timestamp},
                {"EcoAction_" + actionId + "_PhotoLocalPath", currentPhotoPath},
            }
        };

        PlayFabClientAPI.UpdateUserData(updateRequest,
        result => {
            Debug.Log("Eco action photo info updated in PlayFab");
        },
        error => {
            Debug.LogError("Failed to update eco action photo info: " + error.ErrorMessage);
        });
    }

    // You can add a method to sync all photos if the user reinstalls the app
    public void SyncPhotosFromPlayFab()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
        result => {
            // Look for all eco action photo data
            foreach (var item in result.Data)
            {
                if (item.Key.StartsWith("EcoAction_") && item.Key.EndsWith("_HasPhoto"))
                {
                    string actionId = item.Key.Replace("EcoAction_", "").Replace("_HasPhoto", "");
                    Debug.Log("Found photo data for action: " + actionId);

                    // You would implement logic here to handle downloaded photos
                    // This might involve downloading from your own server if needed
                }
            }
        },
        error => {
            Debug.LogError("Failed to get user data: " + error.ErrorMessage);
        });
    }
}