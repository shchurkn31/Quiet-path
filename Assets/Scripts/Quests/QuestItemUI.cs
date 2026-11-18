
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
        if (actionButton == null)
            actionButton = GetComponentInChildren<Button>(true);

        if (buttonText == null && actionButton != null)
            buttonText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void SetupAvailable(
        QuestDefinition definition,
        Action onAccept)
    {
        if (definition == null)
            return;

        questNameText.text = definition.questName;
        questDescriptionText.text = definition.description;
        questProgressText.text = "Ще не прийнято";
        questRewardText.text =
            "Нагорода: " + definition.rewardGold + " золота";

        SetButton("Прийняти", true);

        if (actionButton == null)
        {
            Debug.LogError("QuestItemUI: не знайдено ActionButton!", this);
            return;
        }

        actionButton.onClick.RemoveAllListeners();

        actionButton.onClick.AddListener(() =>
        {
            Debug.Log("Натиснуто кнопку прийняття: " + definition.questId);
            onAccept?.Invoke();
        });
    }

    public void SetupActive(
        QuestDefinition definition,
        ActiveQuest activeQuest,
        Action onClaim)
    {
        if (definition == null || activeQuest == null)
            return;

        questNameText.text = definition.questName;
        questDescriptionText.text = definition.description;

        questProgressText.text =
            "Прогрес: " + activeQuest.currentKills +
            "/" + definition.requiredKills;

        questRewardText.text =
            "Нагорода: " + definition.rewardGold + " золота";

        if (actionButton == null)
            return;

        actionButton.onClick.RemoveAllListeners();

        if (activeQuest.completed)
        {
            SetButton("Отримати нагороду", true);

            actionButton.onClick.AddListener(() =>
            {
                onClaim?.Invoke();
            });
        }
        else
        {
            SetButton("У процесі", false);
        }
    }

    private void SetButton(string text, bool interactable)
    {
        if (buttonText != null)
            buttonText.text = text;
        else
            Debug.LogWarning("QuestItemUI: не призначено Button Text!", this);

        if (actionButton != null)
            actionButton.interactable = interactable;
    }
}
