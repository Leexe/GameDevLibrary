# Dialogue System v1.1

## v1.1
- Replaced the static `DialogueEvents` class with an instanced `DialogueState` event hub class.
- Added a branching dialogue choice system (`DialogueChoicesController` and `DialogueChoiceBox`).
- Removed `DialogueBox.cs`, `Voice` folder, and updated `VisualNovelDictionary` data structures.
- Updated core scripts (`DialogueController`, `VNCharacter`, `VisualNovelUI`, etc.) to support the new event system.
