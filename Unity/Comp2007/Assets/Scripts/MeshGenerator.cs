using UnityEngine;

public class MeshGenetator : MonoBehaviour
{
    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = GeneratePlaneMesh();
    }
    
    public static Mesh GeneratePlaneMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Plane";

        // Calculate dimensions for ~21000 vertices (145x145 grid = 21025 vertices)
        int width = 145;
        int height = 145;
    
        // Create vertices array
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uvs = new Vector2[width * height];
    
        // Calculate steps to distribute vertices
        float xStep = 10f / (width - 1); // Made plane 10 units wide
        float zStep = 10f / (height - 1); // Made plane 10 units long

        // Generate vertices and UVs
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                vertices[index] = new Vector3(x * xStep - 5f, 0, z * zStep - 5f);
                uvs[index] = new Vector2(x * xStep / 10f, z * zStep / 10f);
            }
        }

        // Create triangles (200000 triangles = 600000 indices)
        int[] triangles = new int[600000];
        int triIndex = 0;

        // Generate triangles
        for (int z = 0; z < height - 1 && triIndex < triangles.Length - 6; z++)
        {
            for (int x = 0; x < width - 1 && triIndex < triangles.Length - 6; x++)
            {
                int vertIndex = z * width + x;
            
                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex + width;
                triangles[triIndex + 2] = vertIndex + 1;

                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + width;
                triangles[triIndex + 5] = vertIndex + width + 1;

                triIndex += 6;
            }
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
    
        // Recalculate normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}