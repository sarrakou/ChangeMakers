using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

        activeStatusText.text = "test";
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
        var userDataRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {"TotalPoints", "0"},
                {"Level", "1"},
                {"CompletedChallenges", "0"},
                {"Badges", "[]"},
                {"CreatedAt", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
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

        SceneManager.LoadScene("MainScene");

        GetUserData();
    }

    void GetUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnUserDataReceived, OnError);
    }

    void OnUserDataReceived(GetUserDataResult result)
    {

        if (result.Data.ContainsKey("TotalPoints"))
        {
            int points = int.Parse(result.Data["TotalPoints"].Value);

            //UserDataManager.Instance.TotalPoints = points;
        }

         SceneManager.LoadScene("MainScene");
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