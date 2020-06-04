using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class EconomyTest : MonoBehaviour
{

    public GameObject MeshObject;

    public GameObject GridPoint;
    public RawImage Image;
    public RawImage KingdomMap;


    public Text InfoText;
    GameGenerator2 GameGen;
    void Start()
    {
        GameGen = new GameGenerator2(0);
        GameGen.GenerateWorld();

        Image.texture = GameGen.TerGen.ToTexture();
        GenMeshes();
        KingdomMap.texture = GameGen.KingdomGen.ToTexture();
        foreach (GridPoint gp in GameGen.GridPlacement.GridPoints)
        {
            if (gp == null)
                continue;
            Vec2i pos = gp.ChunkPos;
            GameObject t = Instantiate(GridPoint);
            t.transform.SetParent(Image.transform, false);
            t.transform.localPosition = new Vector3(pos.x - World.WorldSize/2, pos.z-World.WorldSize/2, 0);
            (t.GetComponent<GridPointTest>()).SetPoint(gp);
            //Gizmos.DrawCube(new Vector3(pos.x, pos.z, -1), Vector3.one);
        }
        
    }


    private void Update()
    {
        Vector3 p = Input.mousePosition;
        int x = (int)p.x;
        int z = (int)p.y;

        Vector3[] corn = new Vector3[4];
        Image.rectTransform.GetWorldCorners(corn);
        x = x - (int)corn[0].x;
        z = (z - (int)corn[0].y);

        if (x > 0 && z>0 && x<World.WorldSize && z < World.WorldSize)
        {
            ChunkBase2 current = GameGen.TerGen.ChunkBases[x, z];
            InfoText.text = ChunkBaseDetail(current);
        }


    }



    private string ChunkBaseDetail(ChunkBase2 b)
    {
        if (b == null)
            return "";
        string output = "Chunk: " + b.Pos + "\n";
        output += "Biome: " + b.Biome + "\n";
        output += "Height: " + b.Height + "\n";
        foreach(ChunkResource res in MiscUtils.GetValues<ChunkResource>())
        {
            output += res + ": " + b.GetResourceAmount(res) + "\n";
        }
        output += "riv_height: " + GameGen.TerGen.RiverGen.GetValAtChunk(b.Pos) + "\n";
        return output;
    }


    private void GenMeshes()
    {

        List<Vector3> vert = new List<Vector3>();
        List<int> tri = new List<int>();
        List<Color> col = new List<Color>();
        int size = 256;
        Vec2i[] quad = new Vec2i[] { new Vec2i(0,0), new Vec2i(size,0), new Vec2i(size*2, 0), new Vec2i(size*3, 0),
                                     new Vec2i(0,size), new Vec2i(size,size), new Vec2i(size*2, size), new Vec2i(size*3, size),
                                    new Vec2i(0,2*size), new Vec2i(size,2*size), new Vec2i(size*2, 2*size), new Vec2i(size*3, 2*size),
                                    new Vec2i(0,3*size), new Vec2i(size,3*size), new Vec2i(size*2, 3*size), new Vec2i(size*3, 3*size)};

        foreach(Vec2i q in quad)
        {
            vert.Clear();
            tri.Clear();
            col.Clear();

            for (int x=0; x<size; x++)
            {
                for(int z=0; z<size; z++)
                {
                    int x_ = x + q.x;
                    int z_ = z + q.z;
                    
                    vert.Add(new Vector3(x, GameGen.TerGen.ChunkBases[x_, z_].Height, z));
                    col.Add(GameGen.TerGen.ChunkBases[x_, z_].GetMapColor());
                }
            }
            int verts = 0;
            int tris = 0;
            int size2 = size - 1;
            for (int z = 0; z < size2; z++)
            {
                for (int x = 0; x < size2; x++)
                {
                    tri.Add(verts + 1);
                    tri.Add(verts + size2 + 1);
                    tri.Add(verts + 0);


                    tri.Add(verts + size2 + 2);
                    tri.Add(verts + size2 + 1);
                    tri.Add(verts + 1);


                    verts++;
                    tris += 6;
                }
                verts++;
            }

            GameObject m = Instantiate(MeshObject);
            m.transform.parent = transform;
            m.transform.localPosition = new Vector3(q.x, 0, q.z);
            Mesh mesh = m.GetComponent<MeshFilter>().mesh;
            mesh.vertices = vert.ToArray();
            mesh.triangles = tri.ToArray();
            mesh.colors = col.ToArray();
            mesh.RecalculateNormals();
        }

    }
    
}
