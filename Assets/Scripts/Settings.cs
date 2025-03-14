using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Settings : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField emailInput;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text statusMessageText;

    [Header("Scene References")]
    [SerializeField] private string loginSceneName = "LoginScene";


    [SerializeField] private TMP_Text profileUsername;
    [SerializeField] private TMP_Text settingsUsername;

    private string originalUsername;
    private string originalEmail;

    private void Start()
    {
        LoadUserData();
    }

    private void LoadUserData()
    {
        if (PlayFabAuthManager.Instance != null && usernameInput != null)
        {
            originalUsername = PlayFabAuthManager.Instance.Username;
            usernameInput.text = originalUsername;
        }

        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
            result => {
                if (emailInput != null && result.AccountInfo != null &&
                    result.AccountInfo.PrivateInfo != null &&
                    !string.IsNullOrEmpty(result.AccountInfo.PrivateInfo.Email))
                {
                    originalEmail = result.AccountInfo.PrivateInfo.Email;
                    emailInput.text = originalEmail;
                }
            },
            error => {
                Debug.LogError("Failed to get account info: " + error.ErrorMessage);
                ShowStatusMessage("Erreur lors du chargement des informations de compte", true);
            }
        );
    }

    public void SaveChanges()
    {
        bool hasChanges = false;

        bool usernameChanged = usernameInput != null &&
                             !string.IsNullOrEmpty(usernameInput.text) &&
                             usernameInput.text != originalUsername;

        bool emailChanged = emailInput != null &&
                          !string.IsNullOrEmpty(emailInput.text) &&
                          emailInput.text != originalEmail;

        if (!usernameChanged && !emailChanged)
        {
            ShowStatusMessage("Aucun changement détecté", false);
            return;
        }

        if (usernameChanged)
        {
            UpdateUsername(usernameInput.text);
            hasChanges = true;
        }

        if (emailChanged)
        {
            UpdateEmail(emailInput.text);
            hasChanges = true;
        }

        if (hasChanges)
        {
            ShowStatusMessage("Enregistrement des modifications...", false);
        }
    }

    private void UpdateUsername(string newUsername)
    {
        var updateDisplayNameRequest = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = newUsername
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(updateDisplayNameRequest,
            result => {
                if (PlayFabAuthManager.Instance != null)
                {
                    PlayFabAuthManager.Instance.UpdateUsername(newUsername);
                }

                originalUsername = newUsername;
                profileUsername.text = newUsername;
                settingsUsername.text = newUsername;
                ShowStatusMessage("Pseudo mis à jour avec succès!", false);
            },
            error => {
                string errorMessage = "Erreur lors de la modification du pseudo: ";

                if (error.Error == PlayFabErrorCode.NameNotAvailable)
                {
                    errorMessage = "Ce pseudo est déjà utilisé par un autre joueur";
                }
                else if (error.Error == PlayFabErrorCode.InvalidDisplayNameRandomSuffixLength)
                {
                    errorMessage = "Pseudo invalide";
                }
                else
                {
                    errorMessage += error.ErrorMessage;
                }

                ShowStatusMessage(errorMessage, true);
            }
        );
    }

    private void UpdateEmail(string newEmail)
    {
        var addEmailRequest = new AddOrUpdateContactEmailRequest
        {
            EmailAddress = newEmail
        };

        PlayFabClientAPI.AddOrUpdateContactEmail(addEmailRequest,
            result => {
                originalEmail = newEmail;
                ShowStatusMessage("Email mis à jour avec succès!", false);
            },
            error => {
                string errorMessage = "Erreur lors de la modification de l'email: ";

                if (error.Error == PlayFabErrorCode.EmailAddressNotAvailable)
                {
                    errorMessage = "Cet email est déjà utilisé par un autre compte";
                }
                else if (error.Error == PlayFabErrorCode.InvalidEmailAddress)
                {
                    errorMessage = "L'email saisi est invalide";
                }
                else
                {
                    errorMessage += error.ErrorMessage;
                }

                ShowStatusMessage(errorMessage, true);
            }
        );
    }

    public void SendPasswordResetEmail()
    {
        string userEmail = emailInput.text;

        if (string.IsNullOrEmpty(userEmail))
        {
            ShowStatusMessage("Veuillez saisir votre adresse email", true);
            return;
        }

        var request = new SendAccountRecoveryEmailRequest
        {
            Email = userEmail,
            TitleId = PlayFabSettings.TitleId
        };

        PlayFabClientAPI.SendAccountRecoveryEmail(request,
            result => {
                ShowStatusMessage("Un email de réinitialisation de mot de passe a été envoyé", false);
            },
            error => {
                ShowStatusMessage("Erreur d'envoi d'email: " + error.ErrorMessage, true);
            }
        );
    }

    public void LogOut()
    {
        PlayFabClientAPI.ForgetAllCredentials();

        if (PlayFabAuthManager.Instance != null)
        {
            Destroy(PlayFabAuthManager.Instance.gameObject);
        }

        SceneManager.LoadScene(loginSceneName);
    }

    private void ShowStatusMessage(string message, bool isError)
    {
        if (statusMessageText == null) return;

        statusMessageText.text = message;
        statusMessageText.color = isError ? Color.red : Color.green;

        if (isError)
            Debug.LogWarning(message);
        else
            Debug.Log(message);
    }
}