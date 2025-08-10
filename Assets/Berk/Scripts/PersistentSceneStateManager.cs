using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct SerializableVector3
{
    public float x, y, z;
    public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[System.Serializable]
public struct SerializableQuaternion
{
    public float x, y, z, w;
    public SerializableQuaternion(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
}

[System.Serializable]
public class PersistentObjectState
{
    public string id;

    // which scene this object currently belongs to
    public string currentScene;

    // parent persistent object id (optional) — if set, we'll try to reparent to this parent on load
    public string parentId;

    // local transform (relative to parent) — used if the parent is available
    public SerializableVector3 localPosition;
    public SerializableQuaternion localRotation;
    public SerializableVector3 localScale;

    // world transform — fallback when parent is not available
    public SerializableVector3 worldPosition;
    public SerializableQuaternion worldRotation;
    public SerializableVector3 worldScale;
}

[System.Serializable]
public class PersistentGlobalData
{
    public List<PersistentObjectState> objects = new List<PersistentObjectState>();
}

public static class PersistentSceneStateManager
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "persistent_objects.json");

    static PersistentGlobalData LoadGlobalData()
    {
        if (!File.Exists(FilePath)) return new PersistentGlobalData();
        string json = File.ReadAllText(FilePath);
        var data = JsonUtility.FromJson<PersistentGlobalData>(json);
        return data ?? new PersistentGlobalData();
    }

    static void SaveGlobalData(PersistentGlobalData data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(FilePath, json);
        Debug.Log($"[PersistentSceneStateManager] Saved {data.objects.Count} object states to {FilePath}");
    }

    static PersistentObjectState GetStateForId(PersistentGlobalData data, string id)
    {
        if (data == null || string.IsNullOrEmpty(id)) return null;
        return data.objects.Find(x => x.id == id);
    }

    // Utility: find a PersistentObject by id in a specific scene (search roots)
    static PersistentObject FindPersistentByIdInScene(Scene scene, string id)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(id)) return null;
        var roots = scene.GetRootGameObjects();
        foreach (var r in roots)
        {
            var pers = r.GetComponentsInChildren<PersistentObject>(true);
            foreach (var p in pers)
            {
                if (p != null && p.id == id) return p;
            }
        }
        return null;
    }

    // Save a single object's full state (scene, parent if persistent, local+world transforms)
    public static void SaveObjectState(GameObject go)
    {
        if (go == null) return;
        var pcomp = go.GetComponent<PersistentObject>();
        if (pcomp == null || string.IsNullOrEmpty(pcomp.id)) return;

        var data = LoadGlobalData();
        var state = GetStateForId(data, pcomp.id);
        if (state == null)
        {
            state = new PersistentObjectState { id = pcomp.id };
            data.objects.Add(state);
        }

        state.currentScene = go.scene.IsValid() ? go.scene.name : "";
        // capture parent if it has PersistentObject
        var parentPers = go.transform.parent ? go.transform.parent.GetComponentInParent<PersistentObject>() : null;
        state.parentId = parentPers != null ? parentPers.id : null;

        // local transform
        state.localPosition = new SerializableVector3(go.transform.localPosition);
        state.localRotation = new SerializableQuaternion(go.transform.localRotation);
        state.localScale = new SerializableVector3(go.transform.localScale);

        // world transform (fallback)
        state.worldPosition = new SerializableVector3(go.transform.position);
        state.worldRotation = new SerializableQuaternion(go.transform.rotation);
        state.worldScale = new SerializableVector3(go.transform.lossyScale);

        SaveGlobalData(data);
        Debug.Log($"[PersistentSceneStateManager] Saved state for '{pcomp.id}' in scene '{state.currentScene}', parentId='{state.parentId}'");
    }

    // Save all persistent objects currently in a scene (use on unload)
    public static void SaveTransformsForScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var data = LoadGlobalData();

        foreach (var r in roots)
        {
            var pers = r.GetComponentsInChildren<PersistentObject>(true);
            foreach (var p in pers)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;
                if (p.gameObject.scene != scene) continue;

                var state = GetStateForId(data, p.id);
                if (state == null)
                {
                    state = new PersistentObjectState { id = p.id };
                    data.objects.Add(state);
                }

                state.currentScene = scene.name;
                var go = p.gameObject;

                var parentPers = go.transform.parent ? go.transform.parent.GetComponentInParent<PersistentObject>() : null;
                state.parentId = parentPers != null ? parentPers.id : null;

                // local and world transforms
                state.localPosition = new SerializableVector3(go.transform.localPosition);
                state.localRotation = new SerializableQuaternion(go.transform.localRotation);
                state.localScale = new SerializableVector3(go.transform.localScale);

                state.worldPosition = new SerializableVector3(go.transform.position);
                state.worldRotation = new SerializableQuaternion(go.transform.rotation);
                state.worldScale = new SerializableVector3(go.transform.lossyScale);
            }
        }

        SaveGlobalData(data);
    }

    // Apply saved states when a scene loads:
    // - if state says the object belongs to another scene — destroy the instance in this scene
    // - if state says it belongs here:
    //      * try to reparent to parentId if that parent exists in this scene, then apply local transform
    //      * otherwise unparent and apply world transform
    public static void ApplySavedStatesForScene(string sceneName)
    {
        var data = LoadGlobalData();
        if (data == null || data.objects == null) return;

        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return;

        var dict = new Dictionary<string, PersistentObjectState>();
        foreach (var s in data.objects) dict[s.id] = s;

        var roots = scene.GetRootGameObjects();
        int applied = 0;
        int destroyed = 0;
        foreach (var r in roots)
        {
            var pers = r.GetComponentsInChildren<PersistentObject>(true);
            foreach (var p in pers)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;

                if (dict.TryGetValue(p.id, out var state))
                {
                    if (state.currentScene != sceneName)
                    {
                        // belongs elsewhere -> destroy instance here
#if UNITY_EDITOR
                        if (!Application.isPlaying) Object.DestroyImmediate(p.gameObject);
                        else
#endif
                            Object.Destroy(p.gameObject);
                        destroyed++;
                        continue;
                    }

                    // belongs here -> attempt reparent
                    var go = p.gameObject;
                    bool parentApplied = false;
                    if (!string.IsNullOrEmpty(state.parentId))
                    {
                        var parentPers = FindPersistentByIdInScene(scene, state.parentId);
                        if (parentPers != null)
                        {
                            // parent exists in scene: reparent and apply local transform
                            go.transform.SetParent(parentPers.transform, worldPositionStays: false);
                            go.transform.localPosition = state.localPosition.ToVector3();
                            go.transform.localRotation = state.localRotation.ToQuaternion();
                            go.transform.localScale = state.localScale.ToVector3();
                            parentApplied = true;
                            applied++;
                        }
                    }

                    if (!parentApplied)
                    {
                        // parent not available (or none recorded) -> unparent and apply world transform
                        go.transform.SetParent(null, worldPositionStays: true);
                        go.transform.position = state.worldPosition.ToVector3();
                        go.transform.rotation = state.worldRotation.ToQuaternion();
                        // lossyScale -> can't set directly; approximate via localScale if no parent:
                        go.transform.localScale = state.worldScale.ToVector3();
                        applied++;
                    }
                }
                // else: no saved state -> keep authoring state
            }
        }

        Debug.Log($"[PersistentSceneStateManager] Applied {applied} transforms and destroyed {destroyed} objects for scene '{sceneName}'.");
    }

    // Helper to mark object in given scene and save (use when moving between scenes)
    public static void MarkObjectInSceneAndSave(GameObject go, string sceneName)
    {
        if (go == null) return;
        var p = go.GetComponent<PersistentObject>();
        if (p == null || string.IsNullOrEmpty(p.id)) return;

        // If the object has a parent that is persistent, capture its id; otherwise null
        var parentPers = go.transform.parent ? go.transform.parent.GetComponentInParent<PersistentObject>() : null;
        var parentId = parentPers != null ? parentPers.id : null;

        var data = LoadGlobalData();
        var state = GetStateForId(data, p.id);
        if (state == null)
        {
            state = new PersistentObjectState { id = p.id };
            data.objects.Add(state);
        }

        state.currentScene = sceneName;
        state.parentId = parentId;

        // store both local and world transforms
        state.localPosition = new SerializableVector3(go.transform.localPosition);
        state.localRotation = new SerializableQuaternion(go.transform.localRotation);
        state.localScale = new SerializableVector3(go.transform.localScale);

        state.worldPosition = new SerializableVector3(go.transform.position);
        state.worldRotation = new SerializableQuaternion(go.transform.rotation);
        state.worldScale = new SerializableVector3(go.transform.lossyScale);

        SaveGlobalData(data);
        Debug.Log($"[PersistentSceneStateManager] Marked '{p.id}' in scene '{sceneName}', parentId='{parentId}'.");
    }

    // Optional helper to query saved scene
    public static string GetSavedSceneForId(string id)
    {
        var data = LoadGlobalData();
        var state = GetStateForId(data, id);
        return state != null ? state.currentScene : null;
    }
    /// <summary>
    /// Deletes the persistent_objects.json file so all saved states are flushed.
    /// </summary>
    public static void ClearAll()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
                Debug.Log($"[PersistentSceneStateManager] Cleared all persistent data ({FilePath}).");
            }
            else
            {
                Debug.Log($"[PersistentSceneStateManager] No persistent file to clear ({FilePath}).");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PersistentSceneStateManager] Failed to clear persistent file: {ex}");
        }
    }

}
