using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoActionsChallenges : MonoBehaviour
{
    // Références aux éléments UI
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image challengeImage;
    public string actionID;

    // Fonction pour mettre à jour l'UI avec une action
    private void UpdateUI(string title, string description, string imageName)
    {
        titleText.text = title;
        descriptionText.text = description;
        challengeImage.sprite = Resources.Load<Sprite>("Images/" + imageName);
    }

    // Actions écoresponsables
    public void Action_RamasserDechet()
    {
        UpdateUI("Ramasser un déchet ",
                 "Prends une photo avant/après d’un déchet que tu as ramassé et mis dans la poubelle.",
                 "dechet");
        actionID = "ramasserDechet";
    }

    public void Action_ApporterSac()
    {
        UpdateUI("Apporter son propre sac ",
                 "Montre-toi en train d’utiliser un sac réutilisable pour faire tes courses.",
                 "sac_reutilisable");

        actionID = "ApporterSac";
    }

    public void Action_EteindreLumieres()
    {
        UpdateUI("Éteindre les lumières ",
                 "Prends une photo avant/après d’une pièce avec la lumière allumée puis éteinte.",
                 "lumiere");

        actionID = "EteindreLumiere";
    }

    public void Action_UtiliserGourde()
    {
        UpdateUI("Utiliser une gourde ",
                 "Montre ta gourde réutilisable remplie au lieu d'une bouteille en plastique.",
                 "gourde");

        actionID = "UtiliserGourde";
    }

    public void Action_PrendreTransport()
    {
        UpdateUI("Prendre les transports en commun ou le vélo",
                 "Fais une photo de toi dans un bus, un métro, ou sur un vélo au lieu de la voiture.",
                 "velo");

        actionID = "PrendreTransport";
    }

    public void Action_Recycler()
    {
        UpdateUI("Recycler correctement ",
                 "Prends une photo en train de jeter un déchet dans la bonne poubelle de tri.",
                 "recyclage");

        actionID = "Recycler";
    }

    // Challenges
    public void Challenge_ZeroPlastique()
    {
        UpdateUI("Journée zéro plastique ",
                 "Prends une photo de tous les objets réutilisables que tu as utilisés au lieu de plastique jetable.",
                 "zero_plastique");
    }

    public void Challenge_NettoyageCollectif()
    {
        UpdateUI("Nettoyage collectif ",
                 "Fais une photo avec un groupe d’amis en train de nettoyer un parc, une plage ou une rue.",
                 "nettoyage");
    }

    public void Challenge_ObjetRecyclé()
    {
        UpdateUI("Créer un objet recyclé ",
                 "Prends une photo avant/après d’un objet que tu as transformé à partir de matériaux recyclés.",
                 "objet_recycle");
    }

    public void Challenge_RepasVegetarien()
    {
        UpdateUI("Un repas 100% végétarien ",
                 "Prends une photo de ton assiette avec un repas végétarien préparé par toi-même.",
                 "repas_vege");
    }
}
