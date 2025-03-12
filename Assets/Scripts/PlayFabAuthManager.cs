using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class PlayFabAuthManager : MonoBehaviour
{
    [SerializeField] TMP_InputField SignUpEmailField;
    [SerializeField] TMP_InputField SignUpPasswordField;
    [SerializeField] TMP_InputField SignUpConfirmPasswordField;

    [SerializeField] TMP_InputField LogInEmailField;
    [SerializeField] TMP_InputField LogInPasswordField;

    [SerializeField] TMP_Text SignUpStatusText;
    [SerializeField] TMP_Text LogInStatusText;
    TMP_Text activeStatusText;

    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject signupPanel;

    private string titleId = "122337";

    // Static instance for easy access from other scripts
    public static PlayFabAuthManager Instance { get; private set; }

    // User data
    public int TotalPoints { get; private set; }
    public int Level { get; private set; }
    public int CompletedChallenges { get; private set; }
    public List<string> Badges { get; private set; }
    public DateTime CreatedAt { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Badges = new List<string>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayFabSettings.TitleId = titleId;
        ShowLoginPanel();
    }

    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        activeStatusText = LogInStatusText;
    }

    public void ShowSignupPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        activeStatusText = SignUpStatusText;
    }

    public void RegisterButton()
    {
        if (SignUpPasswordField.text != SignUpConfirmPasswordField.text)
        {
            activeStatusText.text = "Les mots de passe ne correspondent pas!";
            return;
        }

        var registerRequest = new RegisterPlayFabUserRequest
        {
            Email = SignUpEmailField.text,
            Password = SignUpPasswordField.text,
            RequireBothUsernameAndEmail = false
        };

        activeStatusText.text = "Création du compte...";
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, OnRegisterSuccess, OnError);
    }

    public void LoginButton()
    {
        var loginRequest = new LoginWithEmailAddressRequest
        {
            Email = LogInEmailField.text,
            Password = LogInPasswordField.text
        };

        activeStatusText.text = "Connexion en cours...";
        PlayFabClientAPI.LoginWithEmailAddress(loginRequest, OnLoginSuccess, OnError);
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        activeStatusText.text = "Compte créé avec succès!";
        InitializeUserData();
    }

    void InitializeUserData()
    {
        // Reset local data
        TotalPoints = 0;
        Level = 1;
        CompletedChallenges = 0;
        Badges = new List<string>();
        CreatedAt = DateTime.UtcNow;

        // Create an empty JSON array for badges (simple approach)
        string badgesJson = "[]";

        var userDataRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {"TotalPoints", "0"},
                {"Level", "1"},
                {"CompletedChallenges", "0"},
                {"Badges", badgesJson},
                {"CreatedAt", CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")}
            }
        };

        PlayFabClientAPI.UpdateUserData(userDataRequest, OnUserDataInitialized, OnError);
    }

    void OnUserDataInitialized(UpdateUserDataResult result)
    {
        Debug.Log("Profil utilisateur initialisé");
        ShowLoginPanel();
    }

    void OnLoginSuccess(LoginResult result)
    {
        activeStatusText.text = "Connexion réussie!";
        GetUserData();
    }

    void GetUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnUserDataReceived, OnError);
    }

    void OnUserDataReceived(GetUserDataResult result)
    {
        // Load user profile data
        if (result.Data != null)
        {
            if (result.Data.ContainsKey("TotalPoints"))
            {
                TotalPoints = int.Parse(result.Data["TotalPoints"].Value);
            }

            if (result.Data.ContainsKey("Level"))
            {
                Level = int.Parse(result.Data["Level"].Value);
            }

            if (result.Data.ContainsKey("CompletedChallenges"))
            {
                CompletedChallenges = int.Parse(result.Data["CompletedChallenges"].Value);
            }

            if (result.Data.ContainsKey("Badges"))
            {
                // Parse badges from JSON string array
                string badgesJson = result.Data["Badges"].Value;
                Badges = ParseStringArray(badgesJson);
            }

            if (result.Data.ContainsKey("CreatedAt"))
            {
                CreatedAt = DateTime.Parse(result.Data["CreatedAt"].Value);
            }

            Debug.Log($"User data loaded - Points: {TotalPoints}, Level: {Level}, Badges: {Badges.Count}");
        }

        SceneManager.LoadScene("MainScene");
    }

    public void AddPoints(int pointsToAdd)
    {
        TotalPoints += pointsToAdd;

        // Check for level up and badge unlocks
        CheckForBadges();

        // Update PlayFab with new values
        UpdateUserDataInPlayFab();

        Debug.Log($"Points added: {pointsToAdd}, New total: {TotalPoints}");
    }

    public void CompleteChallenge()
    {
        CompletedChallenges++;
        UpdateUserDataInPlayFab();
    }

    public void AddBadge(string badgeName)
    {
        if (!Badges.Contains(badgeName))
        {
            Badges.Add(badgeName);
            Debug.Log($"New badge earned: {badgeName}");
            UpdateUserDataInPlayFab();
        }
    }

    private void CheckForBadges()
    {
        // Define badge thresholds
        if (TotalPoints >= 5 && !Badges.Contains("Débutant"))
        {
            AddBadge("Débutant");
        }

        if (TotalPoints >= 20 && !Badges.Contains("Apprenti"))
        {
            AddBadge("Apprenti");
        }

        if (TotalPoints >= 50 && !Badges.Contains("Écologiste"))
        {
            AddBadge("Écologiste");
        }

        if (TotalPoints >= 100 && !Badges.Contains("Champion de la Terre"))
        {
            AddBadge("Champion de la Terre");
        }

        // Calculate level based on points (example: 10 points per level)
        int newLevel = (TotalPoints / 10) + 1;
        if (newLevel > Level)
        {
            Level = newLevel;
            Debug.Log($"Level up! New level: {Level}");
        }
    }

    // Serializable class for badges
    [System.Serializable]
    private class BadgesData
    {
        public List<string> badges = new List<string>();
    }

    // Helper method to parse a JSON array of strings
    private List<string> ParseStringArray(string json)
    {
        List<string> result = new List<string>();

        // Return empty list for empty arrays
        if (json == "[]" || string.IsNullOrEmpty(json))
            return result;

        // Simple string parsing for non-nested JSON arrays
        if (json.StartsWith("[") && json.EndsWith("]"))
        {
            // Remove brackets
            string content = json.Substring(1, json.Length - 2);

            // Handle empty array
            if (string.IsNullOrWhiteSpace(content))
                return result;

            // Split by commas, but handle quoted strings properly
            bool inQuotes = false;
            int startPos = 0;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                // Toggle quote state
                if (c == '\"')
                    inQuotes = !inQuotes;

                // Process comma if not in quotes
                if (c == ',' && !inQuotes)
                {
                    string item = content.Substring(startPos, i - startPos).Trim();
                    // Remove surrounding quotes
                    if (item.StartsWith("\"") && item.EndsWith("\""))
                        item = item.Substring(1, item.Length - 2);

                    result.Add(item);
                    startPos = i + 1;
                }
            }

            // Add the last item
            if (startPos < content.Length)
            {
                string item = content.Substring(startPos).Trim();
                // Remove surrounding quotes
                if (item.StartsWith("\"") && item.EndsWith("\""))
                    item = item.Substring(1, item.Length - 2);

                result.Add(item);
            }
        }

        return result;
    }

    private void UpdateUserDataInPlayFab()
    {
        // Create a simple JSON array of strings for badges
        // This is a manual approach since JsonUtility doesn't handle lists directly
        string badgesJson = "[";
        for (int i = 0; i < Badges.Count; i++)
        {
            badgesJson += "\"" + Badges[i] + "\"";
            if (i < Badges.Count - 1)
                badgesJson += ",";
        }
        badgesJson += "]";

        var updateRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {"TotalPoints", TotalPoints.ToString()},
                {"Level", Level.ToString()},
                {"CompletedChallenges", CompletedChallenges.ToString()},
                {"Badges", badgesJson}
            }
        };

        PlayFabClientAPI.UpdateUserData(updateRequest,
            result => { Debug.Log("User data updated in PlayFab"); },
            error => { Debug.LogError("Failed to update user data: " + error.ErrorMessage); }
        );
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());

        string errorMessage = "Erreur: ";
        if (error.Error == PlayFabErrorCode.EmailAddressNotAvailable)
        {
            errorMessage += "Cet email est déjà utilisé";
        }
        else if (error.Error == PlayFabErrorCode.InvalidEmailAddress)
        {
            errorMessage += "Email invalide";
        }
        else if (error.Error == PlayFabErrorCode.InvalidPassword)
        {
            errorMessage += "Mot de passe invalide";
        }
        else if (error.Error == PlayFabErrorCode.InvalidEmailOrPassword)
        {
            errorMessage += "Email ou mot de passe incorrect";
        }
        else
        {
            errorMessage += error.ErrorMessage;
        }

        activeStatusText.text = errorMessage;
    }
}