
using UnityEngine;

public class QuestPanelUI : MonoBehaviour
{
    [Header("Панель")]
    public GameObject questsPanel;

    [Header("Контейнери")]
    public Transform availableQuestsParent;
    public Transform activeQuestsParent;

    [Header("Prefab картки")]
    public QuestItemUI questItemPrefab;

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestChanged += RefreshQuests;
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestChanged -= RefreshQuests;
    }

    private void Start()
    {
        RefreshQuests();
    }

    public void OpenPanel()
    {
        if (questsPanel != null)
            questsPanel.SetActive(true);

        RefreshQuests();
    }

    public void ClosePanel()
    {
        if (questsPanel != null)
            questsPanel.SetActive(false);
    }

    public void RefreshQuests()
    {
        if (QuestManager.Instance == null)
            return;

        if (questItemPrefab == null)
        {
            Debug.LogError("QuestPanelUI: не призначено Quest Item Prefab!");
            return;
        }

        ClearContainer(availableQuestsParent);
        ClearContainer(activeQuestsParent);

        QuestManager manager = QuestManager.Instance;

        if (manager.activeQuest != null)
        {
            QuestDefinition definition =
                manager.GetQuestDefinition(manager.activeQuest.questId);

            if (definition == null)
                return;

            QuestItemUI item = Instantiate(
                questItemPrefab,
                activeQuestsParent
            );

            item.SetupActive(
                definition,
                manager.activeQuest,
                () =>
                {
                    manager.ClaimReward(manager.activeQuest.questId);
                }
            );
        }
        else
        {
            QuestDefinition definition = manager.CurrentDefinition;

            if (definition == null)
                return;

            QuestItemUI item = Instantiate(
                questItemPrefab,
                availableQuestsParent
            );

            item.SetupAvailable(
                definition,
                () =>
                {
                    manager.AcceptQuest(definition.questId);
                }
            );
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null)
            return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
