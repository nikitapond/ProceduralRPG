using UnityEngine;
using UnityEditor;

public class SpellManager : MonoBehaviour
{

    public static SpellManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void DestroyBeam(LoadedBeam beam)
    {
        if (beam != null && beam.gameObject != null)
            Destroy(beam.gameObject);
    }
    public LoadedBeam CreateNewBeam(Entity entity, Beam beam, Vector3 target)
    {
        GameObject beamObj = Instantiate(beam.GenerateBeamObject());
        LoadedBeam loadedBeam = beamObj.GetComponent<LoadedBeam>();

        loadedBeam.transform.parent = entity.GetLoadedEntity().transform;
        loadedBeam.transform.localPosition = Vector3.up * 1.5f;
        loadedBeam.CreateBeam(entity, target, beam);
        return loadedBeam;
    }

    public void AddNewProjectile(Vector3 position, Vector3 direction, Projectile projectile, Entity source = null, float yAxisRotate = 0)
    {
        Debug.Log("Projectile added");
        GameObject pro = Instantiate(projectile.GenerateProjectileObject());
        pro.transform.Rotate(new Vector3(0, yAxisRotate, 0));
        pro.transform.parent = transform;
        LoadedProjectile l = pro.AddComponent<LoadedProjectile>();
        l.CreateProjectile(position, direction, projectile, source);
        if (source != null)
        {
        }

        //LoadedProjectiles.Add(l);
    }


    public void DestroyProjectile(LoadedProjectile proj)
    {
        //LoadedProjectiles.Remove(proj);
        Destroy(proj.gameObject);
    }
}