using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

// This class is essentially a wrapper for the Unity jobs system. This allows for multi-threaded generation
// of the terrain and water without all the overhead of sending it to the GPU via a compute shader. 
public class Optimizer
{
    // Terrain data
    public Vector3[] vertices;
    public int[] triangles;
    public Color[] colors;
    public Vector2[] UV1, UV2, UV3, UV4;

    // Water data 
    public Vector3[] waterVertices;
    public Vector2[] waterUVs;
    public Color[] waterColors;

    // Jobs system data
    public Generation_Job[] genJobs; // job data
    public JobHandle[] genJobHandles; // job handles
    private NativeArray<Vector3> data_vertices, data_waterVertices; // native output
    private NativeArray<Color> data_colors, data_waterColors; // native output
    private NativeArray<Vector2> data_uv1, data_uv2, data_uv3, data_uv4; // native output
    private NativeArray<Biomes.PackedBiomeData> biomes;

    // References to scripts used by the optimizer
    public Planet planetScript;
    public PlanetFace planetFace;
    public Vector3 localUp;

    public Optimizer(Planet pScript, PlanetFace tFace, Vector3 up)
    {
        planetScript = pScript;
        planetFace = tFace;
        localUp = up;
    }

    // Call this method to generate the mesh data.
    public Task Dispatch_Jobs(Chunk chunk)
    {
        // Obtain the quadIndex for this LOD
        int quadIndex = (chunk.neighbours[0] | chunk.neighbours[1] * 2 | chunk.neighbours[2] * 4 | chunk.neighbours[3] * 8);
        // Allocate the terrain input/output arrays
        data_vertices = new NativeArray<Vector3>((planetScript.presets.quadRes + 1) * (planetScript.presets.quadRes + 1), Allocator.TempJob);
        data_colors = new NativeArray<Color>(data_vertices.Length, Allocator.TempJob);
        data_uv1 = new NativeArray<Vector2>(data_vertices.Length, Allocator.TempJob);
        data_uv2 = new NativeArray<Vector2>(data_vertices.Length, Allocator.TempJob);
        data_uv3 = new NativeArray<Vector2>(data_vertices.Length, Allocator.TempJob);
        data_uv4 = new NativeArray<Vector2>(data_vertices.Length, Allocator.TempJob);
        // Allocate the water input/output arrays
        data_waterVertices = new NativeArray<Vector3>((planetScript.presets.quadRes + 1) * (planetScript.presets.quadRes + 1), Allocator.TempJob);
        data_waterColors = new NativeArray<Color>(data_vertices.Length, Allocator.TempJob);
        // Allocate the biome input array
        biomes = new NativeArray<Biomes.PackedBiomeData>(12, Allocator.TempJob);
        // Run the task and return the result
        Task task = Task.Run(() => {
            // Initialize the output arrays
            vertices = new Vector3[data_vertices.Length];
            UV1 = new Vector2[data_vertices.Length];
            UV2 = new Vector2[data_vertices.Length];
            UV3 = new Vector2[data_vertices.Length];
            UV4 = new Vector2[data_vertices.Length];
            colors = new Color[data_vertices.Length];
            waterVertices = new Vector3[data_vertices.Length];
            waterColors = new Color[data_vertices.Length];
            waterUVs = new Vector2[data_vertices.Length];
            genJobs = new Generation_Job[vertices.Length];
            genJobHandles = new JobHandle[vertices.Length];

            // Calculate parameters for the job
            Vector3 rotationMatrixAttrib = get_direction();
            Vector3 scaleMatrixAttrib = new Vector3(chunk.radius, chunk.radius, 1);
            // Create transform matrix
            Matrix4x4 transformMatrix = Matrix4x4.TRS(chunk.position, Quaternion.Euler(rotationMatrixAttrib), scaleMatrixAttrib);

            // populate biomes arrays
            for (int i = 0; i < 12; i++)
                biomes[i] = planetScript.biomes.packedBiomes[i];

            // Start multi-threaded generation
            for (int i = 0; i < genJobs.Length; i++)
            {
                // Copy data into job data buffer
                genJobs[i].vertices = data_vertices;
                genJobs[i].colors = data_colors;
                genJobs[i].uv1 = data_uv1;
                genJobs[i].uv2 = data_uv2;
                genJobs[i].uv3 = data_uv3;
                genJobs[i].uv4 = data_uv4;
                genJobs[i].waterVertices = data_waterVertices;
                genJobs[i].waterColors = data_waterColors;
                genJobs[i].terrainHeight = planetScript.planetDiameter;
                genJobs[i].waterHeight = planetScript.seaLevel;
                genJobs[i].localUp = localUp;
                genJobs[i].biomeSettings = planetScript.biomes.biomeSettings;
                genJobs[i].mBiomes = biomes;
                genJobs[i].mOceanBiome = planetScript.biomes.packedOceanBiome;
                genJobs[i].quadTemplate = planetScript.presets.quadTemplateVertices[quadIndex][i];
                genJobs[i].transformationMatrix = transformMatrix;
                genJobs[i].generateIndex = i;
                genJobs[i].axisA = chunk.axisA;
                genJobs[i].axisB = chunk.axisB;
                genJobs[i].drawTerrain = planetScript.drawTerrainFaces;
                genJobs[i].drawWater = planetScript.drawWaterFaces;
                // Schedule the job
                genJobHandles[i] = genJobs[i].Schedule();
                genJobHandles[i].Complete();
                // Copy the result from the buffer
                vertices[i] = data_vertices[i];
                colors[i] = data_colors[i];
                UV1[i] = data_uv1[i];
                UV2[i] = data_uv2[i];
                UV3[i] = data_uv3[i];
                UV4[i] = data_uv4[i];
                waterVertices[i] = data_waterVertices[i];
                waterColors[i] = data_waterColors[i];
            }
            // Copy triangles array from presets
            triangles = planetScript.presets.quadTemplateTriangles[quadIndex];
            waterUVs = planetScript.presets.quadTemplateUVs[quadIndex];
            // Copy the UVs array from presets
            data_vertices.Dispose();
            data_colors.Dispose();
            data_uv1.Dispose();
            data_uv2.Dispose();
            data_uv3.Dispose();
            data_uv4.Dispose();
            data_waterVertices.Dispose();
            data_waterColors.Dispose();
            biomes.Dispose();
        });
        return task;
    }

    // Return the correct rotation for the specified localUp
    private Vector3 get_direction()
    {
        // return rotation in degrees according to the local up vector
        if (localUp == Vector3.forward)
            return new Vector3(0, 0, 180);
        else if (localUp == Vector3.back)
            return new Vector3(0, 180, 0);
        else if (localUp == Vector3.right)
            return new Vector3(0, 90, 270);
        else if (localUp == Vector3.left)
            return new Vector3(0, 270, 270);
        else if (localUp == Vector3.up)
            return new Vector3(270, 0, 90);
        else if (localUp == Vector3.down)
            return new Vector3(90, 0, 270);
        else return new Vector3();
    }
}

// Generates all the data for a single point on the grid.
public struct Generation_Job : IJob
{
    // Terrain data (out)
    public NativeArray<Vector3> vertices;
    public NativeArray<Color> colors;
    public NativeArray<Vector2> uv1, uv2, uv3, uv4;

    // Water data (out)
    public NativeArray<Vector3> waterVertices;
    public NativeArray<Color> waterColors;

    // Biome data (in)
    public NativeArray<Biomes.PackedBiomeData> mBiomes;
    public Biomes.PackedBiomeData mOceanBiome;
    public Biomes.BiomeSettings biomeSettings;

    // Generation parameters (in)
    public bool drawTerrain, drawWater;
    public float terrainHeight, waterHeight;
    public Vector3 localUp, axisA, axisB;
    public Vector3 quadTemplate;
    public Matrix4x4 transformationMatrix;
    public int generateIndex;

    // This is called when the jobs are scheduled. 
    public void Execute()
    {
        if (drawTerrain) GenerateTerrain();
        if (drawWater) GenerateWater();
    }

    // Calculates the data used for drawing the terrain mesh.
    private void GenerateTerrain()
    {
        // Define the point used to sample noise
        Vector3 pointOnCube = transformationMatrix.MultiplyPoint(quadTemplate) / (terrainHeight / 2.0f);

        // Define the temperate mask and noise
        float temperateZoneMask = 1f - Mathf.Pow(Mathf.Abs(Vector3.Dot(pointOnCube.normalized, Vector3.up)), biomeSettings.temperateZoneScale);
        temperateZoneMask = Mathf.Clamp01(temperateZoneMask);
        float temperateNoise = NoiseGenerator.evaluate_noise(pointOnCube, biomeSettings.Biome_Temperate);

        // Define the fertility noise
        float fertilityNoise = NoiseGenerator.evaluate_noise(pointOnCube, biomeSettings.Biome_Fertility);

        // Rate the strength of each biome
        float[] biomeStrengths = ComputeBiomeIndex(temperateNoise * temperateZoneMask, fertilityNoise);

        // Assign those biome strengths for use in the shader
        colors[generateIndex] = new Color(biomeStrengths[0], biomeStrengths[1], biomeStrengths[2], biomeStrengths[3]);
        uv1[generateIndex] = new Vector2(biomeStrengths[4], biomeStrengths[5]);
        uv2[generateIndex] = new Vector2(biomeStrengths[6], biomeStrengths[7]);
        uv3[generateIndex] = new Vector2(biomeStrengths[8], biomeStrengths[9]);
        uv4[generateIndex] = new Vector2(biomeStrengths[10], biomeStrengths[11]);

        // Calculate the noise for the regular biomes
        float vertexDisplacement = 0.0f, sumStrength = 0.0f;
        for (int i = 0; i < 12; i++)
        {
            sumStrength += biomeStrengths[i];
            if (biomeStrengths[i] > 0.0f) // Minimize the number of noise samples!
                vertexDisplacement += ComputeNoise(pointOnCube, i) * biomeStrengths[i];
        }

        // Subtract the noise for the ocean biome (Ocaens get deeper as biomes fade)
        vertexDisplacement -= ComputeOceanNoise(pointOnCube) * (1f - Mathf.Clamp01(sumStrength / biomeSettings.oceanOvertakeFade));

        // Calculate vertex position
        vertices[generateIndex] = pointOnCube.normalized * ((terrainHeight / 2.0f) + vertexDisplacement);
    }

    // Calculates the data used for drawing the water mesh.
    private void GenerateWater()
    {
        // Define the point used to sample noise
        Vector3 pointOnCube = transformationMatrix.MultiplyPoint(quadTemplate) / waterHeight;
        waterVertices[generateIndex] = pointOnCube.normalized * waterHeight;

        // Calculate the slope of the ocean floor
        Vector2 terrainSlope = Gradient(pointOnCube);
        // A zero vector will result in the water not rendering!
        if (terrainSlope == Vector2.zero) waterColors[generateIndex] = new Color(1, 1, 0, 0);
        else waterColors[generateIndex] = new Color(-terrainSlope.x, -terrainSlope.y, terrainSlope.y, terrainSlope.x);
    }

    // Method for computing and combining all noise layers used by the specified biome.
    private float ComputeNoise(Vector3 position, int biomeIndex)
    {
        float tHeight = NoiseGenerator.evaluate_noise(position, mBiomes[biomeIndex].TerrainHeight);
        float tDetail = NoiseGenerator.evaluate_noise(position, mBiomes[biomeIndex].TerrainDetail);
        float mHeight = NoiseGenerator.evaluate_noise(position, mBiomes[biomeIndex].MountainHeight);
        float mDetail = NoiseGenerator.evaluate_noise(position, mBiomes[biomeIndex].MountainHeight);
        return tHeight + tDetail + mHeight + mDetail;
    }

    // Method for computing the noise for the ocean floor.
    private float ComputeOceanNoise(Vector3 position)
    {
        float tHeight = NoiseGenerator.evaluate_noise(position, mOceanBiome.TerrainHeight);
        float tDetail = NoiseGenerator.evaluate_noise(position, mOceanBiome.TerrainDetail);
        float mHeight = NoiseGenerator.evaluate_noise(position, mOceanBiome.MountainHeight);
        float mDetail = NoiseGenerator.evaluate_noise(position, mOceanBiome.MountainHeight);
        return Mathf.Abs(tHeight + tDetail + mHeight + mDetail);
    }

    // Closest we can get to the 'gradient' operation in calculus.
    // Returns the direction of greatest change in ocean depth.
    private Vector2 Gradient(Vector3 position)
    {
        float cHeight = ComputeOceanNoise(position);
        // 'partial derviative with respect to [local] x'
        float aHeight = ComputeOceanNoise(position + (axisA * 0.1f));
        // 'partial derivative with respect to [local] y'
        float bHeight = ComputeOceanNoise(position + (axisB * 0.1f));
        // Gradient operation returns direction of greatest change
        return new Vector2(aHeight - cHeight, bHeight - cHeight).normalized;
    }

    // https://devforum.roblox.com/t/biomes-with-perlin-noise/1743301/2 
    // Each biome can spawn in a per-determined range of temperature and fertility (distance from water)
    // This function takes those values and returns the biome index and strength
    private float[] ComputeBiomeIndex(float temperate, float fertility)
    {
        float[] biomeStrengths = new float[12]; 
        for (int i = 0; i < 12; i++)
            // if the temperature is within range
            if (temperate <= mBiomes[i].TemperateMinMax.y && temperate >= mBiomes[i].TemperateMinMax.x && 
                fertility <= mBiomes[i].FertilityMinMax.y && fertility >= mBiomes[i].FertilityMinMax.x)
                    biomeStrengths[i] = RateBiomeStrength(mBiomes[i].TemperateMinMax, temperate,
                        mBiomes[i].FertilityMinMax, fertility);
        return biomeStrengths; // if a biome cannot be selected return the default at max strength
    }

    // Returns a value between 0 and 1 based on how close to the center of the min/max range the value is.
    // 1 is returned when val is at the mid-point of the min/max range and fades to zero as the edges are approached.
    private float RateBiomeDimension(Vector2 minMax, float val)
    { // this graph should return 0 (or less, so clamp it) if value is outside of minmax
        // and return 1 near the mid range of the minmax
        float scale = 2f / (minMax.y - minMax.x);
        float center = ((minMax.y - minMax.x) / 2f) + minMax.x;
        return 1f - Mathf.Clamp01(Mathf.Pow(scale * (val - center), 4f));
    }

    // Combines the two biome ratings into a single value used for interpolating between biomes. 
    private float RateBiomeStrength(Vector2 tempMinMax, float temp, Vector2 fertMinMax, float fert)
    {
        // This value can be greater than one if conditions are very ideal, so it has to be clampped.
        return Mathf.Clamp01(new Vector2(RateBiomeDimension(tempMinMax, temp), 
                           RateBiomeDimension(fertMinMax, fert)).magnitude);
    }
}