using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class PersistentTransformData
{
    public string id;
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public SerializableVector3 scale;
}

[System.Serializable]
public class PersistentSceneData
{
    public List<PersistentTransformData> objects = new List<PersistentTransformData>();
}

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

public static class PersistentSceneStateManager
{
    static string FilePathForScene(string sceneName)
    {
        return Path.Combine(Application.persistentDataPath, $"sceneState_{sceneName}.json");
    }

    // Save transforms for ALL PersistentObject components that still belong to the given scene.
    public static void SaveTransformsForScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var data = new PersistentSceneData();

        foreach (var r in roots)
        {
            var pers = r.GetComponentsInChildren<PersistentObject>(true);
            foreach (var p in pers)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;
                if (p.gameObject.scene != scene) continue; // only objects still in this scene

                var t = p.transform;
                var item = new PersistentTransformData
                {
                    id = p.id,
                    position = new SerializableVector3(t.localPosition),
                    rotation = new SerializableQuaternion(t.localRotation),
                    scale = new SerializableVector3(t.localScale)
                };
                data.objects.Add(item);
            }
        }

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(FilePathForScene(scene.name), json);
        Debug.Log($"[PersistentSceneStateManager] Saved {data.objects.Count} persistent objects for scene '{scene.name}' at {FilePathForScene(scene.name)}");
    }

    // Apply saved transforms after the scene is loaded and objects exist.
    public static void ApplySavedTransformsForScene(string sceneName)
    {
        string path = FilePathForScene(sceneName);
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<PersistentSceneData>(json);
        if (data == null || data.objects == null) return;

        // find the scene
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return;

        // map id -> transform for quick lookup
        var dict = new Dictionary<string, PersistentTransformData>();
        foreach (var item in data.objects) dict[item.id] = item;

        var roots = scene.GetRootGameObjects();
        int applied = 0;
        foreach (var r in roots)
        {
            var pers = r.GetComponentsInChildren<PersistentObject>(true);
            foreach (var p in pers)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;
                if (dict.TryGetValue(p.id, out var tdata))
                {
                    var tr = p.transform;
                    tr.localPosition = tdata.position.ToVector3();
                    tr.localRotation = tdata.rotation.ToQuaternion();
                    tr.localScale = tdata.scale.ToVector3();
                    applied++;
                }
            }
        }
        Debug.Log($"[PersistentSceneStateManager] Applied {applied} saved transforms to scene '{sceneName}'.");
    }
}
