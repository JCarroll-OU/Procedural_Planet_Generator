using Unity.Mathematics;
using UnityEngine;

// Class for computing noise using the settings defined below.
public class Noise
{
    // Supported noise types.
    public enum NoiseType
    {
        Perlin = 0,
        Cellular = 1
    }

    [System.Serializable]
    public struct NoiseSettings
    {
        public NoiseType type;
        [Tooltip("When using cellular noise and isRigid is enabled, cells are used in place of the regular noise.")]
        public bool isRigid;
        [Tooltip("Amount to add to the vertex position of each sample. This will act a bit like a 'seed' value.")]
        public Vector3 offset;
        [Tooltip("Lower values result in smoother and less varied noise whereas larger values result in large and rapid variations.")]
        public float frequency;
        [Tooltip("Number of times to process the noise during each sample.")]
        public int octaves;
        [Tooltip("Lower values result in smoother noise with less variation in amplitude. Larger values will result in large changes in noise amplitude, and thus terrain height.")]
        public float strength;
        [Tooltip("Value between 0 and 1 describing how much each octave contributes to the overall noise. Each successive octave contributes less to the overall output.")]
        public float persistence;
        [Tooltip("Value describing how the frequency is affected with each successive octive, resulting in each octave providing more detail to the overall noise. Typicaly between 1 and 4.")]
        public float lacunarity;
    }
}

// Static class containing all methods for generating terrain noise.
public class NoiseGenerator
{
    // Simple wrapper for evaluating noise at a point. 
    public static float evaluate_noise(Vector3 v, Noise.NoiseSettings settings)
    {
        if (settings.type == Noise.NoiseType.Perlin)
            return perlin_noise(v, settings);
        else
        {
            if (settings.isRigid) return cellular_noise(v, settings).y;
            else return cellular_noise(v, settings).x;
        }
    }

    // Wrapper for evaluating only perlin noise at a point.
    public static float perlin_noise(Vector3 v, Noise.NoiseSettings settings)
    {
        if (settings.isRigid)
            return perlin_rigid_noise(v + settings.offset, settings.frequency, settings.octaves, settings.strength,
                settings.persistence, settings.lacunarity);
        else return perlin_noise(v + settings.offset, settings.frequency, settings.octaves, settings.strength,
                settings.persistence, settings.lacunarity);
    }

    // Method for evaluating perlin noise using the specified parameters.
    public static float perlin_noise(Vector3 v, float freq, int oct,
        float strength, float persistence, float lacunarity)
    {
        float sumNoise = 0.0f;
        float amplitude = 1;
        float frequency = freq;
        for (int i = 0; i < oct; i++)
        {
            sumNoise += cnoise(v * frequency) * amplitude;
            frequency *= lacunarity;
            amplitude *= persistence;
        }
        return strength * sumNoise;
    }

    // Method for evaluating rigid perlin noise using the specified parameters.
    public static float perlin_rigid_noise(Vector3 v, float freq, int oct,
        float strength, float persistence, float lacunarity)
    {
        float sumNoise = 0.0f;
        float amplitude = 1;
        float frequency = freq;
        for (int i = 0; i < oct; i++)
        {
            sumNoise += cnoise(v * frequency) * amplitude;
            frequency *= lacunarity;
            amplitude *= persistence;
        }
        return Mathf.Abs(-strength * sumNoise);
    }

    // Wrapper for evaluating only cellular noise at a point.
    public static float2 cellular_noise(Vector3 v, Noise.NoiseSettings settings)
    {
        return cellular_noise(v + settings.offset, settings.frequency, settings.octaves, settings.strength,
                settings.persistence, settings.lacunarity);
    }

    // Method for evaluating cellular noise using the specifed parameters.
    public static float2 cellular_noise(Vector3 v, float freq, int oct,
        float strength, float persistence, float lacunarity)
    {
        // x-component is noise and y-component is cells
        float2 sumNoise = 0.0f;
        float amplitude = 1;
        float frequency = freq;
        for (int i = 0; i < oct; i++)
        {
            // x-component is noise and y-component is cells 
            sumNoise += noise.cellular2x2x2(v * frequency) * amplitude;
            frequency *= lacunarity;
            amplitude *= persistence;
        }
        return strength * sumNoise;
    }

    // Wrapper for 'cnoise' function allowing Vector3 inputs.
    private static float cnoise(Vector3 vec)
    {
        return cnoise(vec.x, vec.y, vec.z);
    }

    // Lower-level perlin noise calculation
    private static float cnoise(float x, float y, float z)
    {
        var X = Mathf.FloorToInt(x) & 0xff;
        var Y = Mathf.FloorToInt(y) & 0xff;
        var Z = Mathf.FloorToInt(z) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        z -= Mathf.Floor(z);
        var u = Fade(x);
        var v = Fade(y);
        var w = Fade(z);
        var A = (perm[X] + Y) & 0xff;
        var B = (perm[X + 1] + Y) & 0xff;
        var AA = (perm[A] + Z) & 0xff;
        var BA = (perm[B] + Z) & 0xff;
        var AB = (perm[A + 1] + Z) & 0xff;
        var BB = (perm[B + 1] + Z) & 0xff;
        return Lerp(w, Lerp(v, Lerp(u, Grad(perm[AA], x, y, z), Grad(perm[BA], x - 1, y, z)),
                               Lerp(u, Grad(perm[AB], x, y - 1, z), Grad(perm[BB], x - 1, y - 1, z))),
                       Lerp(v, Lerp(u, Grad(perm[AA + 1], x, y, z - 1), Grad(perm[BA + 1], x - 1, y, z - 1)),
                               Lerp(u, Grad(perm[AB + 1], x, y - 1, z - 1), Grad(perm[BB + 1], x - 1, y - 1, z - 1))));
    }

    // Used by the perlin noise function: 'cnoise'
    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    // Used by the perlin noise function: 'cnoise'
    private static float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    // Used by the perlin noise function: 'cnoise'
    private static float Grad(int hash, float x, float y, float z)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    // This is basically the seed for the noise generator.
    private static int[] perm = {
            151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
            88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
            77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
            135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
            5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
            129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
            49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
            151
        };

    // Min/Max values used for the RND seed when randomizing the 'perm' variable. 
    private static float SEED_RANGE = (float)(0xFFFFFFFF);
    public static void RandomizePerm()
    {
        UnityEngine.Random.InitState((int)UnityEngine.Random.Range(-SEED_RANGE, SEED_RANGE));
        int length = perm.Length;
        int[] newPerm = new int[length];
        for (int i = 0; i < length; i++)
        {
            newPerm[i] = (int)UnityEngine.Random.Range(0, 255);
        }
        perm = newPerm;
    }
}