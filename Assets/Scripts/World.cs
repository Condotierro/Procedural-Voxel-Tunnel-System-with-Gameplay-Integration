using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public int renderDistance = 4;
    public const int chunkSize = 16;
    public Material[] materials;

    public float scale;
    public Transform player;
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    void Start()
    {
        UpdateChunks();
    }

    void Update()
    {
        UpdateChunks();
    }

    void UpdateChunks()
    {
        int playerChunkX = Mathf.FloorToInt(player.position.x / Chunk.chunkSizeX);
        int playerChunkZ = Mathf.FloorToInt(player.position.z / Chunk.chunkSizeZ);

        HashSet<Vector2Int> needed = new HashSet<Vector2Int>();

        for (int dx = -renderDistance; dx <= renderDistance; dx++)
        {
            for (int dz = -renderDistance; dz <= renderDistance; dz++)
            {
                Vector2Int coord = new Vector2Int(playerChunkX + dx, playerChunkZ + dz);
                needed.Add(coord);

                if (!chunks.ContainsKey(coord))
                {
                    ChunkType type = DetermineChunkType(coord);
                    CreateChunk(coord.x, coord.y, type);
                }
            }
        }

        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var kvp in chunks)
        {
            if (!needed.Contains(kvp.Key))
            {
                Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var coord in toRemove)
        {
            chunks.Remove(coord);
        }
    }

    void CreateChunk(int cx, int cz, ChunkType type)
    {
        GameObject chunkObj = new GameObject($"Chunk_{cx}_{cz}_{type}");
        chunkObj.transform.parent = this.transform;

        Chunk chunk = chunkObj.AddComponent<Chunk>();
        chunk.chunkX = cx;
        chunk.chunkZ = cz;
        chunk.scale = scale;
        chunk.materials = materials;
        chunk.chunkType = type;

        chunkObj.transform.position = new Vector3(cx * Chunk.chunkSizeX, 0, cz * Chunk.chunkSizeZ);
        chunks[new Vector2Int(cx, cz)] = chunk;
    }

    ChunkType DetermineChunkType(Vector2Int coord)
    {
        if (coord == Vector2Int.zero)
            return ChunkType.Spawn;

        // Example logic — you can expand it later
        int rand = Random.Range(0, 100);

        if (rand < 70)
            return ChunkType.CorridorForward;
        else if (rand < 80)
            return ChunkType.CorridorLeft;
        else if (rand < 90)
            return ChunkType.CorridorRight;
        else if (rand < 95)
            return ChunkType.CorridorDiagonalLeft;
        else
            return ChunkType.CorridorDiagonalRight;
    }
}
