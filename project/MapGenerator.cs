using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class Chunk
{
    public Vector2Int coordinates;
    public Tilemap tilemap;
    public List<GameObject> structures;
    public List<GameObject> zombies;
}

public class MapGenerator : MonoBehaviour
{
    public float chunkSize = 16;
    public CustomTile[] customTiles;
    public Tilemap tilemap;
    public Transform player;
    public float noiseScale = 0.1f;
    public int seed = 0;
    public GameObject[] structurePrefabs;
    public float treeMinProbability = 0.2f;
    public float treeMaxProbability = 0.5f;

    private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
    private int loadRadius = 2;
    private int unloadDistance = 3;

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        if (seed == 0)
        {
            seed = UnityEngine.Random.Range(0, int.MaxValue);
        }

        // Inicialize o gerador de números aleatórios com a semente **uma vez**
        UnityEngine.Random.InitState(seed);

        Vector2Int playerChunkCoord = GetChunkCoordinates(player.position);
        LoadChunksAroundPlayer(playerChunkCoord);
    }

    void Update()
    {
        ManageChunks();
    }

    void GenerateTerrain(Chunk chunk)
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        List<TileBase> tiles = new List<TileBase>();

        // HashSet para rastrear posições de troncos de árvores
        HashSet<Vector3Int> treePositions = new HashSet<Vector3Int>();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                float perlinNoise = Mathf.PerlinNoise((chunk.coordinates.x * chunkSize + x + seed) * noiseScale, (chunk.coordinates.y * chunkSize + y + seed) * noiseScale);
                CustomTile tile = CreateTiles(perlinNoise);
                Vector3Int tilePosition = new Vector3Int(chunk.coordinates.x * (int)chunkSize + x, chunk.coordinates.y * (int)chunkSize + y, 0);

                // Verifique se o tile é um tronco de árvore e se pode ser colocado
                if (tile == customTiles[1]) // Supondo que customTiles[1] é o tronco da árvore
                {
                    Vector2Int adjacencyOffsets = tile.adjacencyOffsets;
                    int xOffset = adjacencyOffsets.x;
                    int yOffset = adjacencyOffsets.y;

                    bool canPlaceTree = true;

                    // Verifique a distância mínima em relação aos troncos de árvores existentes
                    foreach (var treePos in treePositions)
                    {
                        if (Mathf.Abs(tilePosition.x - treePos.x) < xOffset || Mathf.Abs(tilePosition.y - treePos.y) < yOffset)
                        {
                            canPlaceTree = false;
                            break;
                        }
                    }

                    if (canPlaceTree)
                    {
                        positions.Add(tilePosition);
                        tiles.Add(tile);
                        treePositions.Add(tilePosition); // Adicione a posição do tronco ao HashSet
                        PlaceFoliageAroundTree(tilePosition, treePositions, positions, tiles);
                    }
                    else
                    {
                        // Coloque outro tipo de tile em vez do tronco da árvore
                        tile = customTiles[0]; // Supondo que customTiles[0] é um tile padrão
                        positions.Add(tilePosition);
                        tiles.Add(tile);
                    }
                }
            }
        }

        tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
    }

    void PlaceFoliageAroundTree(Vector3Int treePosition, HashSet<Vector3Int> treePositions, List<Vector3Int> positions, List<TileBase> tiles)
    {
        // Define os offsets para as posições adjacentes
        Vector2Int[] offsets = {
        new Vector2Int(-1, 0), // Esquerda
        new Vector2Int(1, 0),  // Direita
        new Vector2Int(0, -1), // Baixo
        new Vector2Int(0, 1)   // Cima
    };

        // Verifica cada offset
        foreach (var offset in offsets)
        {
            Vector3Int adjacentPosition = treePosition + new Vector3Int(offset.x, offset.y, 0);
            if (!treePositions.Contains(adjacentPosition))
            {
                // Se a posição adjacente estiver vazia, coloque a folhagem lá
                positions.Add(adjacentPosition);
                tiles.Add(customTiles[8]); // Supondo que customTiles[8] é o tile de folhagem
            }
        }
    }


    void ManageChunks()
    {
        Vector2Int playerChunkCoord = GetChunkCoordinates(player.position);
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();

        foreach (var coord in loadedChunks.Keys)
        {
            if (Vector2Int.Distance(coord, playerChunkCoord) > unloadDistance)
            {
                chunksToUnload.Add(coord);
            }
        }

        // Imprime as coordenadas dos chunks que serão descarregados
        foreach (var coord in chunksToUnload)
        {
            UnloadChunk(coord);
        }

        LoadChunksAroundPlayer(playerChunkCoord);
    }

    void LoadChunksAroundPlayer(Vector2Int playerChunkCoord)
    {
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int y = -loadRadius; y <= loadRadius; y++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunkCoord.x + x, playerChunkCoord.y + y);
                if (!loadedChunks.ContainsKey(chunkCoord))
                {
                    LoadChunk(chunkCoord);
                }
            }
        }
    }

    void LoadChunk(Vector2Int chunkCoord)
    {
        Chunk newChunk = new Chunk
        {
            coordinates = chunkCoord,
            tilemap = tilemap,
            structures = new List<GameObject>(),
            zombies = new List<GameObject>()
        };
        GenerateTerrain(newChunk);
        loadedChunks[chunkCoord] = newChunk;
    }

    void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            loadedChunks.Remove(chunkCoord);

            // Obtenha os limites do chunk
            BoundsInt bounds = new BoundsInt(chunkCoord.x * (int)chunkSize, chunkCoord.y * (int)chunkSize, 0, (int)chunkSize, (int)chunkSize, 1);

            // Remova os tiles dentro dos limites do chunk
            foreach (Vector3Int position in bounds.allPositionsWithin)
            {
                chunk.tilemap.SetTile(position, null);
            }
        }
    }

    Vector2Int GetChunkCoordinates(Vector3 position)
    {
        int chunkX = Mathf.FloorToInt(position.x / chunkSize);
        int chunkY = Mathf.FloorToInt(position.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    CustomTile CreateTiles(float perlinNoise)
    {
        if (perlinNoise < 0.1f)
        {
            return customTiles[0];
        }
        else if (perlinNoise < 0.2f)
        {
            return customTiles[1];
        }
        else if (perlinNoise < 0.25f)
        {
            //TREE TRUNK
            return customTiles[2];
        }
        else if (perlinNoise < 0.40f)
        {
            return customTiles[3];
        }
        else if (perlinNoise < 0.5f)
        {
            return customTiles[4];
        }
        else if (perlinNoise < 0.6f)
        {
            return customTiles[5];
        }
        else if (perlinNoise < 0.7f)
        {
            return customTiles[6];
        }
        else if (perlinNoise < 0.8f)
        {
            return customTiles[7];
        }
        else if (perlinNoise < 0.9f)
        {
            return customTiles[8];
        }
        else
        {
            return customTiles[9];
        }
    }
}
