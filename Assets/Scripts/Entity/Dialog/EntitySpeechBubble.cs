using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// Used to display text above an entities head.
/// Currently used for debug purposes - might be useful for gameplay mechanics later?
/// </summary>
public class EntitySpeechBubble : MonoBehaviour
{

    public GameObject SpeechBubblePrefab;

    public ScrollRect ScrollRect;

    private List<GameObject> AddedMessages;

    public GameObject ViewPort;
    public Canvas SpeechCanvas;
    public TextMeshProUGUI Text;

    private float MessageTimeout = 15;

    private void Awake()
    {
        SpeechCanvas.worldCamera = PlayerManager.Instance.PlayerCamera;
        SpeechCanvas.enabled = false;

        AddedMessages = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (SpeechCanvas.enabled)
        {
            transform.LookAt(PlayerManager.Instance.PlayerCamera.transform);
            transform.Rotate(new Vector3(0, 180, 0));
        }
    }

    public void PushMessage(string text)
    {
        SpeechCanvas.enabled = true;
        GameObject nBubble = Instantiate(SpeechBubblePrefab);
        nBubble.transform.SetParent(ViewPort.transform, false);
        AddedMessages.Add(nBubble);
        nBubble.GetComponentInChildren<TextMeshProUGUI>().text = text;
        ScrollRect.verticalNormalizedPosition = 0;
        StartCoroutine(TextFadeOut(nBubble, MessageTimeout));
    }

    public void SetText(string text, float timeout = -1)
    {
        SpeechCanvas.enabled = true;
        PushMessage(text);
        return;
        Text.text = text;

        if(timeout > 0)
        {
            StartCoroutine(TextFadeOut(timeout));
        }
    }

    private IEnumerator TextFadeOut(GameObject text, float time)
    {
        Debug.Log("waiting for " + time);
        yield return new WaitForSeconds(time);
        Debug.Log("Removing");
        AddedMessages.Remove(text);
        Destroy(text.gameObject);

        if (AddedMessages.Count == 0)
            SpeechCanvas.enabled = false;
    }

    private IEnumerator TextFadeOut(float time)
    {
        Debug.Log("waiting for time " + time);
        yield return new WaitForSeconds(time);
        Debug.Log("hiding");
        SpeechCanvas.enabled = false;

    }
}
