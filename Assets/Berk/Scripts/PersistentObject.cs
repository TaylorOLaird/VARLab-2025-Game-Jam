using System;
using UnityEngine;

[ExecuteAlways]
public class PersistentObject : MonoBehaviour
{
    [Tooltip("Must be unique across objects in all your dimensional scenes. Use Generate ID to create.")]
    public string id;

    [ContextMenu("Generate ID")]
    public void GenerateId()
    {
        id = Guid.NewGuid().ToString();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    void Reset()
    {
        if (string.IsNullOrEmpty(id))
            GenerateId();
    }
}
