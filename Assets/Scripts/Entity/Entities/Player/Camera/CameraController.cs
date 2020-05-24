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

public class ThirdPersonCC : CameraController, IGamePauseEvent
{

    private bool Pause;

    private float Zoom;

    private readonly float MinDist = 8;
    private readonly float MaxDist = 15;
    private readonly float MaxPhi = 85;
    private readonly float MinPhi = 10;


    private float Theta, Phi,R;
    private float Sensitivity = 20;
    private float ScrollSense =0.01f;
    void Start()
    {
        EventManager.Instance.AddListener(this);
        Cursor.lockState = CursorLockMode.None;
        Theta = 0;

    }


    public override GameObject GetViewObject()
    {
        return null;
    }

    public override Vector3 GetWorldLookPosition()
    {
        return Vector3.zero;
    }

    public override void Update()
    {

        if (Input.GetKey(KeyCode.Q))
        {
            Theta -= Sensitivity * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            Theta += Sensitivity * Time.deltaTime;
            Debug.Log(Theta);
        }
        if(Input.mouseScrollDelta != Vector2.zero)
        {
            Zoom -= Input.mouseScrollDelta.y * ScrollSense;
            Zoom = Mathf.Clamp(Zoom, 0, 1);
        }
        CalculatePhi_R();
        Debug.Log(Phi);
        transform.position = PlayerManager.Player.GetLoadedEntity().transform.position + SphericalPolarToCart(R, Theta, Phi);
        transform.LookAt(PlayerManager.Player.GetLoadedEntity().transform);

    }

    private void CalculatePhi_R()
    {
        Phi = Mathf.Lerp(MinPhi, MaxPhi, 1-Zoom);
        R = Mathf.Lerp(MinDist, MaxDist, Zoom);

    }
    /// <summary>
    /// Turns the point defined by (r,theta,phi) to cartesian (x,y,z)
    /// Theta and phi should be in degrees
    /// </summary>
    /// <param name="r"></param>
    /// <param name="theta"></param>
    /// <param name="phi"></param>
    /// <returns></returns>
    private Vector3 SphericalPolarToCart(float r, float theta, float phi)
    {
        float thetaRad = Mathf.Deg2Rad * theta;
        float phiRad = Mathf.Deg2Rad * phi;
        float x = r * Mathf.Sin(phiRad) * Mathf.Cos(thetaRad);
        float z = r * Mathf.Sin(phiRad) * Mathf.Sin(thetaRad);
        float y = r *Mathf.Cos(phiRad);
        return new Vector3(x, y, z);
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
}