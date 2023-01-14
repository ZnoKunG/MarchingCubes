using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereDensity : MonoBehaviour
{
    const int threadsGroupSize = 8;
    public ComputeShader densityShader;

    public ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPointsPerAxis, float radius, float spacing, Vector3 centre, float boundsSize)
    {
        int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float) threadsGroupSize);

        densityShader.SetBuffer(0, "points", pointsBuffer);
        densityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        densityShader.SetFloat("radius", radius);
        densityShader.SetFloat("spacing", spacing);
        densityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));

        densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        return pointsBuffer;
    }
}
