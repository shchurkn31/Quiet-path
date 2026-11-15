using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestItemUI : MonoBehaviour
{
    [Header("Текст")]
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI questProgressText;
    public TextMeshProUGUI questRewardText;
    public TextMeshProUGUI buttonText;

    [Header("Кнопка")]
    public Button actionButton;

    private void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveAllListeners();
    }

    public void SetupAvailable(
        QuestDefinition definition,
        Action onAccept)
    {
        questNameText.text = definition.questName;
        questDescriptionText.text = definition.description;
        questProgressText.text = "Ще не прийнято";
        questRewardText.text = "Нагорода: " + definition.rewardGold + " золота";

        buttonText.text = "Прийняти";

        actionButton.interactable = true;
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(() => onAccept?.Invoke());
    }

    public void SetupActive(
        QuestDefinition definition,
        ActiveQuest activeQuest,
        Action onClaim)
    {
        questNameText.text = definition.questName;
        questDescriptionText.text = definition.description;

        questProgressText.text =
            "Прогрес: " + activeQuest.currentKills +
            "/" + definition.requiredKills;

        questRewardText.text = "Нагорода: " + definition.rewardGold + " золота";

        actionButton.onClick.RemoveAllListeners();

        if (activeQuest.completed)
        {
            buttonText.text = "Отримати нагороду";
            actionButton.interactable = true;
            actionButton.onClick.AddListener(() => onClaim?.Invoke());
        }
        else
        {
            buttonText.text = "У процесі";
            actionButton.interactable = false;
        }
    }
}