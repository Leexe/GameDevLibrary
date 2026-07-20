using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class QuestManager : PersistentMonoSingleton<QuestManager>
{
	private HashSet<string> _activeQuests = new();
	private HashSet<string> _completedQuests = new();

	public bool IsQuestActive(string questId)
	{
		return _activeQuests.Contains(questId);
	}

	public bool IsQuestCompleted(string questId)
	{
		return _completedQuests.Contains(questId);
	}

	[Button]
	public void StartQuest(string questId)
	{
		_activeQuests.Add(questId);
		_completedQuests.Remove(questId);
		Debug.Log($"[QuestManager] Quest {questId} started");
	}

	[Button]
	public void CompleteQuest(string questId)
	{
		_activeQuests.Remove(questId);
		_completedQuests.Add(questId);
		Debug.Log($"[QuestManager] Quest {questId} completed");
	}
}
