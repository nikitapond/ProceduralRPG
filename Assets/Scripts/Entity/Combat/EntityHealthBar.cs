﻿using System.Collections;
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
        canvas.worldCamera = PlayerManager.Instance.PlayerCamera;
    }

    public void SetHealthPct(float pct)
    {

        image.fillAmount = pct;
        if(pct == 1)
        {
            canvas.enabled = false;
        }
        else
        {
            canvas.enabled = true;
        }
        transform.LookAt(PlayerManager.Instance.PlayerCamera.transform);
        transform.Rotate(new Vector3(0, 180, 0));
    }
}
