using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Chunk : MonoBehaviour
{
    public const int chunkSizeX = 16;
    public const int chunkSizeY = 128;
    public const int chunkSizeZ = 16;
    public int maxHeight = 64;
    public BlockType[,,] blocks;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    public float scale;
    public int chunkX;
    public int chunkZ;

    public Material[] materials;

    public ChunkType chunkType;

    void Start()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshRenderer.materials = materials;

        GenerateBlocks();
        GenerateMesh();
        AddCollider();
    }

    void AddCollider()
    {
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;
    }
    public void UpdateCollider()
    {
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = null; // clear previous mesh
        meshCollider.sharedMesh = meshFilter.mesh; // assign updated mesh
    }

    public void ModifyBlock(int x, int y, int z, BlockType newType)
    {
        blocks[x, y, z] = newType;
        GenerateMesh();   // rebuild the mesh
        UpdateCollider(); // sync collider
    }

    void GenerateBlocks()
    {
        blocks = new BlockType[chunkSizeX, chunkSizeY, chunkSizeZ];

        for (int x = 0; x < chunkSizeX; x++)
        {
            for (int z = 0; z < chunkSizeZ; z++)
            {
                float wx = (x + chunkX * chunkSizeX);
                float wz = (z + chunkZ * chunkSizeZ);

                float heightNoise = Mathf.PerlinNoise(wx * scale, wz * scale);
                int height = Mathf.FloorToInt(heightNoise * maxHeight);

                // fill column
                for (int y = 0; y < chunkSizeY; y++)
                {
                    if (y < height - 10)
                        blocks[x, y, z] = BlockType.Stone;
                    else if (y < height)
                        blocks[x, y, z] = BlockType.Dirt;
                    else if (y == height)
                        blocks[x, y, z] = BlockType.Grass;
                    else
                        blocks[x, y, z] = BlockType.Air;
                }
            }
        }

        // Carve based on type
        switch (chunkType)
        {
            case ChunkType.Spawn:
                CarveCircle(chunkSizeX / 2, 50, chunkSizeZ / 2, 6);
                break;

            case ChunkType.CorridorForward:
                CarveCorridor(8, 50, 8, Vector3.forward);
                break;

            case ChunkType.CorridorLeft:
                CarveCorridor(8, 50, 8, Vector3.left);
                break;

            case ChunkType.CorridorRight:
                CarveCorridor(8, 50, 8, Vector3.right);
                break;

            case ChunkType.CorridorDiagonalLeft:
                CarveDiagonal(8, 50, 8, -1);
                break;

            case ChunkType.CorridorDiagonalRight:
                CarveDiagonal(8, 50, 8, 1);
                break;
        }
    }



    public void GenerateMesh()
    {
        Mesh mesh = new Mesh();

        // shared across all block faces
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();

        // one triangle list per material
        var submeshTris = new List<List<int>>();
        for (int i = 0; i < materials.Length; i++)
            submeshTris.Add(new List<int>());

        // loop through all blocks in the chunk
        for (int x = 0; x < chunkSizeX; x++)
        {
            for (int y = 0; y < chunkSizeY; y++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    if (blocks[x, y, z] == BlockType.Air) continue;

                    Vector3 pos = new Vector3(x, y, z);

                    // generate only visible faces
                    AddCube(vertices, uvs, submeshTris, pos, x, y, z);
                }
            }
        }

        // build the final mesh
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.subMeshCount = materials.Length;

        // assign triangles per material index
        for (int i = 0; i < materials.Length; i++)
            mesh.SetTriangles(submeshTris[i], i);

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }


    void AddCube(List<Vector3> verts,
             List<Vector2> uvs,
             List<List<int>> submeshTris,
             Vector3 pos, int x, int y, int z)
    {

        BlockType block = blocks[x, y, z];
        int matIndex = GetMaterialIndex(block);
        List<int> targetTris = submeshTris[matIndex];

        // TOP
        if (y + 1 >= chunkSizeY || blocks[x, y + 1, z] == BlockType.Air)
        {
            Vector3[] faceVerts = new Vector3[] {
            pos + new Vector3(0,1,0),
            pos + new Vector3(1,1,0),
            pos + new Vector3(1,1,1),
            pos + new Vector3(0,1,1)
        };
            int startIndex = verts.Count;
            verts.AddRange(faceVerts);

            targetTris.AddRange(new int[] {
            startIndex, startIndex + 2, startIndex + 1,
            startIndex, startIndex + 3, startIndex + 2
        });

            uvs.AddRange(new Vector2[] {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1),
            new Vector2(0,1)
        });
        }

        // BOTTOM
        if (y - 1 < 0 || blocks[x, y - 1, z] == BlockType.Air)
        {
            Vector3[] faceVerts = new Vector3[] {
                pos + new Vector3(0,0,0),
                pos + new Vector3(1,0,0),
                pos + new Vector3(1,0,1),
                pos + new Vector3(0,0,1)
            };
            int startIndex = verts.Count;
            verts.AddRange(faceVerts);
            targetTris.AddRange(new int[] { startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3 });

            uvs.AddRange(new Vector2[] {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1)
            });
        }

        //RIGHT
        if (x + 1 >= chunkSizeX || blocks[x+1, y, z] == BlockType.Air)
        {
            Vector3[] faceVerts = new Vector3[] {
                pos + new Vector3(1,0,0),
                pos + new Vector3(1,1,0),
                pos + new Vector3(1,1,1),
                pos + new Vector3(1,0,1)
            };
            int startIndex = verts.Count;
            verts.AddRange(faceVerts);
            targetTris.AddRange(new int[] { startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3 });

            uvs.AddRange(new Vector2[] {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1)
            });
        }

        //LEFT
        if (x - 1 < 0 || blocks[x - 1, y, z] == BlockType.Air)
        {
            Vector3[] faceVerts = new Vector3[] {
                pos + new Vector3(0,0,0),
                pos + new Vector3(0,1,0),
                pos + new Vector3(0,1,1),
                pos + new Vector3(0,0,1)
            };
            int startIndex = verts.Count;
            verts.AddRange(faceVerts);
            targetTris.AddRange(new int[] { startIndex, startIndex + 2, startIndex + 1, startIndex, startIndex + 3, startIndex + 2 });

            uvs.AddRange(new Vector2[] {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1)
            });
        }

        //FRONT
        if (z + 1 >= chunkSizeZ || blocks[x, y, z + 1] == BlockType.Air)
        {
            Vector3[] faceVerts = new Vector3[] {
                pos + new Vector3(0,0,1),
                pos + new Vector3(0,1,1),
                pos + new Vector3(1,1,1),
                pos + new Vector3(1,0,1)
            };
            int startIndex = verts.Count;
            verts.AddRange(faceVerts);
            targetTris.AddRange(new int[] { startIndex, startIndex + 2, startIndex + 1, startIndex, startIndex + 3, startIndex + 2 });

            uvs.AddRange(new Vector2[] {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1)
            });
        }

        //BACK
        if (z - 1 < 0 || blocks[x, y, z - 1] == BlockType.Air)
        {
            Vector3[] faceVerts = new Vector3[] {
                pos + new Vector3(0,0,0),
                pos + new Vector3(0,1,0),
                pos + new Vector3(1,1,0),
                pos + new Vector3(1,0,0)
            };
            int startIndex = verts.Count;
            verts.AddRange(faceVerts);
            targetTris.AddRange(new int[] { startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3 });

            uvs.AddRange(new Vector2[] {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1)
            });
        }
    }

    int GetMaterialIndex(BlockType type)
    {
        switch (type)
        {
            case BlockType.Dirt: return 0;
            case BlockType.Grass: return 1;
            case BlockType.Stone: return 2;
            default: return 0; // fallback
        }
    }

    void CarveCircle(int cx, int cy, int cz, int radius)
    {
        for (int x = 0; x < chunkSizeX; x++)
            for (int y = 0; y < chunkSizeY; y++)
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    float dist = Vector3.Distance(new Vector3(x, y, z), new Vector3(cx, cy, cz));
                    if (dist < radius)
                        blocks[x, y, z] = BlockType.Air;
                }
    }

    void CarveCorridor(int centerY, int height, int width, Vector3 dir)
    {
        for (int x = 0; x < chunkSizeX; x++)
            for (int z = 0; z < chunkSizeZ; z++)
            {
                int cx = chunkSizeX / 2;
                int cz = chunkSizeZ / 2;

                // simple straight corridor down z-axis
                if (Mathf.Abs(x - cx) < width / 2)
                {
                    for (int y = centerY; y < centerY + height; y++)
                        blocks[x, y, z] = BlockType.Air;
                }
            }
    }

    void CarveDiagonal(int centerY, int height, int width, int dir)
    {
        for (int x = 0; x < chunkSizeX; x++)
            for (int z = 0; z < chunkSizeZ; z++)
            {
                // line goes diagonally across chunk
                int diag = (int)(z + dir * (x - chunkSizeX / 2));
                if (Mathf.Abs(diag) < width / 2)
                {
                    for (int y = centerY; y < centerY + height; y++)
                        blocks[x, y, z] = BlockType.Air;
                }
            }
    }

}
