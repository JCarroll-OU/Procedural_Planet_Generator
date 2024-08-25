using UnityEngine;

// This class controls the LOD system. It dispatches jobs using the optimizer and is controlled by the 
// PlanetFace script. It utilizes a quad-tree to manage the sub-divisions of the terrain and water faces.
public class Chunk : MonoBehaviour
{
    public uint hashvalue; // First bit is not used for anything but preserving zeros in the beginning

    // misc. references
    public Planet planetScript;
    public PlanetFace terrainFace;

    // Quad-tree system data
    public Chunk[] children;
    public int detailLevel;
    public Vector3 localUp;
    public Vector3 axisA;
    public Vector3 axisB;
    public byte corner;

    // size and position data
    public Vector3 position;
    public float radius;
    public Vector3 normalizedPos;

    // mesh data for terrain
    public GameObject terrainGameObject;
    public volatile Mesh terrainMesh;
    public MeshFilter terrainMeshFilter;

    // mesh data for water
    public GameObject waterGameObject;
    public volatile Mesh waterMesh;
    public MeshFilter waterMeshFilter;

    // misc. data
    public byte[] neighbours = new byte[4]; //East, west, north, south. True if less detailed (Lower LOD)
    private static uint idSpecifier = 0;
    public uint ID;
    public Optimizer terrainOptimizer;

    public Chunk(uint hashvalue, Planet planetScript, PlanetFace terrainFace, Chunk[] children, Vector3 position, float radius, int detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB, byte[] neighbours, byte corner)
    {
        this.ID = idSpecifier;
        idSpecifier++;
        this.hashvalue = hashvalue;
        this.planetScript = planetScript;
        this.terrainFace = terrainFace;
        this.children = children;
        this.position = position;
        this.radius = radius;
        this.detailLevel = detailLevel;
        this.localUp = localUp;
        this.axisA = axisA;
        this.axisB = axisB;
        this.neighbours = neighbours;
        this.corner = corner;
        this.normalizedPos = position.normalized;
        terrainOptimizer = new Optimizer(planetScript, terrainFace, localUp);
    }

    // Creates new higher detail children if within range and reason and removes old (overly-detailed) children.
    public void GenerateChildren()
    {
        // If the detail level is under max level and above 0. Max level depends on how many detail levels are defined in planets and needs to be changed manually.
        if (detailLevel <= planetScript.terrainDetailLevelDistances.Length - 1 && detailLevel >= 0)
        {
            // Find the average distance to the center point of any new children that would be generated
            // Not the best way of doing this; it does not scale with very large (Earth-sized) planets!
            Vector3 position0 = position + axisA * radius * 0.5f - axisB * radius * 0.5f;
            Vector3 position1 = position + axisA * radius * 0.5f + axisB * radius * 0.5f;
            Vector3 position2 = position - axisA * radius * 0.5f + axisB * radius * 0.5f;
            Vector3 position3 = position - axisA * radius * 0.5f - axisB * radius * 0.5f;
            float minDst = Mathf.Min(Vector3.Distance(planetScript.Player.transform.position, position0),
                    Vector3.Distance(planetScript.Player.transform.position, position1));
            minDst = Mathf.Min(Vector3.Distance(planetScript.Player.transform.position, position2), minDst);
            minDst = Mathf.Min(Vector3.Distance(planetScript.Player.transform.position, position3), minDst);
            if (minDst <= planetScript.terrainDetailLevelDistances[detailLevel])
            {
                if (children == null || children.Length == 0)
                {
                    children = new Chunk[4];
                    children[0] = new Chunk(hashvalue * 4, planetScript, terrainFace, new Chunk[0], position0, radius * 0.5f, detailLevel + 1, localUp, axisA, axisB, new byte[4], 0); // TOP LEFT
                    children[1] = new Chunk(hashvalue * 4 + 1, planetScript, terrainFace, new Chunk[0], position1, radius * 0.5f, detailLevel + 1, localUp, axisA, axisB, new byte[4], 1); // TOP RIGHT
                    children[2] = new Chunk(hashvalue * 4 + 2, planetScript, terrainFace, new Chunk[0], position2, radius * 0.5f, detailLevel + 1, localUp, axisA, axisB, new byte[4], 2); // BOTTOM RIGHT
                    children[3] = new Chunk(hashvalue * 4 + 3, planetScript, terrainFace, new Chunk[0], position3, radius * 0.5f, detailLevel + 1, localUp, axisA, axisB, new byte[4], 3); // BOTTOM LEFT
                }

                ClearMesh();
                 
                // Create grandchildren
                foreach (Chunk child in children)
                    child.GenerateChildren();
            }
            else
            {
                ClearChildren();
            }
        }
    }

    // Returns the latest chunk in every branch, i.e. the ones to be rendered.
    public async void DrawVisibleChildren()
    {
        if (children.Length > 0)
        {
            foreach (Chunk child in children)
                child.DrawVisibleChildren();
        }
        else
        { 
            if (terrainGameObject == null)
            {
                GetNeighbourLOD();
                await terrainOptimizer.Dispatch_Jobs(this);
                terrainFace.ConstructMesh(this);
            }
        }
    }

    // Returns the LOD level of the neighboring chunks.
    public void GetNeighbourLOD()
    {
        byte[] newNeighbours = new byte[4];

        if (corner == 0) // Top left
        {
            newNeighbours[1] = CheckNeighbourLOD(1, hashvalue); // West
            newNeighbours[2] = CheckNeighbourLOD(2, hashvalue); // North
        }
        else if (corner == 1) // Top right
        {
            newNeighbours[0] = CheckNeighbourLOD(0, hashvalue); // East
            newNeighbours[2] = CheckNeighbourLOD(2, hashvalue); // North
        }
        else if (corner == 2) // Bottom right
        {
            newNeighbours[0] = CheckNeighbourLOD(0, hashvalue); // East
            newNeighbours[3] = CheckNeighbourLOD(3, hashvalue); // South
        }
        else if (corner == 3) // Bottom left
        {
            newNeighbours[1] = CheckNeighbourLOD(1, hashvalue); // West
            newNeighbours[3] = CheckNeighbourLOD(3, hashvalue); // South
        }

        neighbours = newNeighbours;
    }

    // Clear the terrain and water GameObjects and meshes.
    private void ClearMesh()
    {
        if (terrainGameObject != null)
        {
            terrainGameObject.SetActive(false);
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    GameObject.DestroyImmediate(terrainGameObject);
                };
            #else
                GameObject.Destroy(terrainGameObject);
            #endif
        }
        if (terrainMesh != null) terrainMesh.Clear();

        if (waterGameObject != null)
        {
            waterGameObject.SetActive(false);
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    GameObject.DestroyImmediate(waterGameObject);
                };
            #else
                GameObject.Destroy(waterGameObject);
            #endif
        }
        if (waterMesh != null) waterMesh.Clear();
    }

    // Properly removes the old, unwanted children i.e. adults ;( 
    private void ClearChildren()
    {
        foreach (Chunk child in children)
        {
            if (child.children != null || children.Length > 0)
                child.ClearChildren();
            child.ClearMesh();
        }
        children = new Chunk[0];
    }

    // Find neighbouring chunks by applying a partial inverse bitmask to the hash
    private byte CheckNeighbourLOD(byte side, uint hash)
    {
        uint bitmask = 0;
        byte count = 0;
        uint twoLast;

        while (count < detailLevel * 2) // 0 through 3 can be represented as a two bit number
        {
            count += 2;
            twoLast = (hash & 3); // Get the two last bits of the hash. 0b_10011 --> 0b_11

            bitmask = bitmask * 4; // Add zeroes to the end of the bitmask. 0b_10011 --> 0b_1001100

            // Create mask to get the quad on the opposite side. 2 = 0b_10 and generates the mask 0b_11 which flips it to 1 = 0b_01
            if (side == 2 || side == 3)
            {
                bitmask += 3; // Add 0b_11 to the bitmask
            }
            else
            {
                bitmask += 1; // Add 0b_01 to the bitmask
            }

            // Break if the hash goes in the opposite direction
            if ((side == 0 && (twoLast == 0 || twoLast == 3)) ||
                (side == 1 && (twoLast == 1 || twoLast == 2)) ||
                (side == 2 && (twoLast == 3 || twoLast == 2)) ||
                (side == 3 && (twoLast == 0 || twoLast == 1)))
            {
                break;
            }

            // Remove already processed bits. 0b_1001100 --> 0b_10011
            hash = hash >> 2;
        }

        // Return 1 (true) if the quad in quadstorage is less detailed
        if (terrainFace.parentChunk.GetNeighbourDetailLevel(hashvalue ^ bitmask, detailLevel) < detailLevel)
            return 1;
        else return 0;
    }

    // Find the detail level of the neighbouring quad using the querryHash as a map
    public int GetNeighbourDetailLevel(uint querryHash, int dl)
    {
        int dlResult = 0; // dl = detail level

        if (hashvalue == querryHash)
            dlResult = detailLevel;
        else
            if (children.Length > 0)
            dlResult += children[((querryHash >> ((dl - 1) * 2)) & 3)].GetNeighbourDetailLevel(querryHash, dl - 1);

        return dlResult; // Returns 0 if no quad with the given hash is found
    }
}