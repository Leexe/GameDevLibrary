# Dialogue System v1.1

## v1.2
- Added `OnDialogueStarted` and `OnDialogueEnded` events.
- Added external function bindings for `QuestManager` (`IsQuestCompleted`, `IsQuestActive`, `StartQuest`, `CompleteQuest`).
- Updated `DialogueSFX` to properly track and stop the current ambience using `_currentAmbienceKey`.
- Updated `DialogueSFX` to use the renamed `AudioManager.PlayMusic`.

## v1.1
- Replaced the static `DialogueEvents` class with an instanced `DialogueState` event hub class.
- Added a branching dialogue choice system (`DialogueChoicesController` and `DialogueChoiceBox`).
- Removed `DialogueBox.cs`, `Voice` folder, and updated `VisualNovelDictionary` data structures.
- Updated core scripts (`DialogueController`, `VNCharacter`, `VisualNovelUI`, etc.) to support the new event system.
- Made the system self-contained: removed all `GameManager` dependencies. `DialogueController` owns `DialogueState` and exposes it via a public property. Other scripts reference it through a serialized `DialogueController` field.
- Removed game-specific quest bindings (`StartQuest`, `AdvanceQuest`, etc.) from `DialogueController`.
- Added `OnPause`/`OnUnpause` events to `DialogueState` for typewriter control (replaces `GameManager` pause events).
- `DialogueChoiceBox` click SFX is now an optional serialized `EventReference` field instead of a hard-coded `FMODEvents` reference.
