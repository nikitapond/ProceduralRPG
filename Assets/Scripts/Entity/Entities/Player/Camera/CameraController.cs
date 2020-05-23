using UnityEngine;
using UnityEditor;

public abstract class CameraController : MonoBehaviour
{

    protected PlayerManager PlayerManager;
    protected Camera Camera;

    private void Awake()
    {
        PlayerManager = GetComponentInParent<PlayerManager>();
        Camera = GetComponent<Camera>();
    }

    public abstract void Update();

    /// <summary>
    /// Returns the object that the player is selecting.
    /// </summary>
    /// <returns></returns>
    public abstract GameObject GetViewObject();

    /// <summary>
    /// Returns the world coordinate of the current ground point the player is looking at.
    /// </summary>
    /// <returns></returns>
    public abstract Vector3 GetWorldLookPosition();

}

public class FirstPersonCC : CameraController, IGamePauseEvent
{


    private float Theta, Phi;
    private float Sensitivity=0.5f;

    private bool Pause;

    private void Start()
    {

        //Theta = PlayerManager.Player.LookAngle;
        Phi = 0;
        EventManager.Instance.AddListener(this);
        Cursor.lockState = CursorLockMode.Locked;

    }

    public override void Update()
    {
        if (Pause)
            return;
        Theta += Input.GetAxis("Mouse X") * Sensitivity;
        Phi   += Input.GetAxis("Mouse Y") * Sensitivity;
        transform.position = PlayerManager.LoadedPlayer.GetComponent<LoadedEquiptmentManager>().HEAD.transform.position;
        //transform.position = PlayerManager.LoadedPlayer.transform.position + Vector3.up*1.5f;
        transform.rotation = Quaternion.Euler(-Phi, Theta, 0);
        PlayerManager.Player.SetLookAngle(Theta);
        PlayerManager.LoadedPlayer.transform.rotation = Quaternion.Euler(0, Theta, 0);
    }

    public void GamePauseEvent(bool pause)
    {
        Debug.Log("test");
        Pause = pause;
        if (pause)
            Cursor.lockState = CursorLockMode.None;
        else
            Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDisable()
    {
        EventManager.Instance.RemoveListener(this);
    }

    /// <summary>
    /// Returns the GameObject the player is directly looking at
    /// </summary>
    /// <returns></returns>
    public override GameObject GetViewObject()
    {
        Ray ray = Camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit))
        {
            return hit.collider.gameObject;
        }
        return null;
    }


    public override Vector3 GetWorldLookPosition()
    {
        Ray ray = Camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Phi > 0)
        {
            return transform.position + ray.direction * 20;
        }
        else
        {
            RaycastHit[] hit = Physics.RaycastAll(ray.origin, ray.direction, 20);
            foreach(RaycastHit hit_ in hit)
            {
                if (hit_.collider.gameObject.tag == "Ground")
                    return hit_.point;
            }
        }
        return Vector3.zero;
    }

    void OnDrawGizmos()
    {
        Vector3 wPos = GetWorldLookPosition();
        if (wPos == Vector3.zero)
            return;
        Color prev = Gizmos.color;
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(wPos, 0.1f);

        Gizmos.color = prev;
    }
}