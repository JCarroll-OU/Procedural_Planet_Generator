using UnityEngine;
using System;

// This will help generate a list of vertices that can be provided directly to Unity for rendering
// you can even use the cpu, compute shaders, etc. to modify the vertices before drawing them ;P
public class Presets
{
    public int quadRes = 16; // The resolution of the quads
    public Vector3[][] quadTemplateVertices = new Vector3[16][];
    public int[][] quadTemplateTriangles = new int[16][];
    public int[][] quadTemplateEdgeIndices = new int[16][]; // Keeps track of which indices are on the edge fans
    public Vector2[][] quadTemplateUVs = new Vector2[16][];

    public Presets(int res)
    {
        this.quadRes = res;
    }

    public void GenerateQuadTemplate()
    {
        Vector3[] selectedQuadTemplateVertices = new Vector3[] { };
        int[] selectedQuadTemplateTriangles = new int[] { };
        int[] selectedQuadTemplateEdgeIndices = new int[] { }; // 0 or 1 depending on if a vertex is on the edge of an edgefan or not
        Vector2[] selectedQuadTemplateUVs = new Vector2[] { };

        for (int quadI = 0; quadI < 16; quadI++)
        {
            selectedQuadTemplateVertices = new Vector3[(quadRes + 1) * (quadRes + 1)];
            selectedQuadTemplateTriangles = new int[quadRes * quadRes * 6];
            selectedQuadTemplateEdgeIndices = new int[(quadRes + 1) * (quadRes + 1)];
            selectedQuadTemplateUVs = new Vector2[(quadRes + 1) * (quadRes + 1)];

            // Vertices and UVs
            for (int y = 0; y < (quadRes + 1); y++)
            {
                for (int x = 0; x < (quadRes + 1); x++)
                {
                    Vector3 pos = new Vector3(x - quadRes / 2f, y - quadRes / 2f, 0) / (quadRes / 2);
                    Vector2 uv = new Vector2((float)x / (float)(quadRes + 1),
                                             (float)y / (float)(quadRes + 1));
                    selectedQuadTemplateVertices[y * (quadRes + 1) + x] = pos;
                    selectedQuadTemplateUVs[y * (quadRes + 1) + x] = uv;
                }
            }

            int offset = 0;

            // Edges
            for (int i = 0; i < quadRes / 2; i++)
            {
                // Top
                if (Array.Exists(new int[8] { 4, 5, 6, 7, 12, 13, 14, 15 }, p => p == quadI))
                {
                    selectedQuadTemplateTriangles[offset] = i * 2;
                    selectedQuadTemplateTriangles[offset + 1] = i * 2 + 2;
                    selectedQuadTemplateTriangles[offset + 2] = i * 2 + quadRes + 2;

                    // Store edge indices
                    selectedQuadTemplateEdgeIndices[selectedQuadTemplateTriangles[offset]] = 1;
                    selectedQuadTemplateEdgeIndices[selectedQuadTemplateTriangles[offset + 1]] = 1;

                    offset += 3;
                }
                else
                {
                    selectedQuadTemplateTriangles[offset] = i * 2;
                    selectedQuadTemplateTriangles[offset + 1] = i * 2 + 1;
                    selectedQuadTemplateTriangles[offset + 2] = i * 2 + quadRes + 2;

                    selectedQuadTemplateTriangles[offset + 3] = i * 2 + 1;
                    selectedQuadTemplateTriangles[offset + 4] = i * 2 + 2;
                    selectedQuadTemplateTriangles[offset + 5] = i * 2 + quadRes + 2;

                    offset += 6;
                }

                // Bottom
                if (Array.Exists(new int[8] { 8, 9, 10, 11, 12, 13, 14, 15 }, p => p == quadI))
                {
                    selectedQuadTemplateTriangles[offset] = (quadRes + 1) * (quadRes + 1) - quadRes * 2 + i * 2 - 1;
                    selectedQuadTemplateTriangles[offset + 1] = (quadRes + 1) * (quadRes + 1) - quadRes + i * 2 + 1;
                    selectedQuadTemplateTriangles[offset + 2] = (quadRes + 1) * (quadRes + 1) - quadRes + i * 2 - 1;

                    // Store edge indices
                    selectedQuadTemplateEdgeIndices[selectedQuadTemplateTriangles[offset + 1]] = 1;
                    selectedQuadTemplateEdgeIndices[selectedQuadTemplateTriangles[offset + 2]] = 1;

                    offset += 3;
                }
                else
                {
                    selectedQuadTemplateTriangles[offset] = (quadRes + 1) * (quadRes + 1) - quadRes * 2 + i * 2 - 1;
                    selectedQuadTemplateTriangles[offset + 1] = (quadRes + 1) * (quadRes + 1) - quadRes + i * 2;
                    selectedQuadTemplateTriangles[offset + 2] = (quadRes + 1) * (quadRes + 1) - quadRes + i * 2 - 1;

                    selectedQuadTemplateTriangles[offset + 3] = (quadRes + 1) * (quadRes + 1) - quadRes * 2 + i * 2 - 1;
                    selectedQuadTemplateTriangles[offset + 4] = (quadRes + 1) * (quadRes + 1) - quadRes + i * 2 + 1;
                    selectedQuadTemplateTriangles[offset + 5] = (quadRes + 1) * (quadRes + 1) - quadRes + i * 2;

                    offset += 6;
                }

                // Right
                if (Array.Exists(new int[8] { 1, 3, 5, 7, 9, 11, 13, 15 }, p => p == quadI))
                {
                    selectedQuadTemplateTriangles[offset] = quadRes * (i * 2 + 1) + i * 2;
                    selectedQuadTemplateTriangles[offset + 1] = quadRes * (i * 2 + 3) + i * 2 + 2;
                    selectedQuadTemplateTriangles[offset + 2] = quadRes * (i * 2 + 2) + i * 2;

                    // Store edge indices
                    selectedQuadTemplateEdgeIndices[selectedQuadTemplateTriangles[offset]] = 1;
                    selectedQuadTemplateEdgeIndices[selectedQuadTemplateTriangles[offset + 1]] = 1;

                    offset += 3;
                }
                else
                {
                    selectedQuadTemplateTriangles[offset] = quadRes * (i * 2 + 1) + i * 2;
                    selectedQuadTemplateTriangles[offset + 1] = quadRes * (i * 2 + 2) + i * 2 + 1;
                    selectedQuadTemplateTriangles[offset + 2] = quadRes * (i * 2 + 2) + i * 2;

                    selectedQuadTemplateTriangles[offset + 3] = quadRes * (i * 2 + 2) + i * 2;
                    selectedQuadTemplateTriangles[offset + 4] = quadRes * (i * 2 + 2) + i * 2 + 1;
                    selectedQuadTemplateTriangles[offset + 5] = quadRes * (i * 2 + 3) + i * 2 + 2;

                    offset += 6;
                }

                // Left
                if (Array.Exists(new int[8] { 2, 3, 6, 7, 10, 11, 14, 15 }, p => p == quadI))
                {
                    selectedQuadTemplateTriangles[offset] = quadRes * (i * 2) + i * 2;
                    selectedQuadTemplateTriangles[offset + 1] = quadRes * (i * 2 + 1) + i * 2 + 2;
                    selectedQuadTemplateTriangles[offset + 2] = quadRes * (i * 2 + 2) + i * 2 + 2;

                    // Store edge indices
                    selectedQuadTemplateEdgeIndices[selectedQuadTemplateTriangles[offset]] = 1;
                    selectedQuadTemplateEdgeIndices[selectedQuadTemplateTriangles[offset + 2]] = 1;

                    offset += 3;
                }
                else
                {
                    selectedQuadTemplateTriangles[offset] = quadRes * (i * 2) + i * 2;
                    selectedQuadTemplateTriangles[offset + 1] = quadRes * (i * 2 + 1) + i * 2 + 2;
                    selectedQuadTemplateTriangles[offset + 2] = quadRes * (i * 2 + 1) + i * 2 + 1;

                    selectedQuadTemplateTriangles[offset + 3] = quadRes * (i * 2 + 1) + i * 2 + 1;
                    selectedQuadTemplateTriangles[offset + 4] = quadRes * (i * 2 + 1) + i * 2 + 2;
                    selectedQuadTemplateTriangles[offset + 5] = quadRes * (i * 2 + 2) + i * 2 + 2;

                    offset += 6;
                }
            }

            // Transition
            for (int i = 0; i < quadRes / 2 - 1; i++)
            {
                // Top 1
                selectedQuadTemplateTriangles[offset] = (i + 1) * 2;
                selectedQuadTemplateTriangles[offset + 1] = quadRes + 3 + i * 2;
                selectedQuadTemplateTriangles[offset + 2] = quadRes + 2 + i * 2;

                // Top 2
                selectedQuadTemplateTriangles[offset + 3] = (i + 1) * 2;
                selectedQuadTemplateTriangles[offset + 4] = quadRes + 4 + i * 2;
                selectedQuadTemplateTriangles[offset + 5] = quadRes + 3 + i * 2;

                // Bottom 1
                selectedQuadTemplateTriangles[offset + 6] = (quadRes + 1) * (quadRes + 1) - quadRes * 2 + i * 2 - 1;
                selectedQuadTemplateTriangles[offset + 7] = (quadRes + 1) * (quadRes + 1) - quadRes * 2 + i * 2;
                selectedQuadTemplateTriangles[offset + 8] = (quadRes + 1) * (quadRes + 1) - quadRes + i * 2 + 1;

                // Bottom 2
                selectedQuadTemplateTriangles[offset + 9] = (quadRes + 1) * (quadRes + 1) - quadRes * 2 + i * 2;
                selectedQuadTemplateTriangles[offset + 10] = (quadRes + 1) * (quadRes + 1) - quadRes * 2 + i * 2 + 1;
                selectedQuadTemplateTriangles[offset + 11] = (quadRes + 1) * (quadRes + 1) - quadRes + i * 2 + 1;

                // Right 1
                selectedQuadTemplateTriangles[offset + 12] = quadRes * (i * 2 + 2) + i * 2;
                selectedQuadTemplateTriangles[offset + 13] = quadRes * (i * 2 + 3) + i * 2 + 2;
                selectedQuadTemplateTriangles[offset + 14] = quadRes * (i * 2 + 3) + i * 2 + 1;

                // Right 2
                selectedQuadTemplateTriangles[offset + 15] = quadRes * (i * 2 + 3) + i * 2 + 1;
                selectedQuadTemplateTriangles[offset + 16] = quadRes * (i * 2 + 3) + i * 2 + 2;
                selectedQuadTemplateTriangles[offset + 17] = quadRes * (i * 2 + 4) + i * 2 + 2;

                // Left 1
                selectedQuadTemplateTriangles[offset + 18] = quadRes * (i * 2 + 1) + i * 2 + 2;
                selectedQuadTemplateTriangles[offset + 19] = quadRes * (i * 2 + 2) + i * 2 + 3;
                selectedQuadTemplateTriangles[offset + 20] = quadRes * (i * 2 + 2) + i * 2 + 2;

                // Left 2
                selectedQuadTemplateTriangles[offset + 21] = quadRes * (i * 2 + 2) + i * 2 + 2;
                selectedQuadTemplateTriangles[offset + 22] = quadRes * (i * 2 + 2) + i * 2 + 3;
                selectedQuadTemplateTriangles[offset + 23] = quadRes * (i * 2 + 3) + i * 2 + 4;

                offset += 24;
            }

            // Middle
            int n = 0;
            int middleOffset = quadRes + 2;
            for (int y = 0; y < (quadRes - 2); y++)
            {
                for (int x = 0; x < (quadRes - 2); x++)
                {
                    n = y * (quadRes - 2) + x;
                    selectedQuadTemplateTriangles[offset] = middleOffset + n + y * 3;
                    selectedQuadTemplateTriangles[offset + 1] = middleOffset + n + 1 + y * 3;
                    selectedQuadTemplateTriangles[offset + 2] = middleOffset + n + quadRes + 1 + y * 3;

                    selectedQuadTemplateTriangles[offset + 3] = middleOffset + n + 1 + y * 3;
                    selectedQuadTemplateTriangles[offset + 4] = middleOffset + n + quadRes + 2 + y * 3;
                    selectedQuadTemplateTriangles[offset + 5] = middleOffset + n + quadRes + 1 + y * 3;
                    offset += 6;
                }
            }

            // Apply everything
            quadTemplateVertices[quadI] = selectedQuadTemplateVertices;
            quadTemplateTriangles[quadI] = selectedQuadTemplateTriangles;
            quadTemplateEdgeIndices[quadI] = selectedQuadTemplateEdgeIndices;
            quadTemplateUVs[quadI] = selectedQuadTemplateUVs;
        }
    }
}