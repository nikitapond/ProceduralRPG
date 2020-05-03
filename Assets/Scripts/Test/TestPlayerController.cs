using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayerController : MonoBehaviour
{
    private Camera Camera;
    private CharacterController Controller;
    // Start is called before the first frame update
    void Start()
    {
        Controller = GetComponent<CharacterController>();
        Camera = GetComponentInChildren<Camera>();
    }
    
    float angle;
    private float Theta, Phi;
    private float Sensitivity = 5;
    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float ax1 = x*Mathf.Cos(Theta * Mathf.Deg2Rad) + z*Mathf.Sin(Theta * Mathf.Deg2Rad);
        float ax2 = -x * Mathf.Sin(Theta * Mathf.Deg2Rad) + z * Mathf.Cos(Theta * Mathf.Deg2Rad);


        Controller.SimpleMove(new Vector3(ax1, 0, ax2) * 10); ;


        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;

        }

        if(Cursor.lockState == CursorLockMode.Locked)
        {
            Theta += Input.GetAxis("Mouse X") * Sensitivity;
            Phi += Input.GetAxis("Mouse Y") * Sensitivity;
            //transform.position = PlayerManager.LoadedPlayer.transform.position + Vector3.up*1.5f;
            transform.rotation = Quaternion.Euler(0, Theta, 0);
            Camera.transform.localRotation = Quaternion.Euler(-Phi, 0, 0);
        }

        if (Input.GetMouseButton(0))
        {
            GameObject obj = GetViewObject();
            if(obj != null)
            {
                WorldObject wObj = obj.GetComponent<WorldObject>();
                if(wObj != null)
                    wObj.OnEntityInteract(null);
            }
        }


    }

    public GameObject GetViewObject()
    {
        Ray ray = Camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.gameObject;
        }
        return null;
    }

}
