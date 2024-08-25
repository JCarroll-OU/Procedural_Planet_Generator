using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// This is the heart of this planet generation system. Attach this script to a Unity GameObject at the 
// location you want the planet to form. This GameObject can then be moved to simulate orbits. 
public class Planet : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The position of this object will determine the distances used by the LOD system.")]
    public GameObject Player;
    [Tooltip("Used to change the physical properties of the terrain. Water does not have a collider and so it does not use a physic material.")]
    public PhysicMaterial physicsMaterial;
    [Tooltip("Material used by the terrain faces. If you plan to use the default shaders, assign the default terrain material here.")]
    public Material terrainMaterial;
    [Tooltip("Material used by the water faces. If you plan to use the default shaders, assign the default water material here.")]
    public Material waterMaterial;
    [Tooltip("Contains the textures, noise data, and biome size/location data. ")]
    public Biomes biomes;

    [Header("Generation Parameters")]
    [Tooltip("Number of vertices along the x and y axes of each plane used to generate terrain or water chunks.")]
    [Range(2, 32)]
    public int gridCellResolution = 10;
    [Tooltip("Strength of the tessellation on the terrain and water.")]
    [Range(1, 16)]
    public int tessellationFactor = 3;
    [Tooltip("Enables/Disables the generation and rendering of terrain.")]
    public bool drawTerrainFaces = true;
    [Tooltip("Enables/Disables the generation and rendering of water.")]
    public bool drawWaterFaces = true;

    [Header("Size and Mass Parameters")]
    [Tooltip("Diameter of the planet. Terrain height will be about half this distance from the center.")]
    public float planetDiameter = 1000f;
    [Tooltip("This value acts more like a radius, so it should be about half the planet diameter.")]
    public float seaLevel = 500f;
    [Tooltip("Calculate gravitational acceleration based on this number. For reference, Earth is ~6e+24 kg")]
    [Range(1e+10f, 1e+30f)]
    public float mass_kg = 1e+18f; // Used by the PlayerController class to compute the gravitational acceleration.

    [HideInInspector]
    public Presets presets;
    [SerializeField, HideInInspector]
    public PlanetFace[] planetFaces;
    [HideInInspector]   
    public GameObject terrainContainer, waterContainer;

    [Header("LOD System Parameters")]
    public float[] terrainDetailLevelDistances = new float[] {
        Mathf.Infinity,
        210f,
        100f,
        40f,
    };

    [Header("Other Settings")]
    [Tooltip("Regenerates the planet when settings are changed.")]
    public bool generateOnValidation = false;
    [Tooltip("When enabled terrain mesh colliders will be in convex mode. This will result in less accurate but more performant collision detection.")]
    public bool useConvexColliders = false;

    void Start()
    {
        GeneratePlanet();
        InvokeRepeating("UpdateTerrain", 1f, 0.25f);  //1s delay, repeat every 0.25s
        // Trying to update the terrain too quickly results in many errors since the meshes are written asynchronously!
    }

    void Initialize()
    {
        // Pack biome data for jobs system.
        biomes.PackBiomeData();

        // Generate the template grid cell using the specified resolution.
        presets = new Presets(gridCellResolution);
        presets.GenerateQuadTemplate();

        // Clear the children of this GameObject. i.e. delete the old stuff if it exists
        Clear();

        // Create a GameObject that will contain all terrain chunks.
        terrainContainer = new GameObject("Terrain");
        terrainContainer.transform.parent = transform;

        // Create a GameObject that will contain all water chunks.
        waterContainer = new GameObject("Water");
        waterContainer.transform.parent = transform;

        // Define the 6 faces that make up the surface of the planet. One for each direction.
        planetFaces = new PlanetFace[6];
        planetFaces[0] = new PlanetFace(Vector3.up, this); // but there is no up in space??
        planetFaces[1] = new PlanetFace(Vector3.down, this);
        planetFaces[2] = new PlanetFace(Vector3.left, this);
        planetFaces[3] = new PlanetFace(Vector3.right, this);
        planetFaces[4] = new PlanetFace(Vector3.forward, this);
        planetFaces[5] = new PlanetFace(Vector3.back, this);
    }

    void OnValidate()
    {
        if (generateOnValidation) GeneratePlanet();
    }

    private void Clear()
    { 
        // Create a list of all transforms to destroy.
        int count = transform.childCount;
        List<Transform> toDestroy = new List<Transform>();
        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            toDestroy.Add(child);
        }

        // Loop through that list and destroy them.
        foreach (Transform child in toDestroy)
        {
            if (child != null) // Shouldn't happen but let's not assume that.
            {
                child.gameObject.SetActive(false); // It make take a while to destroy, so at least stop it from rendering.
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        GameObject.DestroyImmediate(child.gameObject);
                    };
                #else
                    GameObject.Destroy(child.gameObject);
                #endif
            }
        }
    }

    public void GeneratePlanet()
    {
        Initialize();
        for (int i = 0; i < 6; i++)
            planetFaces[i].ConstructTree();
    }

    private void UpdateTerrain()
    {
        for (int i = 0; i < 6; i++)
            planetFaces[i].UpdateTree();
    }
}

[CustomEditor(typeof(Planet))]
class PlanetInspector : Editor
{
    // Add buttons to planet inspector.
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var planet = target as Planet;

        // Randomize the permutation used by the noise generator.
        bool randomizeSeed = EditorGUILayout.DropdownButton(new GUIContent("Randomize Seed"), FocusType.Passive);
        if (randomizeSeed)
        {
            NoiseGenerator.RandomizePerm();
            planet.GeneratePlanet();
        }

        // Randomize the noise settings that relate to the vertex displacement.
        bool randomizeDisplacement = EditorGUILayout.DropdownButton(new GUIContent("Randomize Displacement"), FocusType.Passive);
        if (randomizeDisplacement)
        {
            for (int i = 0; i < planet.biomes.BiomeList.Length; i++)
                planet.biomes.BiomeList[i].RandomizeDisplacement(planet.planetDiameter);
            planet.biomes.OceanBiome.RandomizeDisplacement(planet.planetDiameter);

            planet.GeneratePlanet();
        }

        // Randomize the noise settings that relate to the biome size/location.
        bool randomizeBiomes = EditorGUILayout.DropdownButton(new GUIContent("Randomize Biomes"), FocusType.Passive);
        if (randomizeBiomes)
        {
            planet.biomes.biomeSettings.RandomizeBiomeSettings();
            planet.GeneratePlanet();
        }

        // Pack the individual textures defined in the biome settings into an array that the shader can use.
        bool generateTextArrs = EditorGUILayout.DropdownButton(new GUIContent("Pack Biome Data"), FocusType.Passive);
        if (generateTextArrs) planet.biomes.PackBiomeTextures(); // This takes a while, so be careful!

        // Generate the planet, useful when 'generateOnValidation' is false.
        bool generate = EditorGUILayout.DropdownButton(new GUIContent("Generate Planet"), FocusType.Passive);
        if (generate)
            planet.GeneratePlanet();
    }
}