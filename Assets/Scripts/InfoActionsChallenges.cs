using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoActionsChallenges : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText; 
    public TextMeshProUGUI titleText1;
    public TextMeshProUGUI descriptionText1;
    public Image challengeImage;
    public string actionID;
    public bool requireLocationValidation = false;

    private void UpdateUI(string title, string description, string imageName, string title1, string description1)
    {
        titleText.text = title;
        descriptionText.text = description;
        challengeImage.sprite = Resources.Load<Sprite>("Images/" + imageName); 
        titleText1.text = title1;
        descriptionText1.text = description1;
    }

    public void Action_RamasserDechet()
    {
        UpdateUI("Ramasser un déchet ",
                 "Prends une photo avant/après d’un déchet que tu as ramassé et mis dans la poubelle.",
                 "dechet",
                 "Tu as participé à l'action Ramasser un déchet ",
                 " ");
        actionID = "ramasserDechet";

        requireLocationValidation = false;
    }

    public void Action_ApporterSac()
    {
        UpdateUI("Apporter son propre sac ",
                 "Montre-toi en train d’utiliser un sac réutilisable pour faire tes courses.",
                 "sac_reutilisable",
                 "Tu as participé à l'action Apporter son propre sac ",
                 " ");

        actionID = "ApporterSac";
        requireLocationValidation = false;
    }

    public void Action_EteindreLumieres()
    {
        UpdateUI("Éteindre les lumières ",
                 "Prends une photo avant/après d’une pièce avec la lumière allumée puis éteinte.",
                 "lumiere",
                 "Tu as participé à l'action Éteindre les lumières ",
                 " ");

        actionID = "EteindreLumiere";
        requireLocationValidation = false;
    }

    public void Action_UtiliserGourde()
    {
        UpdateUI("Utiliser une gourde ",
                 "Montre ta gourde réutilisable remplie au lieu d'une bouteille en plastique.",
                 "gourde",
                 "Tu as participé à l'action Utiliser une gourde ",
                 " ");

        actionID = "UtiliserGourde";
        requireLocationValidation = false;
    }

    public void Action_PrendreTransport()
    {
        UpdateUI("Prendre les transports en commun ou le vélo",
                 "Fais une photo de toi dans un bus, un métro, ou sur un vélo au lieu de la voiture.",
                 "velo",
                 "Tu as participé à l'action Prendre les transports en commun ou le vélo ",
                 " ");

        actionID = "PrendreTransport";
        requireLocationValidation = false;
    }

    public void Action_Recycler()
    {
        UpdateUI("Recycler correctement ",
                 "Prends une photo en train de jeter un déchet dans la bonne poubelle de tri.",
                 "recyclage",
                 "Tu as participé à l'action Recycler correctement ",
                 " ");

        actionID = "Recycler";
        requireLocationValidation = false;
    }

    // Challenges
    public void Challenge_ZeroPlastique()
    {
        UpdateUI("Journée zéro plastique ",
                 "Prends une photo de tous les objets réutilisables que tu as utilisés au lieu de plastique jetable.",
                 "zero_plastique",
                 "Tu as participé au challenge Journée zéro plastique ",
                 " ");
        requireLocationValidation = true;
    }

    public void Challenge_NettoyageCollectif()
    {
        UpdateUI("Nettoyage collectif ",
                 "Fais une photo avec un groupe d’amis en train de nettoyer un parc à Paris .",
                 "nettoyage",
                 "Tu as participé au challenge Nettoyage collectif à Paris ",
                 " ");

        requireLocationValidation = true;
    }

    public void Challenge_ObjetRecyclé()
    {
        UpdateUI("Créer un objet recyclé ",
                 "Prends une photo avant/après d’un objet que tu as transformé à partir de matériaux recyclés.",
                 "objet_recycle",
                 "Tu as participé au challenge Créer un objet recyclé ",
                 " ");

        requireLocationValidation = true;
    }

    public void Challenge_RepasVegetarien()
    {
        UpdateUI("Un repas 100% végétarien ",
                 "Prends une photo de ton assiette avec un repas végétarien préparé par toi-même.",
                 "repas_vege",
                 "Tu as participé au challenge Un repas 100% végétarien ",
                 " ");

        requireLocationValidation = true;
    }
}
