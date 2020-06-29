using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class TerrainGenerator2
{

    /// <summary>
    /// The heightmap array represents the height of each chunk in the world
    /// We have a boundry at each edge of the map, this isto ensure that domain warping <see cref="DomainWarp"/>
    /// does not leave ugly edges
    /// This means that the HeightMap array is offset by this amount.
    /// For example, the chunk (0,0) will be at index (32,32) in this array
    /// </summary>
    public readonly int HeightMapBoundry = 32;
    /// <summary>
    /// The amplitude of domain warping <see cref="DomainWarp"/>
    /// This value should NOT be larger than <see cref="HeightMapBoundry"/>
    /// </summary>
    public readonly int DomainWarpAmplitude = 32;



    public Vec2i GoodDragonMountainPeak;
    public Vec2i EvilDragonMountainPeak;


    public ChunkBase2[,] ChunkBases;

    public float WaterHeight = 48;

    
    private float[,] HeightMap;
    private float[,] WaterMap;
    private GenerationRandom GenRan;
    private GameGenerator2 GameGen;

    public RiverGenerator2 RiverGen;

    public TerrainGenerator2(GameGenerator2 gameGen, int seed)
    {
        GameGen = gameGen;
        GenRan = new GenerationRandom(seed);
    }


    public void Generate()
    {
        
        HeightMap = new float[World.WorldSize, World.WorldSize];
        WaterMap = new float[World.WorldSize, World.WorldSize];
        
        ChunkBases = new ChunkBase2[World.WorldSize, World.WorldSize];


        FillHeightMap();

        GenerateBaseTerrain();
        Rivers();
        //CellularRiver(512, 512);
        //RiverGen = new RiverGenerator2(GameGen);
        //verGen.GenerateRivers(8);
    }

    /// <summary>
    /// Calculates the height map for the world.
    /// This process consists of 3 parts
    /// </summary>
    private void FillHeightMap()
    {

        //We find the position of the two mountains in the game
        //note -only 2 are currently generated (good & bad dragon lairs)
        //Code for height map can be modified to allow for n mountains (might be too many?)
        Vec2i[] mountPos = DecideMountainPlacement();


        GoodDragonMountainPeak = mountPos[1];
        EvilDragonMountainPeak = mountPos[0];

        //Fill height map with perlin noise, as well as base mountain calculations
        float[,] bloatedHeightMap = GenerateInitialHeightMap(mountPos);
        //bloatedHeightMap = Erode(bloatedHeightMap, 100000);
        HeightMap = bloatedHeightMap;
        //Perform domain warping for more realistic looking world.
        //HeightMap = DomainWarp(bloatedHeightMap);
        SurroundWithWater(HeightMap);
        
        return;
        int boundry = 356;
        float boundrySigma=356*356;

        //Iterate each point of the height map
        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {

                //Calculate the distance to the nearest of these mountains
                int d1 = new Vec2i(x, z).QuickDistance(mountPos[0]);
                int d2 = new Vec2i(x, z).QuickDistance(mountPos[1]);
                float valI = Mathf.Min(d1, d2);

                float mount = Mathf.Exp(-valI / (64f * 64f));


                float height = 0;
                //Perlin noise of base height map
                for (int i = 0; i < 4; i++)
                {
                    float scale = 85f * Mathf.Pow(0.5f, i);
                    float octave = Mathf.Pow(2, i) * 0.002f;
                    height += scale * Mathf.PerlinNoise((x + i) * octave, (z + i) * octave);
                }


                
                /*
                if (dist > 256)
                {
                    height *= Mathf.Exp(-(dist - 256) * (dist - 256) / sigma);
                }*/

                height += mount * 128f * RidgeNoise(x, z);

                HeightMap[x, z] = height;




            }
        }
        


        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                float xOver = 0;
                float zOver = 0;

                float exp = 0;

                if (x < boundry)
                    xOver = (boundry - x);
                else if (x > World.WorldSize - boundry)
                    xOver = boundry - (World.WorldSize - x);
                if (z < boundry)
                    zOver = (boundry - z);
                else if (z > World.WorldSize - boundry)
                    zOver = boundry - (World.WorldSize - z);

                exp = Mathf.Exp(-(xOver * xOver + zOver * zOver) / boundrySigma);
                HeightMap[x, z] *= exp;

            }
        }
    }

// Calculates the position of the two mountains required (good & bad dragon locations) 
// Ensures the two mountains are seperated by a minimum distance 
    /// <summary>
    /// Calculates the position of the two required mountains (good and bad dragon locations)
    /// Ensures the two mountains are seperated by a minimum distance '<paramref name="minSep"/>' 
    /// </summary>
    /// <param name="minSep"></param>
    /// <returns>An array containing position of two mountains such that 
    ///             Vec2i[0] = bad dragon  
    ///             Vec2i[1] = good dragon  </returns>
    private Vec2i[] DecideMountainPlacement(int minSep = 356)
    {
        Vec2i mid = new Vec2i(World.WorldSize / 2, World.WorldSize / 2);
        Vec2i badDragonMount = GenRan.RandomVec2i(-World.WorldSize / 3, World.WorldSize / 3) + mid;
        Vec2i goodDragonMount = GenRan.RandomVec2i(-World.WorldSize / 3, World.WorldSize / 3) + mid;
        //Ensure they are seperated by at least 356
        while (badDragonMount.QuickDistance(goodDragonMount) < minSep * minSep)
        {
            goodDragonMount = GenRan.RandomVec2i(-World.WorldSize / 3, World.WorldSize / 3) + mid;
        }
        return new Vec2i[] { badDragonMount, goodDragonMount };
    }
    /// <summary>
    /// Fills the height map over the range <br/>
    /// [-<see cref="HeightMapBoundry"/>, <see cref="World.WorldSize"/> + <see cref="HeightMapBoundry"/>]<br/>
    /// will perlin noise
    /// 
    /// </summary>
    /// <param name="mountains">Array containing positions of all mountains 
    ///                         TODO - replace with struct containing information about mountain shape (height, radius etc)</param>
    /// <param name="amp0">The amplitude of the first iteration of perlin noise</param>
    /// <param name="ampGrowth">The rate at which the amplitude changes for each iteration,
    ///                         such that amp_n = amp0 * ampGrowth^(n)</param>
    /// <param name="freq0">The frequency of the first iteration of perlin noise</param>
    /// <param name="freqGrowth">The rate at which the frequency changes for each iteration, such that
    ///                          freq_n = freq0 * freqGrowth^n</param>
    /// <param name="iterations">The number of iterations of perlin noise to sum</param>
    private float[,] GenerateInitialHeightMap(Vec2i[] mountains, float amp0 = 85f, float ampGrowth = 0.5f,
        float freq0 = 0.002f, float freqGrowth = 2, int iterations=4)
    {
        float[,] heightMap = new float[HeightMapBoundry * 2 + World.WorldSize, HeightMapBoundry * 2 + World.WorldSize];

        //Iterate all points in height map
        for (int x = -HeightMapBoundry; x < World.WorldSize + HeightMapBoundry; x++)
        {
            for (int z = -HeightMapBoundry; z < World.WorldSize + HeightMapBoundry; z++)
            {
                //Get related index for array
                int ix = x + HeightMapBoundry;
                int iz = z + HeightMapBoundry;

                float height = 0;
                //Perlin noise of base height map
                for (int i = 0; i < iterations; i++)
                {
                    float scale = amp0 * Mathf.Pow(ampGrowth, i);
                    float octave = Mathf.Pow(freqGrowth, i) * freq0;
                    height += scale * Mathf.PerlinNoise((x + i) * octave, (z + i) * octave);
                }

                //Calculate the mountain effects
                int shortestDistance = -1;
                
                //We need to find the nearest mountain
                foreach(Vec2i v in mountains)
                {
                    int distSqr = v.QuickDistance(new Vec2i(x, z));
                    if(shortestDistance < 0 || distSqr < shortestDistance)
                    {
                        shortestDistance = distSqr;
                    }
                }

                float mount = Mathf.Exp(-shortestDistance / (64f * 64f));
                //Add mountains to height map
                height += mount * 128f * PerlinNoise(x, z, 100, 0.02f)* (0.5f + 0.5f*RidgeNoise(x,z));
                //Set height map
                heightMap[ix, iz] = height;

            }
        }
        return heightMap;
    }
    /// <summary>
    /// Performs domain warping on the supplied height map 
    /// </summary>
    /// <param name="bloatedHeightMap">A height map that represents values 
    /// [-<see cref="HeightMapBoundry"/>, <see cref="World.WorldSize"/><see cref="HeightMapBoundry"/>]</param> 
    /// <returns>Heightmap after warping from representing area [0, <see cref="World.WorldSize"/>]</returns>
    private float[,] DomainWarp(float[,] bloatedHeightMap)
    {

        //Vector2[,] warp = new Vector2[World.WorldSize, World.WorldSize];
        float[,] warpedHeights = new float[World.WorldSize, World.WorldSize];

        float amp = DomainWarpAmplitude - 1;
        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                int ix_height = x + HeightMapBoundry;
                int iz_height = z + HeightMapBoundry;
                //warpedHeights[x, z] = HeightMap[ix_height, iz_height];

                //Generate perlin noise [-1, 1] for x and z
                float warpX = 2f * (PerlinNoise(x, z, 18, 0.005f) - 0.5f);
                float warpZ = 2f * (PerlinNoise(x, z, 23, 0.005f) - 0.5f);
                int dx = (int)(warpX * amp);
                int dz = (int)(warpZ * amp);
                /*
                if (dx + x < 0 || dx + x >= World.WorldSize)
                    continue;
                if (dz + z < 0 || dz + z>= World.WorldSize)
                    continue;*/
                
                warpedHeights[x, z] = bloatedHeightMap[ix_height + dx, iz_height + dz];
                

            }
        }
        return warpedHeights;


    }

    /// <summary>
    /// Surrounds the supplied map with water by multiplying all values closer to the edge than '<paramref name="boundry"/>'
    /// by a negative gausian ->
    ///     height = height * Exp(-over*over/sigma)
    /// </summary>
    /// <param name="heightmap">The height map to modify</param>
    /// <param name="boundry">The distance from world edge at which we wish to start scaling down</param>
    /// <param name="sigma">Rate of scaling</param>
    private void SurroundWithWater(float[,] heightmap, int boundry=356, float sigma = 356*356)
    {

        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                float xOver = 0;
                float zOver = 0;

                float exp = 0;

                if (x < boundry)
                    xOver = (boundry - x);
                else if (x > World.WorldSize - boundry)
                    xOver = boundry - (World.WorldSize - x);
                if (z < boundry)
                    zOver = (boundry - z);
                else if (z > World.WorldSize - boundry)
                    zOver = boundry - (World.WorldSize - z);

                exp = Mathf.Exp(-(xOver * xOver + zOver * zOver) / sigma);
                heightmap[x, z] *= exp;

            }
        }


    }

    private float[,] Erode(float[,] heightmap, int drops)
    {
        TerrainErosion erode = new TerrainErosion(heightmap);
        return erode.Erode(drops);

    }
    private void Rivers()
    {
        //Calculate a vector field representing the direction of flow for each chunk
        CalcualteFlowField();
        //Find all coast points
        List<Vec2i> coast = FindCoastPoints();
        List<Vec2i> riverStarts = new List<Vec2i>(20);

        List<RiverStartEnd> rivers = new List<RiverStartEnd>();

        for (int i = 0; i < 25; i++)
        {


            Vec2i source = GenRan.RandomVec2i(World.WorldSize / 2 - World.WorldSize / 3, World.WorldSize / 2 + World.WorldSize / 3);
            Vec2i end = GetNearestRiverEnd(source, rivers);
            if(end != null)
            {
                rivers.Add(new RiverStartEnd(source, end));
                FromRiverSource(source, end);
            }
            
            
        }
    }

    private struct RiverStartEnd
    {
        public Vec2i Start;
        public Vec2i End;
        public RiverStartEnd(Vec2i s, Vec2i e)
        {
            this.Start = s;
            this.End = e;
        }
    }

    /// <summary>
    /// Finds a point on the coast near to the supplied source.
    /// If '<paramref name="currentRivers"/>' is not null, then we check only accept 
    /// coastal points that are at least '<paramref name="minDistSep"/>' chunks away from all end points
    /// </summary>
    /// <param name="source"></param>
    /// <param name="currentRivers"></param>
    /// <param name="minDistSep"></param>
    /// <returns></returns>
    private Vec2i GetNearestCoast(Vec2i source, List<RiverStartEnd> currentRivers=null, int minDistSep=128)
    {
       

        bool[] octDirIgnore = new bool[8];
        //We check distances from point - TODO bisect function might be faster?
        for(int i=1; i<512; i++)
        {
            //iterate each direction
            for(int j=0; j<Vec2i.OCT_DIRDIR.Length; j++)
            {
                //If we ignore this direction, ignore it
                if (octDirIgnore[j])
                    continue;

                Vec2i p = source + Vec2i.OCT_DIRDIR[j] * i;
                if (p.x < 0 || p.z < 0 || p.x >= World.WorldSize || p.z >= World.WorldSize)
                {
                    //If this direction leads off the map (shouldn't happen) we ignore it
                    octDirIgnore[j] = true;
                    continue;
                }
                //if this point is an ocean point
                if (ChunkBases[p.x, p.z].Biome == ChunkBiome.ocean)
                {
                    //We ensure we ignore this direction, in case this end point is not valid  
                    octDirIgnore[j] = true;
                    //We find the smallest seperation between this point and the others
                    int minSep = -1;
                    
                    foreach (RiverStartEnd rse in currentRivers)
                    {
                        int sqrDist = rse.End.QuickDistance(p);
                        if (minSep < 0 || sqrDist < minSep)
                            minSep = sqrDist;
                    }
                    //If we are seperated enough, or there are no other rivers
                    if(minSep > minDistSep*minDistSep || minSep < 0)
                    {
                        //Then this point is valid, so we return it
                        return p;
                    }//If this point is not valid, then we don't return it

                }
                    
            }

        }
        return null;

    }


    /// <summary>
    /// Finds a point on the coast or a river near to the supplied source.
    /// If '<paramref name="currentRivers"/>' is not null, then we check only accept 
    /// coastal points that are at least '<paramref name="minDistSep"/>' chunks away from all end points
    /// </summary>
    /// <param name="source"></param>
    /// <param name="currentRivers"></param>
    /// <param name="minDistSep"></param>
    /// <returns></returns>
    private Vec2i GetNearestRiverEnd(Vec2i source, List<RiverStartEnd> currentRivers = null, int minDistSep = 64)
    {
        float sourceHeight = ChunkBases[source.x, source.z].Height;

        bool[] octDirIgnore = new bool[8];
        //We check distances from point - TODO bisect function might be faster?
        for (int i = 1; i < 512; i++)
        {
            //iterate each direction
            for (int j = 0; j < Vec2i.OCT_DIRDIR.Length; j++)
            {
                //If we ignore this direction, ignore it
                if (octDirIgnore[j])
                    continue;

                Vec2i p = source + Vec2i.OCT_DIRDIR[j] * i;
                if (p.x < 0 || p.z < 0 || p.x >= World.WorldSize || p.z >= World.WorldSize)
                {
                    //If this direction leads off the map (shouldn't happen) we ignore it
                    octDirIgnore[j] = true;
                    continue;
                }
                //if this point is an ocean point
                if (ChunkBases[p.x, p.z].Biome == ChunkBiome.ocean || ChunkBases[p.x, p.z].ChunkFeature is ChunkRiverNode)
                {
                    //We ensure we ignore this direction, in case this end point is not valid  
                    octDirIgnore[j] = true;
                    //We find the smallest seperation between this point and the others
                    int minSep = -1;

                    if (ChunkBases[p.x, p.z].Height > sourceHeight)
                        continue;

                    foreach (RiverStartEnd rse in currentRivers)
                    {
                        int sqrDist = rse.End.QuickDistance(p);
                        if (minSep < 0 || sqrDist < minSep)
                            minSep = sqrDist;
                    }
                    //If we are seperated enough, or there are no other rivers
                    if (minSep > minDistSep * minDistSep || minSep < 0)
                    {
                        //Then this point is valid, so we return it
                        return p;
                    }//If this point is not valid, then we don't return it

                }

            }

        }
        return null;

    }




    private Vec2i CalcDir(Vec2i a, Vec2i b)
    {

        int dx = b.x - a.x;
        int dz = b.z - a.z;
        if (dx < 0)
            dx = -1;
        else if (dx > 0)
            dx = 1;
        if (dz < 0)
            dz = -1;
        else if (dz > 0)
            dz = 1;
        return new Vec2i(dx, dz);
    }
    private void FromRiverSource(Vec2i source, Vec2i end, float startHeight=-1, int distSinceFork=0, bool riverRaySearch=true)
    {
        int i = 0;
        if (startHeight < 0)
            startHeight = ChunkBases[source.x, source.z].Height;

        i = 0;

        Vec2i last = source;
        Vec2i mainDir = CalcDir(source, end);



        Vec2i current = source + mainDir;
        bool isDone = false;

        Vector2 exactCurrent = current.AsVector2();

        List<RiverPoint> river = new List<RiverPoint>();
        ChunkRiverNode previousNode = null;
        while (!isDone)
        {

            int distToEnd = end.QuickDistance(current);

            i++;
            /*
            if(i%16 == 0 && riverRaySearch)
            {
                bool success = false;
                //search up to 16 chunks away
                for(int j=1; j<16; j++)
                {
                    if (success)
                        break;
                    //search all 8 directions
                    foreach(Vec2i v in Vec2i.OCT_DIRDIR)
                    {
                        Vec2i p = current + v * j;

                        if(ChunkBases[p.x,p.z].Biome == ChunkBiome.ocean || ChunkBases[p.x, p.z].ChunkFeature is ChunkRiverNode)
                        {
                            end = p;
                            success = true;
                            break;
                        }
                    }
                }
            }*/


            // Vector2 currentFlow = FlowField[current.x, current.z];

            float fx = PerlinNoise(current.x, current.z, 36) * 2 - 1;
            float fz = PerlinNoise(current.x, current.z, 37) * 2 - 1;
            Vector2 noiseFlow = new Vector2(fx, fz);
            Vector2 flowField = FlowField[current.x, current.z];
            Vector2 targetFlow = (end - current).AsVector2().normalized;


            float targetFlowMult = distToEnd < 400 ? 4 * Mathf.Exp((400f - distToEnd) / 200f) : 4;

            Vector2 flow = (noiseFlow + targetFlowMult * targetFlow + 3.5f * flowField).normalized;
            exactCurrent += flow;
            current = Vec2i.FromVector2(exactCurrent);
            int check = Mathf.Min(river.Count, 5);
            bool isValid = true;
            for (int j = 0; j < check; j++)
            {
                if (river[river.Count - j - 1].Pos == current)
                    isValid = false;
            }
            if (!isValid)
            {
                current += mainDir;
                exactCurrent = current.AsVector2();
            }


            if (ChunkBases[current.x, current.z].Biome == ChunkBiome.ocean)
                isDone = true;
            if (ChunkBases[current.x, current.z].ChunkFeature is ChunkRiverNode)
            {
                isDone = true;
                //Shouldn't be null, but lets do a check anyway
                if(previousNode != null)
                {
                    //Get the river node
                    ChunkRiverNode endNode = ChunkBases[current.x, current.z].ChunkFeature as ChunkRiverNode;
                    //Inform river nodes of flow
                    endNode.FlowIn.Add(previousNode);
                    previousNode.FlowOut.Add(endNode);
                    ModifyRiverHeight(endNode);
                }
                if(GenRan.Random() < 0.5f)
                {
                    PlaceLake(current, 8);
                }
            }
            if (current == end)
                isDone = true;
            ChunkRiverNode nextNode = new ChunkRiverNode(current);
            ChunkBases[current.x, current.z].SetChunkFeature(nextNode);
            if(previousNode != null)
            {
                nextNode.FlowIn.Add(previousNode);
                previousNode.FlowOut.Add(nextNode);
            }
            previousNode = nextNode;
            //If this chunk is too high, we modify it and the surrounding area
            if(ChunkBases[current.x, current.z].Height > startHeight)
            {
                ModifyRiverValleyHeight(current, startHeight);
            }else if(ChunkBases[current.x, current.z].Height < startHeight)
            {
                startHeight = ChunkBases[current.x, current.z].Height;
            }
            RiverPoint rp = new RiverPoint();
            rp.Pos = current;
            rp.Flow = flow;

            river.Add(rp);
            if (i > 4096)
            {
                PlaceLake(current, 12);
                return;
            }
                

            /*
            distSinceFork++;

            if(distSinceFork > 256)
            {

                float p = Mathf.Exp(-distSinceFork/200f);
                if(p < GenRan.Random())
                {

                    Vec2i delta = GenRan.RandomVec2i(-64, 64);

                    FromRiverSource(current + delta, current);
                    distSinceFork = 0;
                }

            }*/
            mainDir = CalcDir(current, end);

        }
        return;
        if(river.Count > 128)
        {


            int forkCount = Mathf.CeilToInt(river.Count / 128f);
            int jump =(int) (((float)river.Count)/forkCount);
            int index = GenRan.RandomInt(0, jump);
            for(int j=0; j<forkCount; j++)
            {

                if (index > river.Count - 30)
                    return;
                RiverPoint rp = river[index];
                Vec2i forkEnd = rp.Pos;
                Vec2i delta = GenRan.RandomVec2i(-32, 32);

                Vector2 endToForkDir = (forkEnd-end).AsVector2().normalized;

                Vec2i forkStart = Vec2i.FromVector2(forkEnd.AsVector2() + endToForkDir * GenRan.Random(64, 128)) + delta;


                FromRiverSource(forkStart, forkEnd);



                index += jump + GenRan.RandomInt(-32, 32);
            }


        }
    }

    private void PlaceLake(Vec2i v, int radius)
    {
        for(int x=-radius; x<= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                float noiseRad = radius * 0.5f + 0.5f * radius * PerlinNoise(x, z, 94869);
                if(x*x+z*z < noiseRad * noiseRad)
                {
                    ChunkBases[x + v.x, z + v.z].SetChunkFeature(new ChunkLake(v + new Vec2i(x, z)));
                }
            }
        }
    }

    private void ModifyRiverHeight(ChunkRiverNode crn)
    {
        float height = ChunkBases[crn.Pos.x, crn.Pos.z].Height;
        foreach(ChunkRiverNode crn1 in crn.FlowOut)
        {
            ChunkBase2 cb = ChunkBases[crn1.Pos.x, crn1.Pos.z];
            if(cb.Height < height)
            {
                height = cb.Height;
                ModifyRiverValleyHeight(crn1.Pos, height);

            }
        }
    }


    private struct RiverPoint
    {
        public Vec2i Pos;
        public Vector2 Flow;
    }
    /// <summary>
    /// Modifies the height of the supplied chunk to the paramater <paramref name="height"/>, and then 
    /// further modifies the heights of near chunks that are heigher than this one
    /// </summary>
    /// <param name="position"></param>
    /// <param name="height"></param>
    private void ModifyRiverValleyHeight(Vec2i position, float height, int smoothRad = 32, float smoothAmount=16)
    {
        ChunkBases[position.x, position.z].SetHeight(height);
        //iterate all points in a circle
        for(int x=-smoothRad; x<=smoothRad; x++)
        {
            for(int z=-smoothRad; z<=smoothRad; z++)
            {
                if(x*x + z*z < smoothRad * smoothRad)
                {
                    //Get chunk at this position
                    Vec2i pos = position + new Vec2i(x, z);
                    ChunkBase2 cb = ChunkBases[pos.x, pos.z];
                    //if this chunk is of valid height, we ignore it
                    if (cb.Height <= height)
                        continue;

                    float deltaHeight = cb.Height - height;
                    //Calculate new height 
                    float nHeight = cb.Height - Mathf.Exp(-(x * x + z * z) / (smoothAmount* smoothAmount)) * deltaHeight;
                    cb.SetHeight(nHeight);
                }
            }
        }

    }


    /*
    private void FromRiverSource(Vec2i source, Vec2i mainDir)
    {

        int i = 0;
        Vec2i end = null;
        Vec2i current = source;
        //Ray cast from the source to find the ocean point at this rivers end
        while(end == null)
        {
            i++;
            current += mainDir;
            if(ChunkBases[current.x, current.z].Biome == ChunkBiome.ocean)
            {
                end = current;
            }
            if (i > World.WorldSize)
                return;

        }
        i = 0;

        Vec2i last = source;

        current = source + mainDir;
        bool isDone = false;

        Vector2 lastDir = (end - current).AsVector2().normalized;
        Vector2 exactCurrent = current.AsVector2();

        List<Vec2i> river = new List<Vec2i>();

        while (!isDone)
        {


            
            i++;
            // Vector2 currentFlow = FlowField[current.x, current.z];

            float fx = PerlinNoise(current.x, current.z, 36)*2 - 1;
            float fz = PerlinNoise(current.x, current.z, 37)*2 - 1;
            Vector2 noiseFlow = new Vector2(fx, fz);
            Vector2 flowField = FlowField[current.x, current.z];
            Vector2 targetFlow = (end - current).AsVector2().normalized;

            Vector2 flow = (noiseFlow + 4 * targetFlow + 3 * flowField).normalized;
            exactCurrent += flow;
            current = Vec2i.FromVector2(exactCurrent);
            int check = Mathf.Min(river.Count, 5);
            bool isValid = true;
            for(int j=0; j< check; j++)
            {
                if (river[river.Count - j - 1] == current)
                    isValid = false;
            }
            if (!isValid)
            {
                current += mainDir;
                exactCurrent = current.AsVector2();
            }


            if (ChunkBases[current.x, current.z].Biome == ChunkBiome.ocean)
                isDone = true;
            ChunkBases[current.x, current.z].SetChunkFeature(new ChunkRiverNode());
            river.Add(current);
            if (i > 2048)
                return;
                
        }


    }*/


    private void ReverseFlowRiver(Vec2i start, int length, int distSinceFork=0)
    {


        //Give rivers a slight biase towards the middle of the map
        Vector2 bias = -(new Vector2(World.WorldSize / 2, World.WorldSize / 2) - start.AsVector2()).normalized;


        Vec2i current = start;
        Vector2 fullCurrent = current.AsVector2();
        Vec2i last = Vec2i.FromVector2(fullCurrent + bias);
        List<FlowPoint> inputFlowPoints = new List<FlowPoint>(5);

        for(int i=0; i<length; i++)
        {
            distSinceFork++;
            inputFlowPoints.Clear();
            //if this chunk is a river node already, we have reached the end of this river branch
            if(ChunkBases[current.x, current.z].ChunkFeature is ChunkRiverNode)
            {
                //return;
            }

            //Add a river node here
            ChunkBases[current.x, current.z].SetChunkFeature(new ChunkRiverNode(current));

            //We iterate each of the near chunks
            foreach(Vec2i v in Vec2i.OCT_DIRDIR)
            {
                //Coordinate of point of interest
                Vec2i p = v + current;
                //If this is the point we just came from, ignore it
                if (last == p)
                    continue;
                Vector2 vFlow = FlowField[p.x, p.z];
                //Find the coordinate that this point flows into 
                Vec2i pointFlowPos = Vec2i.FromVector2(p.AsVector2() + vFlow);

                //Check if this point flows into our current point
                if(pointFlowPos == current)
                {
                    FlowPoint fp = new FlowPoint();
                    fp.Pos = p;
                    fp.Dir = vFlow;
                    inputFlowPoints.Add(fp);
                }
            }
            //If no points flow here, then the river has reached an end (add lakes?)
            if(inputFlowPoints.Count == 0)
            {
                Debug.Log("zero flow");
                Vector2 currentToLast = (current - last).AsVector2();
                fullCurrent = fullCurrent - currentToLast;
                //Debug.Log("zero error...");
                //return;
            }else if(inputFlowPoints.Count == 1)
            {
                Debug.Log("single flow");
                fullCurrent = fullCurrent - inputFlowPoints[0].Dir;
                
            }
            else
            {

                if(distSinceFork < 40)
                {
                    fullCurrent = fullCurrent - GenRan.RandomFromList(inputFlowPoints).Dir;
                    Debug.Log("No fork - dist");
                }
                else
                {
                    Debug.Log("fork");
                    //If we are over 40, then we can create a fork

                    //only 2 forks maximum
                    while (inputFlowPoints.Count > 2)
                    {
                        inputFlowPoints.RemoveAt(GenRan.RandomInt(0, inputFlowPoints.Count));
                    }
                    ReverseFlowRiver(inputFlowPoints[0].Pos, length - i, 0);
                    ReverseFlowRiver(inputFlowPoints[1].Pos, length - i, 0);
                    Debug.Log("forks");
                    return;
                }             
            }
            last = new Vec2i(current.x, current.z);
            current = Vec2i.FromVector2(fullCurrent);
            /*
            //We iterate all directions 
            Vector2 grad = (FlowField[current.x, current.z] + 0.1f * bias).normalized;
            fullCurrent = fullCurrent - grad;
            current = Vec2i.FromVector2(fullCurrent);
            */

        }
    }

    private struct FlowPoint
    {
        public Vec2i Pos;
        public Vector2 Dir;
    }
    private List<Vec2i> FindCoastPoints()
    {
        List<Vec2i> coast = new List<Vec2i>(4000);

        for(int x=1; x<World.WorldSize-1; x++)
        {
            for(int z=1; z<World.WorldSize-1; z++)
            {
                bool isLand = HeightMap[x, z] > WaterHeight;

                //We only check water tiles, as there are less of them
                if (!isLand)
                {
                    foreach (Vec2i v in Vec2i.QUAD_DIR)
                    {
                        if (HeightMap[x + v.x, z + v.z] > WaterHeight)
                        {
                            coast.Add(new Vec2i(x + v.x, z + v.z));
                        }
                    }
                }
                


            }
        }
        return coast;
    }


    /// <summary>
    /// Generates the height map & biomes, + chunk resource details
    /// </summary>
    private void GenerateBaseTerrain()
    {
       //imulateWater();
        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                float height = GetChunkHeightAt(x, z);
                int eqDist = Mathf.Abs(z - (World.WorldSize / 2));
                float temp = Mathf.Exp(-eqDist/100f)*2f - height/256f;
                float hum = PerlinNoise(x, z, 2);

                ChunkBiome b = ChunkBiome.ocean;


                if (height > 100)
                {
                    b = ChunkBiome.mountain;
                }
                else if (height < WaterHeight)
                {
                    b = ChunkBiome.ocean;
                }
                else if ((temp > 0.4f && hum < 0.3f) || temp>0.9f)
                {
                    b = ChunkBiome.dessert;
                }
                else if (temp > 0.4f & hum > 0.5f)
                {
                    b = ChunkBiome.forrest;
                }
                else
                {
                    b = ChunkBiome.grassland;
                }
                ChunkBases[x, z] = new ChunkBase2(new Vec2i(x, z), height, b);

                switch (b)
                {
                    //Deserts and mountains are for mining
                    case ChunkBiome.mountain:
                    case ChunkBiome.dessert:
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.ironOre, PerlinNoise(x, z, 3));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.silverOre,0.4f* PerlinNoise(x, z, 4));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.goldOre, 0.2f*PerlinNoise(x, z, 5));
                        break;
                    case ChunkBiome.forrest:
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.wood, 1);
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.sheepFarm, PerlinNoise(x, z, 6));
                        break;
                    case ChunkBiome.grassland:
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.sheepFarm, PerlinNoise(x,z, 7));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.cattleFarm, PerlinNoise(x, z, 8));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.wheatFarm, PerlinNoise(x, z, 9));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.vegetableFarm, PerlinNoise(x, z, 10));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.silkFarm, PerlinNoise(x, z, 11));


                        break;
                }
            }
        }
    }

  

    private Vector2[,] FlowField;
    private void CalcualteFlowField()
    {

        Vector2[,] flowField = new Vector2[World.WorldSize, World.WorldSize];


        List<KeyValuePair<float, Vector2>> pointGradients = new List<KeyValuePair<float, Vector2>>();

        for(int x=0; x<World.WorldSize; x++)
        {
            for(int z=0; z<World.WorldSize; z++)
            {
                if (x == 0 || z == 0 || x >= World.WorldSize-1 || z >= World.WorldSize-1)
                {
                    flowField[x, z] = Vector2.zero;
                    continue;
                }
                pointGradients.Clear();
                float gradNormSum = 0;
                foreach (Vec2i v in Vec2i.OCT_DIRDIR)
                {
                    float dHeight = HeightMap[v.x + x, v.z + z] - HeightMap[x, z];
                    //If the test point is lower than the current point
                    if(dHeight < 0)
                    {
                        pointGradients.Add(new KeyValuePair<float, Vector2>(-dHeight, v.AsVector2()));
                        gradNormSum -= dHeight;
                    }
                }
                foreach(KeyValuePair<float, Vector2> kvp in pointGradients)
                {
                    flowField[x, z] += kvp.Value * kvp.Key / gradNormSum;
                }
                


                


            }
        }
        FlowField = flowField;

    }


    public float GetWorldHeightAt(float x, float z)
    {
        return GetChunkHeightAt(x / World.ChunkSize, z / World.ChunkSize);
    }
    /// <summary>
    /// Calculates the height at a specified chunk coordinate
    /// by interpolating between near values in <see cref="HeightMap"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public float GetChunkHeightAt(float x, float z)
    {
    
        int minX = Mathf.FloorToInt(x);
        int minZ = Mathf.FloorToInt(z);

        int maxX = Mathf.CeilToInt(x);
        int maxZ = Mathf.CeilToInt(z);

        float xPart = x - minX;
        float zPart = z - minZ;

        float v1 = HeightMap[minX, minZ];
        float v2 = HeightMap[minX, maxZ];
        float v3 = HeightMap[maxX, maxZ];
        float v4 = HeightMap[maxX, minZ];

        float i1 = xPart * v1 + (1 - xPart) * v4;
        float i2 = xPart * v2 + (1 - xPart) * v3;
        return zPart * i1 + (1 - zPart) * i2;
    }


    private float PerlinNoise(float x, float z, int i, float freq=0.005f)
    {
        return Mathf.PerlinNoise(x * freq + i * 13, z * freq + i * 27);
    }


    private float RidgeNoise(float x, float z, float freq = 0.02f)
    {

        float c = Mathf.PerlinNoise(x * freq, z * freq);
        c -= 0.5f;
        c = Mathf.Abs(c)*2;

        return 1-c;
    }

    public float HeightFunction(int x, int z)
    {
        int cx = Mathf.FloorToInt((float)x / World.ChunkSize);
        int cz = Mathf.FloorToInt((float)z / World.ChunkSize);
        float px = ((float)(x % World.ChunkSize)) / World.ChunkSize;
        float pz = ((float)(z % World.ChunkSize)) / World.ChunkSize;
        return GetChunkHeightAt(cx + px, cz + pz);
    }


    public Texture2D ToTexture()
    {
        Texture2D tex = new Texture2D(World.WorldSize, World.WorldSize);
        for(int x=0; x<World.WorldSize; x++)
        {
            for(int z=0; z<World.WorldSize; z++)
            {
                tex.SetPixel(x, z, ChunkBases[x, z].GetMapColor());
            }
        }
        tex.Apply();
        return tex;
    }

    public Texture2D DrawContours()
    {
        Texture2D tex = new Texture2D(World.WorldSize, World.WorldSize);

        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                int height = (int)(ChunkBases[x, z].Height*5);
                if (height % 25 == 0)
                {
                    tex.SetPixel(x, z, Color.red);
                }
                else
                {
                    tex.SetPixel(x, z, new Color(0, 0, 0, 0));
                }
            }
        }
        tex.Apply();
        return tex;

    }


   

}