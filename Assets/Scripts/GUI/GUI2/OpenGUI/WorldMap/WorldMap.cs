using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class WorldMap
{
    public int Scale { get; private set; }
    public Texture2D WorldMapBase { get; private set; }
    public List<WorldMapLocation> WorldMapLocations;
    public WorldMap(World world)
    {
        GenerateWorldMap(world);
    }




    private void GenerateWorldMap(World world, int scale=4)
    {
        Scale = scale;
        WorldMapBase = new Texture2D(World.WorldSize* scale, World.WorldSize* scale);
        for(int x=0; x < World.WorldSize; x++)
        {
            for(int z=0; z<World.WorldSize; z++)
            {

                Color c = GetColorFromChunkBase(world.ChunkBases[x, z]);
                for(int x_=0; x_<scale; x_++)
                {
                    for(int z_=0; z_<scale; z_++)
                    {
                        WorldMapBase.SetPixel(x * scale + x_, z * scale + z_, c);
                    }
                }
            }
        }
        WorldMapBase.Apply();

        WorldMapLocations = new List<WorldMapLocation>();

        foreach(KeyValuePair<int, WorldLocation> loc in world.WorldLocations)
        {
            WorldMapLocation wml = new WorldMapLocation(loc.Value.Name, loc.Value.ChunkPos);
            WorldMapLocations.Add(wml);
            loc.Value.SetWorldMapLocation(wml);

        }

    }

    private Color GetColorFromChunkBase(ChunkBase cb)
    {
        
        Color c = Color.magenta;
        switch (cb.Biome)
        {
            case ChunkBiome.ocean:
                c = new Color(0, 0, 1);
                break;
            case ChunkBiome.grassland:
                c = new Color(0, 1, 0);
                break;
            case ChunkBiome.dessert:
                c = new Color(1, 1, 0);
                break;
            case ChunkBiome.forrest:
                c = new Color(34f / 255f, 139f / 255f, 34f / 255f);
                break;
            case ChunkBiome.mountain:
                c = Color.grey;
                break;
        }

        if (cb.RiverNode != null)
            c = new Color(0, 191f / 255f, 1f);
        if (cb.Lake != null)
        {
            c = new Color(0, 0.2f, 1f);
        }
        return c;
    }
}
[System.Serializable]
public class WorldMapLocation
{

    public string Name { get; private set; }
    public Vec2i ChunkPosition { get; private set; }
    public Vec2i WorldPosition { get; private set; }

    public WorldMapLocation(string name, Vec2i worldChunkPosition, Vec2i chunkTileOffset = null)
    {
        Name = name;
        if (name == null)
            Name = "no_name";
        ChunkPosition = worldChunkPosition;

        WorldPosition = ChunkPosition * World.ChunkSize;
        if (chunkTileOffset != null)
        {
            WorldPosition += chunkTileOffset;
        }
    }

    public override string ToString()
    {
        return Name + ":(" + ChunkPosition + ")";
    }


}