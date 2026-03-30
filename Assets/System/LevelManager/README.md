# Level Manager v1.0

How to use:
1) Load up a scene asynchronously: LevelManager.Instance.LoadSceneAsync(LevelManager.SceneNames.Game);
2) Listen to the OnSceneReady event in LevelManager that tells the script when the scene is finished loading
3) Create a method that calls LevelManager.Instance.ActivatePreloadedScene() if the scene is loaded and transition is
done playing
