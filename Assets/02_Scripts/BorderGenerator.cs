using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// All this class is like a nice to have. It is not required for the challenge 
// but it is something that could add a lot of appealing to the terrain mesh.
// This was wrote to only work with the challenge data of timining. It may fail with other similar meshes.
// Also this is just a quick way to do something like this, so some artifacts are expected.
public class BorderGenerator : MonoBehaviour
{
    [SerializeField]
    private Material _borderMaterial = null;

    private Mesh _generatedMesh = null;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        _meshFilter   = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        if (_meshFilter == null)
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
    }

    // This function should generate a border for the sourceMesh. This function was not tested with other meshes 
    // so is probable that will not work if it is not the TiMining input mesh.
    public bool GenerateBorderMesh(Mesh sourceMesh)
    {
        // sourceMesh should be set as redeable.
        if (sourceMesh.isReadable)
        {
            var borderPolygon = new List<Vector3>();
            // Get Border Loop.
            if (!GetBorderLoop(sourceMesh, borderPolygon))
            {
                Debug.LogError("Border Generator Failed to get Border Loop");
                return false;
            }

            // Generate Border Geometry
            return GenerateBorders(sourceMesh, borderPolygon);
        }

        Debug.LogError("Border Generator Source Mesh is not readable");

        return false;
    }

    // Returns as parameter a closed loop that contains the border of sourceMesh mesh.
    // Returns true if was sucessful false otherwise.
    private bool GetBorderLoop(Mesh sourceMesh, List<Vector3> outPolygonVertexList)
    {
        Dictionary<Vector2Int, int> edgeList = new Dictionary<Vector2Int, int>();
        List<Vector2Int> borderEdgeList = new List<Vector2Int>();

        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();

        sourceMesh.GetTriangles(triangles, 0);
        sourceMesh.GetVertices(vertices);

        // For each triangle generate all edges
        for (int a = 0; a < triangles.Count; a += 3)
        {
            Vector2Int edge1 = new Vector2Int(triangles[a], triangles[a + 1]);
            Vector2Int edge2 = new Vector2Int(triangles[a + 1], triangles[a + 2]);
            Vector2Int edge3 = new Vector2Int(triangles[a + 2], triangles[a]);

            AddEdgeToSet(edgeList, edge1);
            AddEdgeToSet(edgeList, edge2);
            AddEdgeToSet(edgeList, edge3);
        }

        // Just consider the edges that are used one time as they are border edge candidates.
        foreach (var edge in edgeList)
        {
            if (edge.Value == 1)
            {
                borderEdgeList.Add(edge.Key);
            }
        }

        // Some vertices may be duplicated so all duplicated edges are removed.
        // Duplicated vertex may happen in the seams of the UV Mapping since
        // sharing the vertex if not possible.
        List<Vector2Int> duplicatedEdgeList = new List<Vector2Int>();
        foreach (var edge1 in borderEdgeList)
        {
            foreach (var edge2 in borderEdgeList)
            {
                if(edge1 != edge2)
                {
                    // Comparing these vector3 is ok. They use some epsilon for the comparison.
                    if((vertices[edge1.x] == vertices[edge2.x] ||
                        vertices[edge1.x] == vertices[edge2.y]) &&
                        (vertices[edge1.y] == vertices[edge2.x] ||
                        vertices[edge1.y] == vertices[edge2.y])
                        )
                        {
                        // Mark the current edge as duplicated.
                        duplicatedEdgeList.Add(edge1);
                        duplicatedEdgeList.Add(edge2);
                        break;
                    }   
                }
            }
        }

        // Remove edges marked as duplicated.
        foreach (var edge in duplicatedEdgeList)
        {
            borderEdgeList.Remove(edge);
        }

        // Discard loops of less than 3 edges.
        if (borderEdgeList.Count < 3)
        {
            Debug.LogError("GetBorderLoop Invalid border Edge Count");
            return false;
        }

        // Traverse all edges to generate a closed vertex loop.
        HashSet<Vector2Int> visitedEdges = new HashSet<Vector2Int>();

        outPolygonVertexList.Clear();

        var startEdge = borderEdgeList[0];

        var startVertex = vertices[startEdge.x];
        var currentVertex = vertices[startEdge.x];

        visitedEdges.Add(startEdge);
        outPolygonVertexList.Add(startVertex);

        // Number of iterations before cancelling to prevent infinite loops.
        int giveupIterations = borderEdgeList.Count * 2;

        while (true)
        {
            // Follow the edge loop
            foreach (var edge in borderEdgeList)
            {
                if (!visitedEdges.Contains(edge))
                {
                    if (currentVertex == vertices[edge.x])
                    {
                        currentVertex = vertices[edge.y];
                        visitedEdges.Add(edge);
                        outPolygonVertexList.Add(currentVertex);
                    }
                    else if (currentVertex == vertices[edge.y])
                    {
                        currentVertex = vertices[edge.x];
                        visitedEdges.Add(edge);
                        outPolygonVertexList.Add(currentVertex);
                    }
                }
            }


            // There is not more edges to traverse return what we have
            if (visitedEdges.Count == borderEdgeList.Count)
            {
                break;
            }

            giveupIterations--;
            if (giveupIterations < 0)
            {
                Debug.LogError("GetBorderLoop Was not able to find the solution in enough iterations.");
                return false;
            }
        }
        
        return true;
    }

    // Function that generate the actual border mesh using the input 
    // polygon loop
    private bool GenerateBorders(Mesh sourceMesh,List<Vector3> baseList)
    {
        _generatedMesh = new Mesh();
 
        Vector3[] vertices = new Vector3[baseList.Count * 4];
        Vector3[] normals  = new Vector3[baseList.Count * 4];
        Vector2[] texUVs   = new Vector2[baseList.Count * 4];
        Color[] colors     = new Color[baseList.Count * 4];
        int[] triIndices   = new int[baseList.Count * 6];

        int currentTriIndex = 0;
        float lineAdvance = 0;

        for(int a = 0; a < baseList.Count;a++)
        { 
            int index = a * 4;

            if (a == baseList.Count - 1)
            {
                vertices[index + 0] = baseList[a];
                vertices[index + 1] = new Vector3(baseList[a].x, -10, baseList[a].z);
                vertices[index + 2] = new Vector3(baseList[0].x, -10, baseList[0].z);
                vertices[index + 3] = baseList[0];
            }
            else
            {
                vertices[index + 0] = baseList[a];
                vertices[index + 1] = new Vector3(baseList[a].x, -10, baseList[a].z);
                vertices[index + 2] = new Vector3(baseList[a + 1].x, -10, baseList[a + 1].z);
                vertices[index + 3] = baseList[a + 1];
            }

            // Vector pointing from current vertex to next vertex.
            var moveVector = vertices[index + 3] - vertices[index + 0];

            var normal = Vector3.Cross(moveVector.normalized, Vector3.down);

            normals[index + 0] = normal;
            normals[index + 1] = normal;
            normals[index + 2] = normal;
            normals[index + 3] = normal;

            var advDist = (moveVector).magnitude;

            texUVs[index + 0]   = new Vector2(lineAdvance, vertices[index + 0].y);
            texUVs[index + 1] = new Vector2(lineAdvance, vertices[index + 1].y);
            texUVs[index + 2] = new Vector2(lineAdvance + advDist, vertices[index + 2].y);
            texUVs[index + 3] = new Vector2(lineAdvance + advDist, vertices[index + 3].y);

            lineAdvance += advDist;

            triIndices[currentTriIndex + 0] = index + 1;
            triIndices[currentTriIndex + 1] = index + 0;
            triIndices[currentTriIndex + 2] = index + 3;

            triIndices[currentTriIndex + 3] = index + 1;
            triIndices[currentTriIndex + 4] = index + 3;
            triIndices[currentTriIndex + 5] = index + 2;

            currentTriIndex += 6;
        }

        _generatedMesh.vertices = vertices;
        _generatedMesh.uv = texUVs;
        _generatedMesh.normals = normals;
        // TODO: Add Color?
        //mesh.colors = colors;
        _generatedMesh.triangles = triIndices;

        _meshFilter.sharedMesh = _generatedMesh;
        _meshRenderer.sharedMaterial = _borderMaterial;

        return true;
    }

    // Add an edge to the set. An edge at this stage do not have direction so 1,0 is the same as 0,1.
    private static void AddEdgeToSet(Dictionary<Vector2Int, int> set, Vector2Int edge)
    {
        Vector2Int invertedEdge = new Vector2Int(edge.y, edge.x);

        if (set.ContainsKey(edge))
        {
            set[edge] += 1;
        }
        else if (set.ContainsKey(invertedEdge))
        {
            set[invertedEdge] += 1;
        }
        else
        {
            set.Add(edge, 1);
        }
    }
}
