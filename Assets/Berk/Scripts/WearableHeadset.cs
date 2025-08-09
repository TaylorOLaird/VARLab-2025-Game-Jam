using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering; // for Volume
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WearableHeadset : MonoBehaviour
{
    [Header("Runtime targets")]
    public XRSocketInteractor headSocket;               // assign your HeadSocket (child of Camera)
    public XRGrabInteractable grabInteractable;        // will auto-assign if null

    [Header("Effects")]
    public Volume postProcessVolume;                   // optional: set weight to 1 when worn, 0 when off
    public List<GameObject> objectsToEnableOnWear;     // Other scene objects to enable/disable

    [Header("Scene / behaviour")]
    public string dimensionSceneName;                  // optional: additive scene to load when worn
    public bool loadSceneAdditively = true;
    public bool unloadSceneOnUnwear = true;

    AsyncOperation _loadOp;
    bool _isWorn = false;

    void Reset()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // If it was placed into the head socket, mark as worn.
        if (args.interactorObject is XRSocketInteractor socket && socket == headSocket)
        {
            Wear();
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        // If it was removed from head socket, unwear.
        if (args.interactorObject is XRSocketInteractor socket && socket == headSocket)
        {
            Unwear();
        }
    }

    void Wear()
    {
        if (_isWorn) return;
        _isWorn = true;
        // post-process
        if (postProcessVolume != null) postProcessVolume.weight = 1f;
        // objects on
        foreach (var go in objectsToEnableOnWear) if (go) go.SetActive(true);
        // load additive scene
        if (!string.IsNullOrEmpty(dimensionSceneName) && loadSceneAdditively)
        {
            if (SceneManager.GetSceneByName(dimensionSceneName).isLoaded == false)
                _loadOp = SceneManager.LoadSceneAsync(dimensionSceneName, LoadSceneMode.Additive);
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
        foreach (var go in objectsToEnableOnWear) if (go) go.SetActive(false);
        if (unloadSceneOnUnwear && !string.IsNullOrEmpty(dimensionSceneName) && loadSceneAdditively)
        {
            // ensure loaded before unload
            if (SceneManager.GetSceneByName(dimensionSceneName).isLoaded)
                SceneManager.UnloadSceneAsync(dimensionSceneName);
        }
    }
}
