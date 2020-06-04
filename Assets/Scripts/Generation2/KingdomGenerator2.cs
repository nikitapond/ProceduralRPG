using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class KingdomGenerator2
{
    private GameGenerator2 GameGen;
    private GenerationRandom GenRan;


    public Kingdom[] Kingdoms;

    public KingdomGenerator2(GameGenerator2 gameGen)
    {
        GameGen = gameGen;
        GenRan = new GenerationRandom(gameGen.Seed);
    }

    public Dictionary<Kingdom, List<Vec2i>> ClaimedChunks;
    public void ClaimKingdomChunks()
    {
        ClaimedChunks = new Dictionary<Kingdom, List<Vec2i>>();

        Vec2i[] caps = ChooseStartChunks(4, 250);
        Kingdoms = new Kingdom[4];
        for(int i=0; i<4; i++)
        {
            Kingdoms[i] = new Kingdom("king " + i, caps[i]);
            int ip1 = (i + 1) % 4;
            //GameGen.GridPlacement.GetNearestPoint(caps[i]).HasSettlement = true;
            //BuildRoad(GameGen.GridPlacement.GetNearestPoint(caps[i]), GameGen.GridPlacement.GetNearestPoint(caps[ip1]));
        }
        ClaimChunks(Kingdoms);
    }

    private void ClaimChunks(Kingdom[] kings)
    {
        foreach(Kingdom k in kings)
        {
            ClaimedChunks.Add(k, new List<Vec2i>());
        }
        for(int x=0; x<World.WorldSize; x++)
        {
            for(int z=0; z<World.WorldSize; z++)
            {
                ChunkBase2 cb = GameGen.TerGen.ChunkBases[x, z];

                
                if(cb.Biome != ChunkBiome.ocean)
                {
                    int lowestDist = -1;
                    int curKing = -1;
                    for (int i = 0; i < kings.Length; i++)
                    {
                        int dist = new Vec2i(x, z).QuickDistance(kings[i].CapitalChunk);
                        if (lowestDist == -1 || dist < lowestDist)
                        {
                            lowestDist = dist;
                            curKing = i;
                        }
                    }
                    ClaimedChunks[kings[curKing]].Add(new Vec2i(x, z));
                    GameGen.TerGen.ChunkBases[x, z].KingdomID = curKing;

                }
                


            }
        }

        for(int x=0; x<GridPlacement.GridSize; x++)
        {
            for(int z=0; z<GridPlacement.GridSize; z++)
            {
                GridPoint p = GameGen.GridPlacement.GridPoints[x,z];
               // Debug.Log(p);
                if(p != null)
                {
                    int id = GameGen.TerGen.ChunkBases[p.ChunkPos.x, p.ChunkPos.z].KingdomID;
                   /// Debug.Log(kings[GameGen.TerGen.ChunkBases[x, z].KingdomID]);
                    p.Kingdom = kings[GameGen.TerGen.ChunkBases[p.ChunkPos.x, p.ChunkPos.z].KingdomID];
                }
            }
        }
    }


    public Texture2D ToTexture()
    {
        Texture2D tex = new Texture2D(World.WorldSize, World.WorldSize);

        Dictionary<int, Color> kingCol = new Dictionary<int, Color>();
        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                ChunkBase2 cb = GameGen.TerGen.ChunkBases[x, z];
                if(cb.KingdomID != -1)
                {
                    if (!kingCol.ContainsKey(cb.KingdomID))
                    {
                        kingCol.Add(cb.KingdomID, GenRan.RandomColor(0.7f));
                    }
                    tex.SetPixel(x, z, kingCol[cb.KingdomID]);
                }
            }
        }

                
        tex.Apply();
        return tex;
    }

    public Vec2i[] ChooseStartChunks(int count, int minSep)
    {
        Vec2i[] caps = new Vec2i[count];

        Vec2i[] dirs = new Vec2i[] { new Vec2i(1, 1), new Vec2i(-1, 1), new Vec2i(-1, -1), new Vec2i(1, -1) };
        Vec2i mid = new Vec2i(World.WorldSize / 2, World.WorldSize / 2);

        for(int i=100; i<400; i++)
        {
            for(int j=0; j<dirs.Length; j++)
            {
                if(caps[j] == null)
                {
                    Vec2i p = mid + dirs[j] * i;
                    ChunkBase2 b = GameGen.TerGen.ChunkBases[p.x, p.z];
                    if (b.Biome == ChunkBiome.ocean)
                    {
                        Vec2i p_ = mid + dirs[j] * (int)(0.75f * i);
                        caps[j] = p_;

                        GridPoint gp = GameGen.GridPlacement.GetNearestPoint(p_);

                    }
                }
                
            }
        }
        return caps;

        for (int i=0; i<count; i++)
        {
            bool validPoint = false;
            while (!validPoint)
            {
                Vec2i pos = GenRan.RandomVec2i(0, World.WorldSize-1);
                GridPoint p = GameGen.GridPlacement.GetNearestPoint(pos);
                if(p != null && p.IsValid)
                {
                    if (i == 0)
                    {
                        caps[0] = p.ChunkPos;
                        validPoint = true;
                    }
                    else
                    {
                        int minDist = -1;
                        for(int j=0; j<i; j++)
                        {
                            int sqrDist = p.ChunkPos.QuickDistance(caps[j]);
                            if(minDist == -1 || sqrDist < minDist)
                            {
                                minDist = sqrDist;
                            }
                        }
                        if(minDist < minSep * minSep)
                        {
                            caps[i] = p.ChunkPos;
                            validPoint = true;
                        }
                    }
                    
                }
              
             
            }
            Debug.Log(caps[i]);
            
        }
        return caps;
    }

    public void BuildRoad(GridPoint a, GridPoint b)
    {
        List<GridPoint> path = GameGen.GridPlacement.ConnectPoints(a, b);
        path[path.Count - 1].ChunkRoad = new ChunkRoad(ChunkRoad.RoadType.Paved);
        Debug.Log(path.Count);
        for(int i=0; i<path.Count-1; i++)
        {
            path[i].ChunkRoad = new ChunkRoad(ChunkRoad.RoadType.Paved);
            //GameGen.GridPlacement.GridPoints[path[i].GridPos.x, path[i].GridPos.z].ChunkRoad = new ChunkRoad(ChunkRoad.RoadType.Paved);
            Vec2i v1 = path[i].ChunkPos;
            Vec2i v2 = path[i + 1].ChunkPos;
            LineI li = new LineI(v1, v2);
            foreach(Vec2i v in li.ConnectPoints())
            {
                GameGen.TerGen.ChunkBases[v.x, v.z].SetChunkFeature(new ChunkRoad(ChunkRoad.RoadType.Paved));
            }
        }

        
    }

}