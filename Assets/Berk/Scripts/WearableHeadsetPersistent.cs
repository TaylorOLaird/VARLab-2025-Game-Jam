using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WearableHeadsetPersistent : MonoBehaviour
{
    [Header("Runtime targets")]
    public XRSocketInteractor headSocket;               // assign your HeadSocket (child of Camera)
    public XRGrabInteractable grabInteractable;        // will auto-assign if null

    [Header("Unwear control")]
    [Tooltip("If false, attempting to remove the headset will be prevented.")]
    public bool canUnwear = true;

    [Tooltip("Transform to snap headset to when forcibly reattaching (usually a child of the player camera).")]
    public Transform headAttachPoint;


    [Header("Effects")]
    public Volume postProcessVolume;                   // optional: set weight to 1 when worn, 0 when off
    public List<GameObject> objectsToEnableOnWear;     // Other scene objects to enable/disable
    public List<GameObject> objectsToDisableOnWear;     // Other scene objects to enable/disable

    [Header("Scene / behaviour")]
#if UNITY_EDITOR
    public SceneAsset dimensionSceneAsset;             // editor-only friendly assignment
#endif
    [Tooltip("Name of scene in Build Settings. Will be filled from SceneAsset if assigned in editor.")]
    public string dimensionSceneName;

    [Header("Scene / behaviour")]
#if UNITY_EDITOR
    public SceneAsset finalSceneAsset;             // editor-only friendly assignment
#endif
    [Tooltip("Name of scene in Build Settings. Will be filled from SceneAsset if assigned in editor.")]
    public string finalSceneName;

    public bool loadSceneAdditively = true;
    public bool unloadSceneOnUnwear = true;

    // internal
    AsyncOperation _loadOp;
    bool _isWorn = false;

    // track the loaded scene reference
    Scene _loadedScene;
    bool _sceneIsLoaded = false;

    // track grabbed objects that belong to the loaded scene
    HashSet<GameObject> _currentlyHeldFromLoadedScene = new HashSet<GameObject>();

    // cached grab listeners so we can unsubscribe
    List<XRGrabInteractable> _sceneGrabInteractables = new List<XRGrabInteractable>();

    [Header("Debug / dev")]
    [Tooltip("When true, clears persistent_objects.json on startup (useful for testing).")]
    public bool flushPersistentDataOnStart = false;

    [Header("Audio")]
    [Tooltip("ID of ambient audio entry in AudioManager to play while this headset is worn (leave empty for none).")]
    public string ambientSoundId;

    void Start()
    {
        if (flushPersistentDataOnStart)
        {
            PersistentSceneStateManager.ClearAll();
        }
    }

    void Reset()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        if (dimensionSceneAsset != null)
            dimensionSceneName = dimensionSceneAsset.name;
        if (finalSceneAsset != null)
            finalSceneName = finalSceneAsset.name;
#endif
    }

    void OnEnable()
    {
        if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // These are select events for the headset itself (grabbed/worn via socket).

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // If the head socket put it on, allow wearing
        if (args.interactorObject is XRSocketInteractor socket && socket == headSocket)
        {
            Wear();
            return;
        }

        // It's a controller / other interactor trying to grab the headset.
        // Block grabs when canUnwear is false by forcing SelectExit using the manager (new IXR API).
        if (!canUnwear)
        {
            // Try to cancel selection through the interaction manager using the IXR interfaces
            var manager = grabInteractable.interactionManager;

            var ixrInteractor = args.interactorObject as IXRSelectInteractor;
            var ixrInteractable = args.interactableObject as IXRSelectInteractable;

            if (manager != null && ixrInteractor != null && ixrInteractable != null)
            {
                manager.SelectExit(ixrInteractor, ixrInteractable);
                Debug.Log("[WearableHeadsetPersistent] Blocked controller grab via SelectExit (IXR).");
            }
            else
            {
                Debug.LogWarning("[WearableHeadsetPersistent] Blocking grab fallback: reattaching next frame.");
            }

            return;
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        // Only care if the head socket deselected us
        if (args.interactorObject is XRSocketInteractor socket && socket == headSocket)
        {
            if (!canUnwear)
            {
                return;
            }

            // allowed -> perform normal unwear
            Unwear();
        }
    }

    void Wear()
    {
        if (_isWorn) return;
        _isWorn = true;

        if (gameObject.name == "FinalHeadset")
        {
            SceneManager.LoadSceneAsync(finalSceneName);
        }

        if (postProcessVolume != null) postProcessVolume.weight = 1f;

        // start ambient sfx for this headset (if assigned)
        if (!string.IsNullOrEmpty(ambientSoundId) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAmbient(ambientSoundId);
        }

        foreach (var go in objectsToEnableOnWear) if (go) go.SetActive(true);
        foreach (var go in objectsToDisableOnWear) if (go) go.SetActive(false);

        if (!string.IsNullOrEmpty(dimensionSceneName) && loadSceneAdditively)
        {
            if (!SceneManager.GetSceneByName(dimensionSceneName).isLoaded)
            {
                _loadOp = SceneManager.LoadSceneAsync(dimensionSceneName, LoadSceneMode.Additive);
                // OnSceneLoaded will handle setup
            }
            else
            {
                // already loaded, just ensure setup
                _loadedScene = SceneManager.GetSceneByName(dimensionSceneName);
                _sceneIsLoaded = true;
                SetupGrabListenersForLoadedScene(_loadedScene);
                // apply saved transforms for PersistentObjects (in case)
                PersistentSceneStateManager.ApplySavedStatesForScene(_loadedScene.name);
            }
        }
        else if (!string.IsNullOrEmpty(dimensionSceneName) && !loadSceneAdditively)
        {
            SceneManager.LoadScene(dimensionSceneName);
        }
    }

    void Unwear()
    {
        if (!_isWorn) return;
        _isWorn = false;

        if (postProcessVolume != null) postProcessVolume.weight = 0f;

        // stop ambient sfx for this headset (if assigned)
        if (!string.IsNullOrEmpty(ambientSoundId) && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAmbient(ambientSoundId);
        }

        foreach (var go in objectsToEnableOnWear) if (go) go.SetActive(false);
        foreach (var go in objectsToDisableOnWear) if (go) go.SetActive(true);

        if (unloadSceneOnUnwear && !string.IsNullOrEmpty(dimensionSceneName) && loadSceneAdditively)
        {
            // Before unloading, make sure any held-from-loaded-scene objects are moved to the main active scene
            MoveHeldObjectsToMainScene();

            // Save transforms of persistent objects remaining in the loaded scene
            if (_sceneIsLoaded)
                PersistentSceneStateManager.SaveTransformsForScene(_loadedScene);

            // Unsubscribe listeners and unload
            TeardownGrabListenersForLoadedScene();
            if (_sceneIsLoaded && SceneManager.GetSceneByName(dimensionSceneName).isLoaded)
            {
                SceneManager.UnloadSceneAsync(dimensionSceneName);
            }
            _sceneIsLoaded = false;
            _loadedScene = default;
        }
    }




    // Called when any scene loads - we'll use it to detect when our target scene finishes loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(dimensionSceneName) && scene.name == dimensionSceneName)
        {
            _loadedScene = scene;
            _sceneIsLoaded = true;

            // Apply saved transforms to persistent objects (so they appear where player left them)
            PersistentSceneStateManager.ApplySavedStatesForScene(scene.name);

            // Set up grab listeners so we know what the player grabs inside this loaded scene
            SetupGrabListenersForLoadedScene(scene);

            // NEW: if the player is currently holding persistent objects in another scene (e.g. main),
            // move those held objects into this loaded additive scene and register them.
            TransferHeldObjectsToLoadedScene();
        }
    }

    void OnSceneUnloaded(Scene scene)
    {
        if (!string.IsNullOrEmpty(dimensionSceneName) && scene.name == dimensionSceneName)
        {
            _sceneIsLoaded = false;
            _loadedScene = default;
            _sceneGrabInteractables.Clear();
            _currentlyHeldFromLoadedScene.Clear();
        }
    }

    void SetupGrabListenersForLoadedScene(Scene scene)
    {
        _sceneGrabInteractables.Clear();
        var roots = scene.GetRootGameObjects();
        foreach (var r in roots)
        {
            var grabs = r.GetComponentsInChildren<XRGrabInteractable>(true);
            foreach (var g in grabs)
            {
                // don't add duplicates
                if (_sceneGrabInteractables.Contains(g)) continue;
                _sceneGrabInteractables.Add(g);
                g.selectEntered.AddListener(OnObjectSelectEntered);
                g.selectExited.AddListener(OnObjectSelectExited);
            }
        }
    }

    void TeardownGrabListenersForLoadedScene()
    {
        foreach (var g in _sceneGrabInteractables)
        {
            if (g == null) continue;
            g.selectEntered.RemoveListener(OnObjectSelectEntered);
            g.selectExited.RemoveListener(OnObjectSelectExited);
        }
        _sceneGrabInteractables.Clear();
    }

    // called when any XRGrabInteractable in the loaded scene is grabbed
    void OnObjectSelectEntered(SelectEnterEventArgs args)
    {
        var go = args.interactableObject.transform.gameObject;
        // if this object belongs to the loaded additive scene, track it
        if (_sceneIsLoaded && go.scene == _loadedScene)
        {
            _currentlyHeldFromLoadedScene.Add(go);
        }
    }

    // called when any XRGrabInteractable in the loaded scene is released
    void OnObjectSelectExited(SelectExitEventArgs args)
    {
        var go = args.interactableObject.transform.gameObject;
        if (_currentlyHeldFromLoadedScene.Contains(go))
        {
            // the player released it inside the additive scene; it will remain in that scene
            _currentlyHeldFromLoadedScene.Remove(go);
        }
    }

    // move objects that are currently being held and belong to the loaded scene into the main active scene
    void MoveHeldObjectsToMainScene()
    {
        if (!_sceneIsLoaded) return;

        // get current active scene (should be your main scene where XR Origin lives)
        Scene main = SceneManager.GetActiveScene();

        // make a list to avoid modifying set during iteration
        var held = new List<GameObject>(_currentlyHeldFromLoadedScene);

        foreach (var go in held)
        {
            if (go == null) continue;
            // move to main scene so it isn't destroyed with the additive scene unload
            SceneManager.MoveGameObjectToScene(go, main);

            // now persist that this object is in the main scene and record its transform
            PersistentSceneStateManager.MarkObjectInSceneAndSave(go, main.name);

            // remove from tracking set (now it's in main scene)
            _currentlyHeldFromLoadedScene.Remove(go);
        }
    }
    // add near other methods in WearableHeadsetPersistent
    void TransferHeldObjectsToLoadedScene()
    {
        if (!_sceneIsLoaded || !_loadedScene.IsValid()) return;

        // find all XRGrabInteractables in the project (including inactive ones)
        var allGrabs = UnityEngine.Object.FindObjectsOfType<XRGrabInteractable>(true);

        foreach (var grab in allGrabs)
        {
            if (grab == null) continue;

            // only consider currently selected (held) items
            if (!grab.isSelected) continue;

            var go = grab.gameObject;
            var p = go.GetComponent<PersistentObject>();
            if (p == null || string.IsNullOrEmpty(p.id)) continue;

            // if the grabbed object is not already part of the loaded additive scene, move it
            if (go.scene != _loadedScene)
            {
                // Move into additive scene so it becomes part of that scene
                SceneManager.MoveGameObjectToScene(go, _loadedScene);

                // Ensure we have listeners so we track release/hold in the dimension
                if (!_sceneGrabInteractables.Contains(grab))
                {
                    _sceneGrabInteractables.Add(grab);
                    grab.selectEntered.AddListener(OnObjectSelectEntered);
                    grab.selectExited.AddListener(OnObjectSelectExited);
                }

                // Track as currently held from the loaded scene (so MoveHeldObjectsToMainScene can handle it on unwear)
                _currentlyHeldFromLoadedScene.Add(go);

                // Persist that this object now belongs to the additive scene and save its transform
                PersistentSceneStateManager.MarkObjectInSceneAndSave(go, _loadedScene.name);

                Debug.Log($"[WearableHeadsetPersistent] Transferred held persistent object '{p.id}' into scene '{_loadedScene.name}'");
            }
        }
    }

}
