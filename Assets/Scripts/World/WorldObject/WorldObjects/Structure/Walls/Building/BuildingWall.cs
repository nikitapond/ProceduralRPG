using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A building wall 
/// </summary>
[System.Serializable]
public class BuildingWall : WorldObjectData
{


    private string WallBase;
    private WorldObjectData WallObject;
    private int WallObjectHeight;
    private int WallObjectPosition;
    private int WallHeight;
    private Roof Roof;
    public BuildingWall(Vec2i worldPosition, string wallBase, int wallHeight, WorldObjectData wallobject=null, int wallObjectPosition=0, int wallObjectHeight=1) : base(worldPosition, null, null)
    {
        WallBase = wallBase;
        WallObject = wallobject;
        WallObjectHeight = wallObjectHeight;
        WallObjectPosition = wallObjectPosition;
        WallHeight = wallHeight;
    }

    public void AddRoof(Roof roof)
    {
        Roof = roof;
    }

    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        WorldObject empty_base = new EmptyObjectBase(WorldPosition).CreateWorldObject(transform);

        if(WallObject == null)
        {

            WorldObject wall_base = base.CreateWorldObject(empty_base.transform);
            wall_base.transform.localScale = new Vector3(1, WallHeight, 1);
            wall_base.transform.localPosition = Vector3.zero;



            Mesh m = wall_base.GetComponentInChildren<MeshFilter>().mesh;
            float height = WallHeight;
            Vector2 scale = new Vector2(1, height);
            Vector2[] uv = new Vector2[m.uv.Length];
            for (int i=0; i<m.uv.Length; i++)
            {
                uv[i] = Vector2.Scale(m.uv[i], scale);
            }
            m.uv = uv;


            wall_base.GetComponentInChildren<MeshRenderer>().material = ResourceManager.GetMaterial(WallBase);
            //objBase.GetComponentInChildren<MeshFilter>().mesh.uv *= 2;
        }
        else if(WallObjectPosition == 0)
        {
            //Create both objects
            WorldObject wall_base = base.CreateWorldObject(empty_base.transform);





            WorldObject wallObject = WallObject.CreateWorldObject(empty_base.transform);
            wallObject.transform.localPosition = Vector3.zero;
            //Define main wall to exist above addition object
            wall_base.transform.position = new Vector3(0, WallObjectHeight, 0);
            wall_base.transform.localScale = new Vector3(1, WallHeight - WallObjectHeight, 1);


            Mesh m = wall_base.GetComponentInChildren<MeshFilter>().mesh;
            float height = WallHeight;
            Vector2 scale = new Vector2(1, height);
            Vector2[] uv = new Vector2[m.uv.Length];
            for (int i = 0; i < m.uv.Length; i++)
            {
                uv[i] = Vector2.Scale(m.uv[i], scale);
            }
            m.uv = uv;

        }
        else
        {
            WorldObject wall_base = base.CreateWorldObject(empty_base.transform);
            wall_base.transform.localScale = new Vector3(1, WallObjectPosition, 1);
            wall_base.transform.localPosition = Vector3.zero;

            wall_base.GetComponentInChildren<MeshRenderer>().material = ResourceManager.GetMaterial(WallBase);

            WorldObject wallObject = WallObject.CreateWorldObject(empty_base.transform);
            wallObject.transform.localPosition = new Vector3(0, WallObjectPosition, 0);

            WorldObject aboveWall = base.CreateWorldObject(empty_base.transform);
            aboveWall.transform.localPosition = new Vector3(0, WallObjectPosition + WallObjectHeight, 0);
            aboveWall.transform.localScale = new Vector3(1, WallHeight - (WallObjectPosition + WallObjectHeight), 1);
            aboveWall.GetComponentInChildren<MeshRenderer>().material = ResourceManager.GetMaterial(WallBase);



            Mesh m = wall_base.GetComponentInChildren<MeshFilter>().mesh;
            Mesh m2 = aboveWall.GetComponentInChildren<MeshFilter>().mesh;

            Vector2 scale = new Vector2(1, WallObjectPosition);
            Vector2 scale2 = new Vector2(1, WallHeight - (WallObjectPosition + WallObjectHeight));

            Vector2[] uv = new Vector2[m.uv.Length];
            Vector2[] uv2 = new Vector2[m.uv.Length];

            for (int i = 0; i < m.uv.Length; i++)
            {
                uv[i] = Vector2.Scale(m.uv[i], scale);
                uv2[i] = Vector2.Scale(m2.uv[i], scale2);
            }
            m.uv = uv;
            m2.uv = uv2;


        }

        if(Roof != null)
        {

            WorldObject roofObj = Roof.CreateWorldObject(empty_base.transform);
            roofObj.transform.localPosition = new Vector3(0, WallHeight, 0);
        }

        foreach(Transform tran in empty_base.transform)
        {
            tran.gameObject.layer = 8;
        }

        return empty_base;
    }
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new BuildingWall(pos, this.WallBase, this.WallHeight, this.WallObject, this.WallObjectPosition, this.WallObjectHeight);

    }
    public override WorldObjects ObjID => WorldObjects.WALL;

    public override string Name => "Building Wall";
}