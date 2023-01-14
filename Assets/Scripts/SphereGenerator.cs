using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEngine.EventSystems.EventTrigger;

public class SphereGenerator : MonoBehaviour
{
    public ComputeShader marchShader;
    public SphereDensity sphereDensity;

    const int threadsGroupSize = 8;
    [Header("Voxel Setting")]
    public float isoLevel;
    public int boundsSize = 1;
    public Vector3 offset = Vector3.zero;
    [Range(2, 100)]
    public int numPointsPerAxis;
    public bool canCollide;
    public Material mat;
    public float radius;
    public bool isInterpolate;

    private bool hasCollider;

    Vector4[] pointsToUpdate;

    [Header("Debugging")]
    public Transform pointPrefab;
    public Color activeColor;
    public Color unactiveColor;

    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;


    struct Triangle
    {
        Vector3 a;
        Vector3 b;
        Vector3 c;

        public Vector3 this[float index]
        {
            get {
                switch (index)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }   
    }

    GameObject meshObject;
    GameObject pointsHolder;

    int indexFromCoord(float x, float y,float z)
    {
        return Mathf.CeilToInt(x + y * numPointsPerAxis + z * numPointsPerAxis * numPointsPerAxis);
    }

    private void Awake()
    {
        UpdateMesh();
    }

    public void UpdateMesh()
    {
        if (meshObject == null)
        {
            CreateMeshObject();
        }

        CreateBuffers();
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float) threadsGroupSize);
        int pointSpacing = boundsSize / numVoxelsPerAxis;

        // Calculate PointsBuffer Density
        Vector3 centre = Vector3.zero;
        sphereDensity.Generate(pointsBuffer, numPointsPerAxis, radius, pointSpacing, centre, boundsSize);

        SetBuffers();

        // Calculate triangles
        marchShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        int numTris = GetNumTrisFromTriangleBuffer();

        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        // Set the mesh data
        meshObject.GetComponent<MeshFilter>().mesh = new Mesh();
        Mesh mesh = meshObject.GetComponent<MeshFilter>().sharedMesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        SetVertAndTri(tris, vertices, meshTriangles, numTris);

        ReleaseBuffers();

        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }

    void SetVertAndTri(Triangle[] outputTris, Vector3[] vertices, int[] meshTriangles, int numTris)
    {
        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = outputTris[i][j];
            }
        }
    }

    int GetNumTrisFromTriangleBuffer()
    {
        // Get the number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0); // GTet the triangleBuffer count and copy to triCountBuffer
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray); // Set the triangle counts to the array (the only way to set data)
        int numTris = triCountArray[0];
        return numTris;
    }
    
    void CreatePointDebuggerObjects()
    {
        // Instantiate point objects
        for (int i = 0; i < pointsToUpdate.Length; i++)
        {
            if (pointPrefab != null)
            {
                Vector3 currentPos = new Vector3(pointsToUpdate[i].x, pointsToUpdate[i].y, pointsToUpdate[i].z);
                Transform point = Instantiate(pointPrefab, currentPos, Quaternion.identity);
                point.parent = pointsHolder.transform;
            }
        }
    }

    void SetBuffers()
    {
        // Clear old triangle buffers
        triangleBuffer.SetCounterValue(0);
        marchShader.SetBuffer(0, "points", pointsBuffer);
        marchShader.SetBuffer(0, "triangles", triangleBuffer);
        marchShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        marchShader.SetFloat("isoLevel", isoLevel);
        marchShader.SetBool("isInterpolate", isInterpolate);
    }

    void CreateMeshObject()
    {
        meshObject = new GameObject("Mesh Object", typeof(MeshFilter), typeof(MeshRenderer));
        if (hasCollider) {
            meshObject.AddComponent<MeshCollider>();
            meshObject.GetComponent<MeshCollider>().convex = true;
        }

        meshObject.GetComponent<Renderer>().sharedMaterial = mat;

        if (pointsHolder == null)
        {
            pointsHolder = new GameObject("Points Holder");
        }
    }

    void CreateBuffers()
    {
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        /*if (!Application.isPlaying || pointsBuffer == null || numPoints != pointsBuffer.count) {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }
        }*/
        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }

    private void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();
        }
    }

    private void OnDrawGizmosSelected()
    {
        int pointSpacing = boundsSize / (numPointsPerAxis - 1);
        Vector3 centre = Vector3.zero;
        pointsToUpdate = new Vector4[numPointsPerAxis * numPointsPerAxis * numPointsPerAxis];
        ComputeBuffer generatedPointsBuffer = new ComputeBuffer(numPointsPerAxis * numPointsPerAxis * numPointsPerAxis, sizeof(float) * 4);
        sphereDensity.Generate(generatedPointsBuffer, numPointsPerAxis, radius, pointSpacing, centre, boundsSize);
        generatedPointsBuffer.GetData(pointsToUpdate);

        foreach (var point in pointsToUpdate)
        {
            Gizmos.DrawWireSphere(point, 0.5f);
        }

        generatedPointsBuffer.Release();
    }
}
