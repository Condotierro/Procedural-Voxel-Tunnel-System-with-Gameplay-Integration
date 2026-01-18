using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 50f;
    public float lifetime = 5f;
    public float destroyRadius = 3f;
    public float hitForce = 5f;
    public int mode = 0;

    [Header("Explosion FX")]
    public GameObject explosionPrefab; 
    public AudioClip explosionSound;   
    public float explosionVolume = 0.8f;

    private Rigidbody rb;
    private AudioSource audioSource;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.velocity = transform.forward * speed;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.playOnAwake = false;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = collision.contacts[0].point;
        Quaternion hitRotation = Quaternion.LookRotation(collision.contacts[0].normal);

        if (collision.rigidbody != null)
            collision.rigidbody.AddForce(-collision.contacts[0].normal * hitForce, ForceMode.Impulse);

        ShipController sc = FindAnyObjectByType<ShipController>();

        Chunk chunk = collision.collider.GetComponentInParent<Chunk>();
        if (chunk != null)
        {
            switch (mode)
            {
                case 0:
                    sc.brokenBlocks++;
                    sc.brokenBlocks++;
                    DestroyVoxel(chunk, hitPoint); 
                break;
                case 1:
                    sc.brokenBlocks++;
                    sc.brokenBlocks++;
                    sc.brokenBlocks++;
                    DestroyVoxelWithOuterShell(chunk, hitPoint);
                    break;
                case 2:
                    sc.brokenBlocks += 20;
                    PaintVoxel(chunk, hitPoint);
                    break;
                default: break;
            }
        }
            

        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, hitPoint, hitRotation);
            Destroy(explosion, 1f);
        }

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, hitPoint, explosionVolume);
        }

        Destroy(gameObject);
    }

    void DestroyVoxel(Chunk chunk, Vector3 hitPoint)
    {
        Vector3 localPos = hitPoint - chunk.transform.position;
        int cx = Mathf.FloorToInt(localPos.x);
        int cy = Mathf.FloorToInt(localPos.y);
        int cz = Mathf.FloorToInt(localPos.z);

        int radius = Mathf.CeilToInt(destroyRadius);

        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                for (int z = -radius; z <= radius; z++)
                {
                    if (x * x + y * y + z * z <= destroyRadius * destroyRadius)
                    {
                        int bx = cx + x;
                        int by = cy + y;
                        int bz = cz + z;

                        if (bx < 0 || by < 0 || bz < 0 ||
                            bx >= Chunk.chunkSizeX || by >= Chunk.chunkSizeY || bz >= Chunk.chunkSizeZ)
                            continue;

                        chunk.blocks[bx, by, bz] = BlockType.Air;
                    }
                }

        chunk.GenerateMesh();
        chunk.UpdateCollider();
    }

    void PaintVoxel(Chunk chunk, Vector3 hitPoint)
    {
        Vector3 localPos = hitPoint - chunk.transform.position;
        int cx = Mathf.FloorToInt(localPos.x);
        int cy = Mathf.FloorToInt(localPos.y);
        int cz = Mathf.FloorToInt(localPos.z);

        int radius = Mathf.CeilToInt(destroyRadius);

        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                for (int z = -radius; z <= radius; z++)
                {
                    if (x * x + y * y + z * z <= destroyRadius * destroyRadius)
                    {
                        int bx = cx + x;
                        int by = cy + y;
                        int bz = cz + z;

                        if (bx < 0 || by < 0 || bz < 0 ||
                            bx >= Chunk.chunkSizeX || by >= Chunk.chunkSizeY || bz >= Chunk.chunkSizeZ)
                            continue;

                        chunk.blocks[bx, by, bz] = BlockType.Sand;
                    }
                }

        chunk.GenerateMesh();
        chunk.UpdateCollider();
    }


    void DestroyVoxelWithOuterShell(Chunk chunk, Vector3 hitPoint)
    {
        Vector3 localPos = hitPoint - chunk.transform.position;
        int cx = Mathf.FloorToInt(localPos.x);
        int cy = Mathf.FloorToInt(localPos.y);
        int cz = Mathf.FloorToInt(localPos.z);

        int radius = Mathf.CeilToInt(destroyRadius);

        int outerSqr = radius * radius;
        int inner = radius - 1;
        int innerSqr = inner * inner;

        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                for (int z = -radius; z <= radius; z++)
                {
                    int sqrDist = x * x + y * y + z * z;

                    // outside sphere
                    if (sqrDist > outerSqr)
                        continue;

                    int bx = cx + x;
                    int by = cy + y;
                    int bz = cz + z;

                    if (bx < 0 || by < 0 || bz < 0 ||
                        bx >= Chunk.chunkSizeX ||
                        by >= Chunk.chunkSizeY ||
                        bz >= Chunk.chunkSizeZ)
                        continue;

                    if (sqrDist <= innerSqr)
                    {
                        chunk.blocks[bx, by, bz] = BlockType.Air;
                    }
                    else if(chunk.blocks[bx, by, bz] != BlockType.Air)
                    {
                        chunk.blocks[bx, by, bz] = BlockType.Burned;
                    }
                }

        chunk.GenerateMesh();
        chunk.UpdateCollider();
    }
}
