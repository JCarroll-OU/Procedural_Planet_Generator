using UnityEngine;
using Unity.Entities;

// Class containing all biome data and generation parameters used by the planet generator system. 
// A maximum of 12 biomes are supported! If additional biomes are specified, they simply will not be used.
[CreateAssetMenu()]
public class Biomes : ScriptableObject
{
    // Contains all the textures used by a single biome. 
    [System.Serializable]
    public struct PBR_Material
    {
        public Texture2D Albedo, Normal, Occlusion, Metallic, Smoothness;
    }

    // Contains all the data used for a single biome. 
    [System.Serializable]
    public struct Biome
    {
        [Tooltip("This only affects the appearance in the inspector!")]
        public string Name;
        [Tooltip("The x- and y-values specifies the minimum and maximum acceptable 'temperate' rating for this biome to spawn.")]
        public Vector2 TemperateMinMax;
        [Tooltip("The x- and y-values specifies the minimum and maximum acceptable 'fertility' rating for this biome to spawn.")]
        public Vector2 FertilityMinMax;
        [Tooltip("Textures used by this biome.")]
        public PBR_Material Textures;

        public Noise.NoiseSettings TerrainHeight, TerrainDetail;
        public Noise.NoiseSettings MountainHeight, MountainDetail;

        // Randomize the noise values for this biome.
        public void RandomizeDisplacement(float planetDiameter)
        {
            TerrainHeight.offset = new Vector3(Random.Range(-10000f, 10000f),
                                               Random.Range(-10000f, 10000f),
                                               Random.Range(-10000f, 10000f));
            TerrainHeight.frequency = Random.Range(1f, 2.5f);
            TerrainHeight.octaves = 2;
            TerrainHeight.strength = Random.Range(2.5f, 25f);
            TerrainHeight.persistence = 1f;
            TerrainHeight.lacunarity = 2f;
            TerrainHeight.isRigid = false;

            TerrainDetail.offset = new Vector3(Random.Range(-10000f, 10000f),
                                               Random.Range(-10000f, 10000f),
                                               Random.Range(-10000f, 10000f));
            TerrainDetail.frequency = Random.Range(2.5f, 10f);
            TerrainDetail.octaves = 2;
            TerrainDetail.strength = Random.Range(1.25f, 12.5f);
            TerrainDetail.persistence = 1f;
            TerrainDetail.lacunarity = 2f;
            TerrainDetail.isRigid = false;

            MountainHeight.offset = new Vector3(Random.Range(-10000f, 10000f),
                                                Random.Range(-10000f, 10000f),
                                                Random.Range(-10000f, 10000f));
            MountainHeight.frequency = Random.Range(1f, 7.5f);
            MountainHeight.octaves = 2;
            MountainHeight.strength = Random.Range(2.5f, 25f);
            MountainHeight.persistence = 1f;
            MountainHeight.lacunarity = 2f;
            MountainHeight.isRigid = true;

            MountainDetail.offset = new Vector3(Random.Range(-10000f, 10000f),
                                                Random.Range(-10000f, 10000f),
                                                Random.Range(-10000f, 10000f));
            MountainDetail.frequency = Random.Range(2.5f, 10f);
            MountainDetail.octaves = 2;
            MountainDetail.strength = Random.Range(2.5f, 25f);
            MountainDetail.persistence = 1f;
            MountainDetail.lacunarity = 2f;
            MountainDetail.isRigid = true;
        }
    }

    // Data that can be sent to the jobs system (all data is blittable).
    [InternalBufferCapacity(12)]
    public struct PackedBiomeData : IBufferElementData
    {
        public Vector2 TemperateMinMax, FertilityMinMax;
        public Noise.NoiseSettings TerrainHeight, TerrainDetail;
        public Noise.NoiseSettings MountainHeight, MountainDetail;

        // Same as the biome data above just without the textures so that it works with ECS
        public PackedBiomeData(Biome src)
        {
            TemperateMinMax = src.TemperateMinMax;
            FertilityMinMax = src.FertilityMinMax;
            TerrainHeight = src.TerrainHeight;
            TerrainDetail = src.TerrainDetail;
            MountainHeight = src.MountainHeight;
            MountainDetail = src.MountainDetail;   
        }

        // This is used for undefined biomes, so put the settings outside the range of what will be drawn
        public PackedBiomeData(float x)
        {
            TemperateMinMax = new Vector2(100f, 100f);
            FertilityMinMax = new Vector2(100f, 100f);
            TerrainHeight = new Noise.NoiseSettings();
            TerrainDetail = TerrainHeight;
            MountainHeight = TerrainHeight;
            MountainDetail = TerrainHeight;
        }
    }

    // Biome generation settings, in a struct so it can be passed to ECS/jobs
    [System.Serializable]
    public struct BiomeSettings 
    {
        // Distance above water line to draw the 'underwater' terrain textures
        public float shoreOffset; // i.e. sand and clay
        [Range(1, 32)]
        public float shoreFade; // how quickly the terrain transitions from the
        [Range(1, 32)]
        public float oceanOvertakeFade; // how quickly the terrain displacement is replaced with the oceans

        [Range(0, 1)] // mask to control temperate, which is used to find the biome index
        public float temperateZoneScale; // 1 means the entire planet, and 0 is just the equator

        // Noise settings used to assign biomes, dictates the size, shape, and location of biomes.
        public Noise.NoiseSettings Biome_Temperate, Biome_Fertility;
        // Smaller values for strength result in larger biomes! 

        // Randomize the noise settings that relate to the biome size, shape, and location. 
        public void RandomizeBiomeSettings()
        {
            // some of the parameters are not used in biome cellular noise since we only use 1 octave
            Biome_Temperate.offset = new Vector3(Random.Range(-10000f, 10000f),
                                                 Random.Range(-10000f, 10000f),
                                                 Random.Range(-10000f, 10000f));
            Biome_Temperate.frequency = Random.Range(0.25f, 1f);
            Biome_Temperate.octaves = 1;
            Biome_Temperate.strength = Random.Range(0.05f, 1f);
            Biome_Temperate.persistence = 1f;
            Biome_Temperate.lacunarity = 2f;

            Biome_Fertility.offset = new Vector3(Random.Range(-10000f, 10000f),
                                                 Random.Range(-10000f, 10000f),
                                                 Random.Range(-10000f, 10000f));
            Biome_Fertility.frequency = Random.Range(0.25f, 1f);
            Biome_Fertility.octaves = 1;
            Biome_Fertility.strength = Random.Range(0.05f, 1f);
            Biome_Fertility.persistence = 1f;
            Biome_Fertility.lacunarity = 2f;
        }
    }

    // Reference to the biome settings struct.
    public BiomeSettings biomeSettings;
    [Tooltip("Base or default biome.")]
    public Biome OceanBiome;
    [Tooltip("You may specify up to 12 biomes.")]
    public Biome[] BiomeList;
    [HideInInspector]
    public Biomes.PackedBiomeData[] packedBiomes;
    [HideInInspector]
    public Biomes.PackedBiomeData packedOceanBiome;

    // Packs the textures specified in this class into a texture arrays that the shader can use.
    public void PackBiomeTextures()
    {
        PackBiomeData();
        if (BiomeList.Length <= 0)
        {
            Debug.LogWarning("No biomes to pack!");
            return;
        }
        int width = BiomeList[0].Textures.Albedo.width;
        int height = BiomeList[0].Textures.Albedo.height;
        TextureFormat format = BiomeList[0].Textures.Albedo.format;
        TextureFormat normFormat = BiomeList[0].Textures.Normal.format;
        int mipCount = BiomeList[0].Textures.Albedo.mipmapCount;
        biomes_albArr = new Texture2DArray(width, height, 13, format, true);
        biomes_normArr = new Texture2DArray(width, height, 13, normFormat, true);
        biomes_occArr = new Texture2DArray(width, height, 13, format, true);
        biomes_metallicArr = new Texture2DArray(width, height, 13, format, true);
        biomes_smoothArr = new Texture2DArray(width, height, 13, format, true);

        int index = 0;
        // Copy biome textures
        foreach (Biome cBiome in BiomeList)
        {
            // Pack textures for each mip map
            for (int mip = 0; mip < mipCount; mip++)
            {
                Graphics.CopyTexture(cBiome.Textures.Albedo, 0, mip, biomes_albArr, index, mip);
                Graphics.CopyTexture(cBiome.Textures.Normal, 0, mip, biomes_normArr, index, mip);
                Graphics.CopyTexture(cBiome.Textures.Occlusion, 0, mip, biomes_occArr, index, mip);
                Graphics.CopyTexture(cBiome.Textures.Metallic, 0, mip, biomes_metallicArr, index, mip);
                Graphics.CopyTexture(cBiome.Textures.Smoothness, 0, mip, biomes_smoothArr, index, mip);
            }

            index++;
        }
        // copy base biome texture to last element of array
        for (int mip = 0; mip < mipCount; mip++)
        {
            Graphics.CopyTexture(OceanBiome.Textures.Albedo, 0, mip, biomes_albArr, 12, mip);
            Graphics.CopyTexture(OceanBiome.Textures.Normal, 0, mip, biomes_normArr, 12, mip);
            Graphics.CopyTexture(OceanBiome.Textures.Occlusion, 0, mip, biomes_occArr, 12, mip);
            Graphics.CopyTexture(OceanBiome.Textures.Metallic, 0, mip, biomes_metallicArr, 12, mip);
            Graphics.CopyTexture(OceanBiome.Textures.Smoothness, 0, mip, biomes_smoothArr, 12, mip);
        }
        UnityEditor.AssetDatabase.CreateAsset(biomes_albArr, "Assets/Planet Generator/Shaders/Terrain/Shader Data/BiomesAlbedo.asset");
        UnityEditor.AssetDatabase.CreateAsset(biomes_normArr, "Assets/Planet Generator/Shaders/Terrain/Shader Data/BiomesNormal.asset");
        UnityEditor.AssetDatabase.CreateAsset(biomes_occArr, "Assets/Planet Generator/Shaders/Terrain/Shader Data/BiomesOcclusion.asset");
        UnityEditor.AssetDatabase.CreateAsset(biomes_metallicArr, "Assets/Planet Generator/Shaders/Terrain/Shader Data/BiomesMetallic.asset");
        UnityEditor.AssetDatabase.CreateAsset(biomes_smoothArr, "Assets/Planet Generator/Shaders/Terrain/Shader Data/BiomesSmoothness.asset");
    }

    // Must be called during planet initialization, packs only biome data not textures
    public void PackBiomeData()
    {
        // Pack the ocean biome data
        packedOceanBiome = new PackedBiomeData(OceanBiome);

        // Pack regular biome data
        packedBiomes = new Biomes.PackedBiomeData[12];
        for (int i = 0; i < 12; i++)
        {
            if (BiomeList.Length - 1 >= i)
                packedBiomes[i] = new PackedBiomeData(BiomeList[i]);
            else packedBiomes[i] = new PackedBiomeData(0f);
        }
    }

    // Reference the packed textures for later use. 
    [HideInInspector]
    public Texture2DArray biomes_albArr, biomes_normArr, biomes_occArr, biomes_metallicArr, biomes_smoothArr;
}