using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class PlayFabAuthManager : MonoBehaviour
{
    [SerializeField] TMP_InputField SignUpPseudo;
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

    public static PlayFabAuthManager Instance { get; private set; }

    // User data
    public string Username { get; private set; }
    public int TotalPoints { get; private set; }
    public int Level { get; private set; }
    public int CompletedChallenges { get; private set; }
    public List<string> Badges { get; private set; }
    public DateTime CreatedAt { get; private set; }

    void Awake()
    {
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

    /// <summary>
    /// Updates the user's username both locally and in PlayFab
    /// </summary>
    /// <param name="newUsername">The new username to set</param>
    public void UpdateUsername(string newUsername)
    {
        Username = newUsername;

        var updateRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {"Username", Username}
            }
        };

        PlayFabClientAPI.UpdateUserData(updateRequest,
            result => { Debug.Log("Username updated in PlayFab user data"); },
            error => { Debug.LogError("Failed to update username in user data: " + error.ErrorMessage); }
        );
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
            RequireBothUsernameAndEmail = false,
            DisplayName = SignUpPseudo.text 
        };

        activeStatusText.text = "Création du compte...";
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, OnRegisterSuccess, OnError);
    }

    public void LoginButton()
    {
        var loginRequest = new LoginWithEmailAddressRequest
        {
            Email = LogInEmailField.text,
            Password = LogInPasswordField.text,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        activeStatusText.text = "Connexion en cours...";
        PlayFabClientAPI.LoginWithEmailAddress(loginRequest, OnLoginSuccess, OnError);
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        activeStatusText.text = "Compte créé avec succès!";
        Username = SignUpPseudo.text;
        InitializeUserData();
    }

    void InitializeUserData()
    {
        TotalPoints = 0;
        Level = 1;
        CompletedChallenges = 0;
        Badges = new List<string>();
        CreatedAt = DateTime.UtcNow;

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

        if (result.InfoResultPayload != null &&
            result.InfoResultPayload.PlayerProfile != null &&
            !string.IsNullOrEmpty(result.InfoResultPayload.PlayerProfile.DisplayName))
        {
            Username = result.InfoResultPayload.PlayerProfile.DisplayName;
            Debug.Log($"Logged in as: {Username}");
        }

        GetUserData();
    }

    void GetUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnUserDataReceived, OnError);
    }

    void OnUserDataReceived(GetUserDataResult result)
    {
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

        CheckForBadges();

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

    public void CheckForBadges()
    {
        if (TotalPoints >= 2 && !Badges.Contains("Débutant"))
        {
            AddBadge("Débutant");
        }

        if (TotalPoints >= 5 && !Badges.Contains("Apprenti"))
        {
            AddBadge("Apprenti");
        }

        if (TotalPoints >= 8 && !Badges.Contains("Écologiste"))
        {
            AddBadge("Écologiste");
        }

        if (TotalPoints >= 10 && !Badges.Contains("Éco-innovateur"))
        {
            AddBadge("Éco-innovateur");
        }

        if (TotalPoints >= 15 && !Badges.Contains("Défenseur"))
        {
            AddBadge("Défenseur");
        }

        if (TotalPoints >= 20 && !Badges.Contains("Champion de la Terre"))
        {
            AddBadge("Champion de la Terre");
        }

        int newLevel = (TotalPoints / 10) + 1;
        if (newLevel > Level)
        {
            Level = newLevel;
            Debug.Log($"Level up! New level: {Level}");
        }
    }

    [System.Serializable]
    private class BadgesData
    {
        public List<string> badges = new List<string>();
    }

    private List<string> ParseStringArray(string json)
    {
        List<string> result = new List<string>();

        if (json == "[]" || string.IsNullOrEmpty(json))
            return result;

        if (json.StartsWith("[") && json.EndsWith("]"))
        {
            string content = json.Substring(1, json.Length - 2);

            if (string.IsNullOrWhiteSpace(content))
                return result;

            bool inQuotes = false;
            int startPos = 0;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                if (c == '\"')
                    inQuotes = !inQuotes;

                if (c == ',' && !inQuotes)
                {
                    string item = content.Substring(startPos, i - startPos).Trim();
                    if (item.StartsWith("\"") && item.EndsWith("\""))
                        item = item.Substring(1, item.Length - 2);

                    result.Add(item);
                    startPos = i + 1;
                }
            }

            if (startPos < content.Length)
            {
                string item = content.Substring(startPos).Trim();
                if (item.StartsWith("\"") && item.EndsWith("\""))
                    item = item.Substring(1, item.Length - 2);

                result.Add(item);
            }
        }

        return result;
    }

    private void UpdateUserDataInPlayFab()
    {
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