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

    /// <summary>
    /// We decide the positions of the kingdom capitals, and then claim 
    /// chunks for their territory
    /// </summary>
    public void ClaimKingdomChunks(int kingdomCount=4)
    {
        ClaimedChunks = new Dictionary<Kingdom, List<Vec2i>>();

        Vec2i[] caps = ChooseStartChunks(4, 250);
        Kingdoms = new Kingdom[4];
        for(int i=0; i<4; i++)
        {
            Kingdoms[i] = new Kingdom("king " + i, caps[i]);
            GameGen.World.AddKingdom(Kingdoms[i]);
            int ip1 = (i + 1) % 4;
            //GameGen.GridPlacement.GetNearestPoint(caps[i]).HasSettlement = true;
            //BuildRoad(GameGen.GridPlacement.GetNearestPoint(caps[i]), GameGen.GridPlacement.GetNearestPoint(caps[ip1]));
        }
        ClaimChunks(Kingdoms);
        SetKingdomCapitalGridPoints();
    }
    /// <summary>
    /// Calculates information for each kingdom, such as its personality
    /// TODO - add kingdom 'types' such as dessert, horse riders, idk what else
    /// </summary>
    public void GenerateKingdoms()
    {
        foreach(Kingdom k in Kingdoms)
        {
            k.SetAggression(GenRan.Random());
        }
    }


    private void SetKingdomCapitalGridPoints()
    {
        foreach(Kingdom k in Kingdoms)
        {
            //Find the nearest grid point
            GridPoint gp = GameGen.GridPlacement.GetNearestPoint(k.CapitalChunk);
            //move to the exact capital chunk
            //gp.ChunkPos = k.CapitalChunk;
            k.CapitalChunk = gp.ChunkPos;
            gp.IsCapital = true;

        }
    }

   

    private void ClaimChunks(Kingdom[] kings)
    {

        int[,] claimedChunkKingdomIDs = new int[World.WorldSize, World.WorldSize];
        bool[,] obstruction = new bool[World.WorldSize, World.WorldSize];

        for(int x=0; x<World.WorldSize; x++)
        {
            for(int z=0; z<World.WorldSize; z++)
            {
                claimedChunkKingdomIDs[x, z] = -1;
                if (GameGen.TerGen.ChunkBases[x, z].Biome is ChunkBiome.ocean)
                {
                    obstruction[x, z] = true;
                }
                else if (GameGen.TerGen.ChunkBases[x, z].ChunkFeature is ChunkRiverNode)
                {
                    obstruction[x, z] = true;
                }
                //else if (GameGen.TerGen.EvilDragonMountainPeak.QuickDistance(new Vec2i(x, z)) < 128 * 128)
                //    obstruction[x, z] = true;
            }
        }

        Dictionary<Kingdom, List<Vec2i>> freeBorder = new Dictionary<Kingdom, List<Vec2i>>();

        foreach (Kingdom k in kings)
        {
            ClaimedChunks.Add(k, new List<Vec2i>(250000));
            ClaimedChunks[k].Add(k.CapitalChunk);
            freeBorder.Add(k, new List<Vec2i>(2000));
            freeBorder[k].Add(k.CapitalChunk);
        }

        bool shouldRun = true;
        while (shouldRun)
        {
            shouldRun = false;
            foreach (Kingdom k in kings)
            {
                if (freeBorder[k].Count == 0)
                    continue;
                shouldRun = true;
                //We randomly select a border point and remove it from the list
                Vec2i curBorder = GenRan.RandomFromList(freeBorder[k]);
                freeBorder[k].Remove(curBorder);

                foreach (Vec2i v in Vec2i.QUAD_DIR)
                {
                    //We iterate all 8 surrounding chunks
                    Vec2i p = curBorder + v;

                    //if there is an obstruction we cannot claim the chunk
                    if (obstruction[p.x, p.z])
                        continue;
                    if (claimedChunkKingdomIDs[p.x, p.z] == -1)
                    {
                        claimedChunkKingdomIDs[p.x, p.z] = k.KingdomID;
                        freeBorder[k].Add(p);
                        ClaimedChunks[k].Add(p);
                    }

                }


            }
        }

        foreach(Kingdom k in Kingdoms)
        {
            k.ClaimedChunks = ClaimedChunks[k];
        }
        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                GameGen.TerGen.ChunkBases[x, z].KingdomID = claimedChunkKingdomIDs[x,z];

            }
        }
        return;
        
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

    struct KingdomCapitalRaycast
    {
    }
    public Vec2i[] ChooseStartChunks(int count, int minSep)
    {
        //The angle between map middle that each capital should approximately 
        //be seperated by
        float thetaSep = 360f / count;

        float thetaOffset = GenRan.Random(0, 90);
        Vector2 middle = new Vector2(World.WorldSize / 2, World.WorldSize / 2);
        Vec2i[] caps = new Vec2i[count];
        //We iterate each of the kingdom capital positions we wish to calculate
        for (int i=0; i<count; i++)
        {
            //Theta compared to (1,0) anticlockwise
            float theta = thetaOffset + thetaSep * i;
            float dx = Mathf.Cos(theta * Mathf.Deg2Rad);
            float dz = Mathf.Sin(theta * Mathf.Deg2Rad);
            Vector2 delta = new Vector2(dx, dz);
            //We start at middle
            Vector2 current = middle;

            int minLength = 32;
            int maxLength = -1;

            //iterate out
            for(int l= minLength; l<World.WorldSize/2; l++)
            {
                current = middle + delta * l;
                Vec2i curChunk = Vec2i.FromVector2(current);
                ChunkBase2 cb = GameGen.TerGen.ChunkBases[curChunk.x, curChunk.z];
                if(cb.Biome == ChunkBiome.ocean)
                {
                    //When we reach the ocean, we define this as the max distance away
                    maxLength = l - 1;
                    break;
                }
            }
            //Capital is random point between min length, and max length
            Vec2i capPoint = Vec2i.FromVector2(middle + GenRan.RandomInt(minLength, maxLength) * delta);
            caps[i] = capPoint;
        }

        return caps;


        

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

}