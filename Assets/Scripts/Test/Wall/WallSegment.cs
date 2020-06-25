using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class WallSegment : MonoBehaviour
{
    public const float WallDeltaUpdate = 0.1f;


    public MeshFilter WallMesh;
    public MeshFilter WallWalkway;

    public float WallLength;
    public float WallHeight;
    public Vector3 WallStart;


    private List<Vector3> WallBase = new List<Vector3>();

    private List<Vector3> WallMainVerticies = new List<Vector3>(200);
    private List<Vector2> WallMainUV = new List<Vector2>(200);
    private List<int> WallMainTris = new List<int>(200);

    private List<Vector3> WallWalkVerticies = new List<Vector3>(100);
    private List<Vector2> WallWalkUV = new List<Vector2>(200);
    private List<int> WallWalkTris = new List<int>(200);

    Vector3 wallHalfSize = new Vector3(0, 0, 0.5f);
    Vector3 wallSideSize = new Vector3(0, 0, 0.1f);
    float wallAboveWalk = 0.5f;

    // Update is called once per frame
    public void EditorUpdate()
    {
        //transform directly based on wall start
        transform.position = WallStart;
        //if we increas wall by enough, we decide number of points
   
        UpdateWall();

    }

    /// <summary>
    /// Updates the wall from the previous length <see cref="WallLength_"/> to the new
    /// length <see cref="WallLength"/>
    /// We defide the number of segments, as well as the size of each segment
    /// </summary>
    private void UpdateWall()
    {
        WallBase.Clear();

        Debug.Log("called");

        int wallSegments = (int)WallLength;
        float segmentSize = WallLength / wallSegments;
        WallMainMeshUpdate(wallSegments, segmentSize);
        WallWalkwayMeshUpdate(wallSegments, segmentSize);
       // WallMesh.sharedMesh.UploadMeshData(false);



    }
    private void WallWalkwayMeshUpdate(int wallSegments, float segmentSize)
    {
        WallWalkVerticies.Clear();
        WallWalkUV.Clear();
        WallWalkTris.Clear();

        for(int i=0; i<wallSegments; i++)
        {
            WallWalkVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + new Vector3(0, 1, 0) * (WallHeight - wallSideSize.z) + wallHalfSize * 0.95f);
            WallWalkVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + new Vector3(0, 1, 0) * (WallHeight - wallSideSize.z) - wallHalfSize * 0.95f);

        }
        for(int i=0; i<wallSegments-1; i++)
        {
            WallWalkTris.Add(i * 2 + 2);
            WallWalkTris.Add(i * 2 + 1);
            WallWalkTris.Add(i * 2 + 0);

            WallWalkTris.Add(i * 2 + 2);
            WallWalkTris.Add(i * 2 + 3);
            WallWalkTris.Add(i * 2 + 1);

        }
        if (WallWalkway.sharedMesh == null)
            WallWalkway.sharedMesh = new Mesh();
        WallWalkway.sharedMesh.Clear();
        WallWalkway.sharedMesh.vertices = WallWalkVerticies.ToArray();
        WallWalkway.sharedMesh.triangles = WallWalkTris.ToArray();

    }

    private void WallMainMeshUpdate(int wallSegments, float segmentSize)
    {
        WallMainVerticies.Clear();
        WallMainUV.Clear();
        WallMainTris.Clear();


        Vector2 wallStartUV = new Vector2(WallStart.x + WallStart.z, WallStart.y);
        //iterate first time for 'forward' wall
        for (int i = 0; i < wallSegments; i++)
        {
            WallMainVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + wallHalfSize);
            WallMainVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + new Vector3(0, 1, 0) * WallHeight + wallHalfSize);
            WallMainVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + new Vector3(0, 1, 0) * WallHeight + wallHalfSize - wallSideSize);
            WallMainVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + new Vector3(0, 1, 0) * WallHeight - new Vector3(0, wallAboveWalk, 0) + wallHalfSize - wallSideSize);

            WallMainUV.Add(wallStartUV + new Vector2(segmentSize * i, 0));
            WallMainUV.Add(wallStartUV + new Vector2(segmentSize * i, WallHeight));
            WallMainUV.Add(wallStartUV + new Vector2(segmentSize * i, WallHeight + wallSideSize.z));
            WallMainUV.Add(wallStartUV + new Vector2(segmentSize * i, WallHeight + wallSideSize.z + wallAboveWalk));


        }





        for (int i = 0; i < wallSegments; i++)
        {
            WallMainVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i - wallHalfSize);
            WallMainVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + new Vector3(0, 1, 0) * WallHeight - wallHalfSize);
            WallMainVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + new Vector3(0, 1, 0) * WallHeight - wallHalfSize + wallSideSize);
            WallMainVerticies.Add(WallStart + new Vector3(1, 0, 0) * segmentSize * i + new Vector3(0, 1, 0) * WallHeight - new Vector3(0, wallAboveWalk, 0) - wallHalfSize + wallSideSize);

            WallMainUV.Add(wallStartUV + new Vector2(segmentSize * i, 0));
            WallMainUV.Add(wallStartUV + new Vector2(segmentSize * i, WallHeight));
            WallMainUV.Add(wallStartUV + new Vector2(segmentSize * i, WallHeight + wallSideSize.z));
            WallMainUV.Add(wallStartUV + new Vector2(segmentSize * i, WallHeight + wallSideSize.z + wallAboveWalk));


        }



        int wallSecondStartIndex = 0;
        for (int i = 0; i < wallSegments - 1; i++)
        {
            WallMainTris.Add(i * 4 + 4);
            WallMainTris.Add(i * 4 + 1);
            WallMainTris.Add(i * 4 + 0);

            WallMainTris.Add(i * 4 + 4);
            WallMainTris.Add(i * 4 + 5);
            WallMainTris.Add(i * 4 + 1);

            WallMainTris.Add(i * 4 + 5);
            WallMainTris.Add(i * 4 + 2);
            WallMainTris.Add(i * 4 + 1);

            WallMainTris.Add(i * 4 + 5);
            WallMainTris.Add(i * 4 + 6);
            WallMainTris.Add(i * 4 + 2);

            WallMainTris.Add(i * 4 + 6);
            WallMainTris.Add(i * 4 + 3);
            WallMainTris.Add(i * 4 + 2);

            WallMainTris.Add(i * 4 + 3);
            WallMainTris.Add(i * 4 + 6);
            WallMainTris.Add(i * 4 + 7);

            /*
            WallMainTris.Add(i * 4 + 4);
            WallMainTris.Add(i * 4 + 5);
            WallMainTris.Add(i * 4 + 1);
            */


            wallSecondStartIndex = i * 4 + 8;
        }

        for (int i = 0; i < wallSegments - 1; i++)
        {
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 0);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 1);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 4);

            WallMainTris.Add(wallSecondStartIndex + i * 4 + 1);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 5);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 4);

            WallMainTris.Add(wallSecondStartIndex + i * 4 + 1);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 2);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 5);

            WallMainTris.Add(wallSecondStartIndex + i * 4 + 2);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 6);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 5);

            WallMainTris.Add(wallSecondStartIndex + i * 4 + 2);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 3);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 6);

            WallMainTris.Add(wallSecondStartIndex + i * 4 + 7);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 6);
            WallMainTris.Add(wallSecondStartIndex + i * 4 + 3);
        }
        WallMesh.sharedMesh.Clear();
        WallMesh.sharedMesh.vertices = WallMainVerticies.ToArray();
        WallMesh.sharedMesh.triangles = WallMainTris.ToArray();
        WallMesh.sharedMesh.uv = WallMainUV.ToArray();
        WallMesh.sharedMesh.RecalculateNormals();
    }


    private void OnDrawGizmos()
    {

        EditorUpdate();

        if (WallMainVerticies.Count == 0)
            return;
        if(WallMainVerticies.Count == 1)
        {
            Gizmos.DrawSphere(WallMainVerticies[0], 0.2f);
            return;
        }
        for(int i=0; i< WallMainVerticies.Count-1; i++)
        {
            Gizmos.DrawSphere(WallMainVerticies[i], 0.2f);
            Gizmos.DrawLine(WallMainVerticies[i], WallMainVerticies[i + 1]);
        }
        Gizmos.DrawSphere(WallMainVerticies[WallMainVerticies.Count - 1], 0.2f);
    }
}
