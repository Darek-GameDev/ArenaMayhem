/*

  _____ _ _    __        __         _     _  ____                _             
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __ 
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |   
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|   
                                                                               
	TileWorldCreator (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/

using System.Collections.Generic;
using UnityEngine;

namespace GiantGrey.TileWorldCreator.Utilities
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public static class GridMeshGenerator
    {

        public static Mesh GenerateMesh(HashSet<Vector2> cellPositions, HashSet<Vector2> allCells, float cellSize, float height, float extrusionHeight, bool invertWalls)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            float insetAmount = 0.0f; // Experimental

            // For our base quad the vertices are:
            // 0: bottom-left, 1: bottom-right, 2: top-right, 3: top-left

            // Mapping for wall edges corresponding to a missing neighbor
            // Order: left, right, up, down
            // For each, tuple (v0, v1) from the base quad to extrude along.
            int[,] edgeIndices = new int[4, 2] {
                { 0, 3 }, // Left edge: bottom-left → top-left
                { 2, 1 }, // Right edge: top-right → bottom-right (reversed order)
                { 3, 2 }, // Up edge: top-left → top-right
                { 1, 0 }  // Down edge: bottom-right → bottom-left (reversed order)
            };

            // Directions to check (should correspond to the above order)
            Vector2[] directions = new Vector2[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down };


            foreach (Vector2 cell in cellPositions)
            {
                Vector3 basePos = new Vector3(cell.x * cellSize - cellSize * 0.5f, height, cell.y * cellSize - cellSize * 0.5f);
                // Vector3 basePos = new Vector3(cell.x * cellSize, height, cell.y * cellSize);
                //         Vector3 basePos = new Vector3(cell.x, 0, cell.y) - parentOffset;
                // basePos *= cellSize;
                // basePos.y = height;

                // Create the base quad for the cell (floor)
                int baseIndex = vertices.Count;
                vertices.Add(basePos);                                      // 0: bottom-left
                vertices.Add(basePos + new Vector3(cellSize, 0, 0));         // 1: bottom-right
                vertices.Add(basePos + new Vector3(cellSize, 0, cellSize));  // 2: top-right
                vertices.Add(basePos + new Vector3(0, 0, cellSize));          // 3: top-left
        
                // Create floor triangles (two triangles forming the quad)
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);

                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 2);

            
                // Wall triangles
                // for (int d = 0; d < 4; d++)
                // {
                //     Vector2 neighborPos = cell + directions[d];

                //     // Use the full map to decide if a wall is needed
                //     if (!allCells.Contains(neighborPos))
                //     {
                //         int idxA = baseIndex + edgeIndices[d, 0];
                //         int idxB = baseIndex + edgeIndices[d, 1];
                //         Vector3 v0 = vertices[idxA];
                //         Vector3 v1 = vertices[idxB];

                //         int wallBaseIndex = vertices.Count;
                //         vertices.Add(v0);
                //         vertices.Add(v1);
                //         vertices.Add(v1 + Vector3.up * extrusionHeight);
                //         vertices.Add(v0 + Vector3.up * extrusionHeight);

                //         if (!invertWalls)
                //         {
                //             triangles.Add(wallBaseIndex + 0);
                //             triangles.Add(wallBaseIndex + 1);
                //             triangles.Add(wallBaseIndex + 2);

                //             triangles.Add(wallBaseIndex + 0);
                //             triangles.Add(wallBaseIndex + 2);
                //             triangles.Add(wallBaseIndex + 3);
                //         }
                //         else
                //         {
                //             // Reverse winding to flip normals
                //             triangles.Add(wallBaseIndex + 0);
                //             triangles.Add(wallBaseIndex + 2);
                //             triangles.Add(wallBaseIndex + 1);

                //             triangles.Add(wallBaseIndex + 0);
                //             triangles.Add(wallBaseIndex + 3);
                //             triangles.Add(wallBaseIndex + 2);
                //         }
                //     }
                // }

                for (int d = 0; d < 4; d++)
                {
                    Vector2 neighborPos = cell + directions[d];
                    if (!allCells.Contains(neighborPos))
                    {
                        int idxA = baseIndex + edgeIndices[d, 0];
                        int idxB = baseIndex + edgeIndices[d, 1];
                        Vector3 v0 = vertices[idxA];
                        Vector3 v1 = vertices[idxB];

                        // Compute outward normal in XZ plane
                        Vector3 edgeDir = (v1 - v0).normalized;
                        Vector3 outward = new Vector3(-edgeDir.z, 0, edgeDir.x);

                        // Offset both bottom and top
                        Vector3 v0Offset = v0 + outward * insetAmount;
                        Vector3 v1Offset = v1 + outward * insetAmount;

                        int wallBaseIndex = vertices.Count;

                        // Wall vertices (offset)
                        vertices.Add(v0Offset);                         // 0 bottom offset
                        vertices.Add(v1Offset);                         // 1 bottom offset
                        vertices.Add(v1Offset + Vector3.up * extrusionHeight); // 2 top offset
                        vertices.Add(v0Offset + Vector3.up * extrusionHeight); // 3 top offset

                        // Wall face (vertical, offset outward)
                        if (!invertWalls)
                        {
                            triangles.Add(wallBaseIndex + 0);
                            triangles.Add(wallBaseIndex + 1);
                            triangles.Add(wallBaseIndex + 2);

                            triangles.Add(wallBaseIndex + 0);
                            triangles.Add(wallBaseIndex + 2);
                            triangles.Add(wallBaseIndex + 3);
                        }
                        else
                        {
                            triangles.Add(wallBaseIndex + 0);
                            triangles.Add(wallBaseIndex + 2);
                            triangles.Add(wallBaseIndex + 1);

                            triangles.Add(wallBaseIndex + 0);
                            triangles.Add(wallBaseIndex + 3);
                            triangles.Add(wallBaseIndex + 2);
                        }

                        // ----- Extra connector strip between ground and offset bottom -----
                        int connectorBase = vertices.Count;

                        vertices.Add(v0);        // 0 original ground
                        vertices.Add(v1);        // 1 original ground
                        vertices.Add(v1Offset);  // 2 offset bottom
                        vertices.Add(v0Offset);  // 3 offset bottom

                        // Triangles for connector
                        triangles.Add(connectorBase + 0);
                        triangles.Add(connectorBase + 1);
                        triangles.Add(connectorBase + 2);

                        triangles.Add(connectorBase + 0);
                        triangles.Add(connectorBase + 2);
                        triangles.Add(connectorBase + 3);
                    }

                }
            }

        
            // Assign data to the mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }


        /// <summary>
        /// A simple struct representing an edge (line segment) between two 2D points.
        /// Two edges are considered equal if they have the same endpoints (order independent).
        /// </summary>
        struct Edge
        {
            public Vector2 a, b;
            public Edge(Vector2 a, Vector2 b)
            {
                this.a = a;
                this.b = b;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Edge))
                    return false;
                Edge other = (Edge)obj;
                return (Vector2.Distance(a, other.a) < 0.001f && Vector2.Distance(b, other.b) < 0.001f) ||
                    (Vector2.Distance(a, other.b) < 0.001f && Vector2.Distance(b, other.a) < 0.001f);
            }

            public override int GetHashCode()
            {
                // Order-independent hash code.
                int hash1 = a.GetHashCode() ^ b.GetHashCode();
                int hash2 = b.GetHashCode() ^ a.GetHashCode();
                return hash1 ^ hash2;
            }
        }
    }

    /// <summary>
    /// A simple ear-clipping triangulator for 2D polygons.
    /// </summary>
    public class Triangulator
    {
        private List<Vector2> m_points = new List<Vector2>();

        public Triangulator(Vector2[] points)
        {
            m_points = new List<Vector2>(points);
        }

        public int[] Triangulate()
        {
            List<int> indices = new List<int>();

            int n = m_points.Count;
            if (n < 3)
                return indices.ToArray();

            int[] V = new int[n];
            if (Area() > 0)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                    return indices.ToArray();

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a = V[u], b = V[v], c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    for (int s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            int n = m_points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = m_points[p];
                Vector2 qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return A * 0.5f;
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector2 A = m_points[V[u]];
            Vector2 B = m_points[V[v]];
            Vector2 C = m_points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = m_points[V[p]];
                if (InsideTriangle(A, B, C, P))
                    return false;
            }
            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }
}