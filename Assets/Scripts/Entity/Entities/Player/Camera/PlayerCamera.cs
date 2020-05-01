using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class PlayerCamera : MonoBehaviour
{
    public Vector3 Dif { get; private set; }
    public Camera Camera { get; private set; }
    public PlayerManager PlayerManager { get; private set; }


    // Camera coordinates relative to player are based on spherical polar coordinates.
    public float Theta { get; private set; }
    public float R { get; private set; }
    public float Phi { get; private set; }

    public CameraController CameraController;

    public void SetFirstPersonCamera()
    {

        if(CameraController != null)
        {
            Destroy(CameraController);
        }
        CameraController = gameObject.AddComponent<FirstPersonCC>();

    }

    public void SetCameraController()
    {
        CameraController oldCC = GetComponent<CameraController>();
    }


    /// <summary>
    /// Initialises the camera, sets the initial relative coordinates and collects
    /// various game object and component references.
    /// </summary>
    private void Awake()
    {
        Camera = GetComponent<Camera>();
        if (TestMain.TEST_MODE)
        {
            PlayerManager = GameObject.Find("PlayerManager").gameObject.GetComponent<PlayerManager>();
        }
        else
        {
            PlayerManager = transform.parent.gameObject.GetComponent<PlayerManager>();

        }
        Theta = 0;
        R = 15;
        Phi = 45;
        Dif = CartesianCoord();
        SetFirstPersonCamera();
    }

    public void ClockwiseMove(float amount)
    {
        Theta += amount;
    }

    private void Update()
    {
        CameraController.Update();
        /*
        if(PlayerManager.Player != null)
        {
            Dif = CartesianCoord();
            transform.position = PlayerManager.LoadedPlayer.transform.position + Dif;
            transform.LookAt(PlayerManager.LoadedPlayer.transform);
        }*/
    }

    /// <summary>
    /// Calculates the cartesian coordinates based on the spherical coordinates
    /// saved (Theta,R,Phi)
    /// </summary>
    /// <returns></returns>
    private Vector3 CartesianCoord()
    {
        float x = R * Mathf.Sin(Phi * Mathf.Deg2Rad) * Mathf.Cos(Theta * Mathf.Deg2Rad);

        float z = R * Mathf.Sin(Phi * Mathf.Deg2Rad) * Mathf.Sin(Theta * Mathf.Deg2Rad);
        float y = R * Mathf.Cos(Phi * Mathf.Deg2Rad);
        return new Vector3(x, y, z);
    }



}