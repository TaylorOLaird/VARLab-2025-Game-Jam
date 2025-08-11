// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class TextureFromCamera : MonoBehaviour
// {
//     // Start is called before the first frame update
//     void Start()
//     {
        
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }
using UnityEngine;
using System.Collections;

public class TextureFromCamera : MonoBehaviour 
{ 
    public Camera cam;
    public Transform target;
    public RenderTexture renderTexture;
    public Vector2 textureSize = new Vector2(512,512);
    public int depth = 16;

    void Reset() 
    {
        if (!cam) cam = Camera.main;
        if (!target) target = transform;
    }

    void Awake() 
    {
        Reset();
    }

    void Start () 
    {
        if ((depth != 0) && (depth != 16) && (depth != 24)) 
        {
            //Debug.LogError(this + " depth must be one of 0, 16, 24");
        }

        if (!renderTexture) 
        {
            renderTexture = new RenderTexture((int)textureSize.x, (int)textureSize.y, depth);//depth must be one of 0, 16, 24
            renderTexture.name = "TextureFromCamera_" + cam.name; //optional, but nice
            renderTexture.Create();
            //Debug.Log(renderTexture.depth + " width " + renderTexture.width + " height " + renderTexture.height);
        }

        cam.targetTexture = renderTexture;
        target.GetComponent<Renderer>().material.mainTexture = renderTexture;
    }

    void OnDisable() 
    {
        renderTexture.DiscardContents();    
        renderTexture.Release();    
    }
}