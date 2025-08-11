using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.InputSystem.HID;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

public class EndSwitcher : MonoBehaviour
{
    public Image fadeImage;        // Fullscreen black Image
    public GameObject loadingUI;   // Optional loading UI object
    public AudioSource endAudio;
    public AudioSource radio;
    public AudioSource music;
    public TMP_Text text;
    public float fadeDuration = 1f;

    public void End()
    {
        FadeAndLoad();
    }

    private void FadeAndLoad()
    {

        StartCoroutine(FadeAndSwitch());
    }

    private IEnumerator FadeAndSwitch()
    {
        fadeImage.gameObject.SetActive(true);
        radio.Pause();
        // Fade to black  
        yield return StartCoroutine(Fade(0f, 1f));

        // Show loading screen  
        if (loadingUI != null)
            loadingUI.SetActive(true);



        // Small pause to show loading screen  
        yield return new WaitForSeconds(1f);
        endAudio.Play();
        yield return new WaitForSeconds(8f);
        music.Play();
        text.text = "THE END";

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
