// WorldTiledTexture.cs
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class TextureTiling : MonoBehaviour
{
    [Tooltip("World units per tile. 0.5 means each tile is 0.5m x 0.5m.")]
    public float tileSize = 0.5f;

    [Tooltip("Which texture set to sync. _BaseMap/_MainTex are albedo in URP/Builtin.")]
    public bool affectAlbedo = true;
    public bool affectNormal = true;     // _BumpMap
    public bool affectMask = true;       // _MetallicGlossMap in URP

    Renderer _r;
    Mesh _mesh;
    MaterialPropertyBlock _mpb;

    void OnEnable() { Init(); UpdateTiling(); }
    void OnValidate() { Init(); UpdateTiling(); }
    void Update()
    {
        // Keep it cheap in Play Mode; only when transform changes
        if (Application.isPlaying)
        {
            if (transform.hasChanged) { UpdateTiling(); transform.hasChanged = false; }
        }
        else
        {
            // In Edit Mode, update continuously so scaling in the editor previews correctly
            UpdateTiling();
        }
    }

    void Init()
    {
        if (_r == null) _r = GetComponent<Renderer>();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (_mesh == null)
        {
            var mf = GetComponent<MeshFilter>();
            _mesh = mf ? mf.sharedMesh : null;
        }
    }

    void UpdateTiling()
    {
        if (_r == null || tileSize <= 0f) return;

        // Compute world-size along the mesh's local X/Z (rotation-safe).
        float baseX = 1f, baseZ = 1f;
        if (_mesh != null)
        {
            var b = _mesh.bounds.size;         // local-space size of the mesh
            baseX = b.x;
            baseZ = b.z;
        }

        var s = transform.lossyScale;
        float worldX = Mathf.Abs(baseX * s.x);
        float worldZ = Mathf.Abs(baseZ * s.z);

        // Tiles per axis = world length / desired tile size
        Vector2 tiling = new Vector2(
            Mathf.Max(0.0001f, worldX / tileSize),
            Mathf.Max(0.0001f, worldZ / tileSize)
        );

        _r.GetPropertyBlock(_mpb);
        // Albedo
        if (affectAlbedo)
        {
            _mpb.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, 0, 0));
        }
        // Normal
        if (affectNormal)
        {
            _mpb.SetVector("_BumpMap_ST", new Vector4(tiling.x, tiling.y, 0, 0));
        }
        // Metallic/Mask (covering common names across pipelines)
        if (affectMask)
        {
            _mpb.SetVector("_MaskMap_ST", new Vector4(tiling.x, tiling.y, 0, 0));
        }
        _r.SetPropertyBlock(_mpb);
    }
}
