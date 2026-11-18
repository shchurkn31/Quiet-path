using TMPro;
using UnityEngine;

public class HuntingQuestHUD : MonoBehaviour
{
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TMP_Text questText;

    private QuestManager questManager;

    private void OnEnable()
    {
        questManager = QuestManager.Instance;

        if (questManager != null)
            questManager.OnQuestChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (!ReferenceEquals(questManager, null))
            questManager.OnQuestChanged -= Refresh;

        questManager = null;
    }

    private void Refresh()
    {
        ActiveQuest activeQuest = questManager != null ? questManager.activeQuest : null;
        QuestDefinition definition = activeQuest != null
            ? questManager.GetQuestDefinition(activeQuest.questId)
            : null;

        if (definition == null)
        {
            questPanel.SetActive(false);
            return;
        }

        string status = activeQuest.completed ? "Виконано" : "Прогрес";
        questText.text = $"<b>{definition.questName}</b>\n{definition.description}\n" +
                         $"{status}: {activeQuest.currentKills}/{definition.requiredKills}";
        questPanel.SetActive(true);
    }
}
