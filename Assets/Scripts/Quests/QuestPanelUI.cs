
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

    private QuestManager subscribedManager;

    private void OnEnable()
    {
        SubscribeToQuestChanges();
    }

    private void Start()
    {
        SubscribeToQuestChanges();
        RefreshQuests();
    }

    private void OnDisable()
    {
        UnsubscribeFromQuestChanges();
    }

    private void SubscribeToQuestChanges()
    {
        if (QuestManager.Instance == null)
            return;

        if (subscribedManager == QuestManager.Instance)
            return;

        UnsubscribeFromQuestChanges();

        subscribedManager = QuestManager.Instance;
        subscribedManager.OnQuestChanged += RefreshQuests;
    }

    private void UnsubscribeFromQuestChanges()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnQuestChanged -= RefreshQuests;
            subscribedManager = null;
        }
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
        QuestManager manager = QuestManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("QuestPanelUI: QuestManager не знайдено!");
            return;
        }

        if (questItemPrefab == null)
        {
            Debug.LogError("QuestPanelUI: не призначено Quest Item Prefab!");
            return;
        }

        if (availableQuestsParent == null || activeQuestsParent == null)
        {
            Debug.LogError("QuestPanelUI: не призначено контейнери квестів!");
            return;
        }

        ClearContainer(availableQuestsParent);
        ClearContainer(activeQuestsParent);

        if (manager.activeQuest != null)
        {
            QuestDefinition definition =
                manager.GetQuestDefinition(manager.activeQuest.questId);

            if (definition == null)
            {
                Debug.LogWarning(
                    "QuestPanelUI: не знайдено визначення активного квесту. " +
                    "ID: " + manager.activeQuest.questId
                );

                return;
            }

            QuestItemUI item = Instantiate(
                questItemPrefab,
                activeQuestsParent
            );

            string questId = manager.activeQuest.questId;

            item.SetupActive(
                definition,
                manager.activeQuest,
                () =>
                {
                    manager.ClaimReward(questId);
                }
            );
        }
        else
        {
            QuestDefinition definition = manager.CurrentDefinition;

            if (definition == null)
            {
                Debug.Log("QuestPanelUI: усі квести виконано.");
                return;
            }

            QuestItemUI item = Instantiate(
                questItemPrefab,
                availableQuestsParent
            );

            string questId = definition.questId;

            item.SetupAvailable(
                definition,
                () =>
                {
                    manager.AcceptQuest(questId);
                }
            );
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}
