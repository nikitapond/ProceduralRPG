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
    private List<GameObject> FreeBubbles;
    private List<GameObject> AddedMessages;
    private Queue<GameObject> Messages;
    public GameObject ViewPort;
    public Canvas SpeechCanvas;
    public TextMeshProUGUI Text;

    private float MessageTimeout = 30;
    public const int MaxMessages = 4;
    private void Awake()
    {
        SpeechCanvas.worldCamera = PlayerManager.Instance.PlayerCamera;
        SpeechCanvas.enabled = false;
        Messages = new Queue<GameObject>();
        AddedMessages = new List<GameObject>();
        FreeBubbles = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (SpeechCanvas.enabled)
        {

            //float angle = -Vector2.SignedAngle(transform.position.XZ(), PlayerManager.Instance.Player.Position2);
            //Debug.Log("Angle speech bubble: " + angle);
            //transform.rotation = Quaternion.Euler(0, 180, 0);
            transform.LookAt(PlayerManager.Instance.Player.GetLoadedEntity().transform);
            //transform.rotation = Quaternion.Euler(0, angle, 0);
            transform.Rotate(new Vector3(0, 180, 0));
        }
    }

    public void PushMessage(string text)
    {

        //prevent clone messages
        if (AddedMessages.Count > 0 && SpeechCanvas.enabled && text == AddedMessages[AddedMessages.Count - 1].GetComponentInChildren<TextMeshProUGUI>().text)
            return;

        

        SpeechCanvas.enabled = true;
        GameObject nBubble;
        if (FreeBubbles.Count == 0)
            nBubble = Instantiate(SpeechBubblePrefab);
        else
        {
            nBubble = FreeBubbles[0];
            FreeBubbles.RemoveAt(0);
        }
            

        nBubble.transform.SetParent(ViewPort.transform, false);
        AddedMessages.Add(nBubble);
        nBubble.GetComponentInChildren<TextMeshProUGUI>().text = text;
        ScrollRect.verticalNormalizedPosition = 0;

        Messages.Enqueue(nBubble);
        while(Messages.Count > MaxMessages)
        {
            GameObject rem = Messages.Dequeue();
            rem.SetActive(false);
            FreeBubbles.Add(rem);
            if (Messages.Count == 0)
                SpeechCanvas.enabled = false;
        }
        StartCoroutine(TextFadeOut(nBubble, MessageTimeout));
    }



    private IEnumerator TextFadeOut(GameObject text, float time)
    {
       // Debug.Log("waiting for " + time);
        yield return new WaitForSeconds(time);
      //  Debug.Log("Removing");
        if(text == Messages.Peek())
        {
            GameObject bubble  = Messages.Dequeue();
            bubble.SetActive(false);
            FreeBubbles.Add(bubble);
            if (Messages.Count == 0)
                SpeechCanvas.enabled = false;
        }
        /*
        AddedMessages.Remove(text);

        Destroy(text.gameObject);

        if (AddedMessages.Count == 0)
            SpeechCanvas.enabled = false;*/
    }

    private IEnumerator TextFadeOut(float time)
    {
       // Debug.Log("waiting for time " + time);
        yield return new WaitForSeconds(time);
       // Debug.Log("hiding");
        SpeechCanvas.enabled = false;

    }
}
