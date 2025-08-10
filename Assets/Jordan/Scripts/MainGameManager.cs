using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainGameManager : MonoBehaviour
{
    public Image fadeImage;        // Fullscreen black Image
    public GameObject loadingUI;   // Optional loading UI object
    public float fadeDuration = 1f;
    // Start is called before the first frame update
    void Start()
    {
        EventManager.OnHeadsetDon += HandleHeadsetDon;
    }

    void HandleHeadsetDon(HMD headset)
    {
        if(headset.gameObject.name.Equals("StartHeadset"))
        {
            StartHeadset();
        }
    }

    void StartHeadset()
    {
        FadeAndLoad("FirstRoom");
    }

    public void FadeAndLoad(string sceneName)
    {
        StartCoroutine(FadeAndSwitch(sceneName));
    }

    private IEnumerator FadeAndSwitch(string sceneName)
    {
        fadeImage.gameObject.SetActive(true);

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // Show loading screen
        if (loadingUI != null)
            loadingUI.SetActive(true);

        // Start loading asynchronously
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // Wait until the scene is loaded
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // Small pause to show loading screen
        yield return new WaitForSeconds(0.5f);

        // Activate the new scene
        op.allowSceneActivation = true;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        Color c = fadeImage.color;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, endAlpha, t / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
    }

}
