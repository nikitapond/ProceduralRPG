using UnityEngine;
using UnityEditor;

public class LoadedProjectile : MonoBehaviour
{
    public Projectile Projectile { get; private set; }
    private Entity Source;
    private Rigidbody RBody;
    /// <summary>
    /// Sets the projectile to this loaded projectile
    /// </summary>
    /// <param name="proj"></param>
    public void CreateProjectile(Vector3 position, Vector3 direction, Projectile proj, Entity source=null)
    {
        Projectile = proj;
        transform.position = position;
        RBody = GetComponent<Rigidbody>();
        if (RBody == null)
            RBody = gameObject.AddComponent<Rigidbody>();
        RBody.useGravity = false;
        RBody.velocity = new Vector3(direction.x, direction.y, direction.z).normalized * proj.ProjectileSpeed;
        transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(direction.z, direction.x) * Mathf.Deg2Rad, Vector3.forward);
        Source = source;
    }

    /// <summary>
    /// Called by unity engine when projectile collides with another object.
    /// Triggers <see cref="Projectile.InternalOnCollision(Collision)"/>
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "MainCamera")
            return;
        if(Source != null)
        {
            LoadedEntity LoadedEntity = other.gameObject.GetComponent<LoadedEntity>();
            if (LoadedEntity == null)
                LoadedEntity = other.gameObject.GetComponentInParent<LoadedEntity>();
            if(LoadedEntity != null)
            {
                if (LoadedEntity.Entity.Equals(Source))
                    return;
            }
        }
        Projectile.InternalOnCollision(other);

        if (Projectile.DestroyOnCollision)
            SpellManager.Instance.DestroyProjectile(this);
    }

}