using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntityHealthBar : MonoBehaviour
{

    public Image image;
    public Canvas canvas;
    void Start()
    {
        SetHealthPct(1);
        if (PlayerManager.Instance == null)
            return;
        canvas.worldCamera = PlayerManager.Instance.PlayerCamera;
    }

    public void SetHealthPct(float pct)
    {
        if (canvas == null || PlayerManager.Instance == null)
            return;
        image.fillAmount = pct;
        if(pct == 1)
        {
            canvas.gameObject.SetActive(false);
        }
        else
        {
            canvas.gameObject.SetActive(true);
        }
        transform.LookAt(PlayerManager.Instance.PlayerCamera.transform);
        transform.Rotate(new Vector3(0, 180, 0));
    }
}
