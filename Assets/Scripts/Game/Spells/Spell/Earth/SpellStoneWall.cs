using UnityEngine;
using UnityEditor;
[System.Serializable]
public class SpellStoneWall : HoldSpell
{
    public override float ManaCost => 10;


    public override string Description => "Builds a stone wall";

    public override float XPGain => 1;
    public override Spells ID => Spells.StoneWall;

    public override SpellCombatType SpellCombatType => SpellCombatType.DEFENSIVE;

    public override string Name => "Stone Wall";

    private MarchingCubesStoneWall mcw;
    public override void SpellEnd(SpellCastData data)
    {
        //For this spell, we shouldn't need to do anything...
        IsCast = false;

    }
    /// <summary>
    /// Start of spell checks if target position is existing wall object, if it is we 
    /// define this as our wall, and continue to build on it
    /// </summary>
    /// <param name="data"></param>
    public override void SpellStart(SpellCastData data)
    {
        if (!IsPointFree(data.Target))
            return;
        //Get objects in raycast of spell
        Vector3 disp = data.Target - data.Source.GetLoadedEntity().transform.position;
        RaycastHit[] hit = Physics.RaycastAll(data.Source.GetLoadedEntity().transform.position, disp, disp.magnitude);
        //iterate all objects hit by the spell ray
        foreach(RaycastHit hit_ in hit)
        {
            MarchingCubesStoneWall hit_wall = hit_.collider.gameObject.GetComponent<MarchingCubesStoneWall>();
            //If the hit object is a valid wall
            if(hit_wall != null)
            {
                //Then set the wall here to that wall, and buid onto it
                mcw = hit_wall;
                mcw.BuildWall(data.Target);
                return;
            }

        }
        //If no wall in the way, create a new one
        GameObject obj = GameManager.WorldManager.ForceInstanse(ResourceManager.GetMiscPrefab("march_sone_wall"));
        
        mcw = obj.GetComponent<MarchingCubesStoneWall>();
        mcw.Initiate(data.Target);
        mcw.BuildWall(data.Target);
        IsCast = true;
    }

    /// <summary>
    /// Checks if the target position for spell is valid
    /// i.e. - the position has no entity or world object in it
    /// </summary>
    /// <param name="data"></param>
    public override void SpellUpdate(SpellCastData data)
    {
        if (IsPointFree(data.Target))
        {
            //Add target point to wall
            mcw.BuildWall(data.Target);
        }
       
    }

    private bool IsPointFree(Vector3 point)
    {
        //Get objects at target position
        Collider[] cols = Physics.OverlapSphere(point, 1);
        foreach (Collider c in cols)
        {
            //If there is an entity or world object there, we cannot add to the wall
            if (c.gameObject.GetComponent<LoadedEntity>() != null)
                return false;
            if (c.gameObject.GetComponent<WorldObject>() != null)
                return false;
        }
        return true;
    }
}