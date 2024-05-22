using System.Collections.Generic;
using UnityEngine;
using static MapGeneratorWithSpriteSpawn;

public class MapGeneratorWithSpriteSpawn : MonoBehaviour
{
    public float chunkSize = 16;
    public Transform player;
    public float noiseScale = 0.1f;
    public int seed = 0;
    public GameObject tilePrefabWithNoCollider;
    public CustomTile[] customTiles;
    public Transform noCollider;

    private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
    private int loadRadius = 2;
    private int unloadDistance = 3;
    [HideInInspector] public Sprite tileSprite;

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        if (seed == 0)
        {
            seed = Random.Range(0, int.MaxValue);
        }

        // Inicialize o gerador de números aleatórios com a semente **uma vez**
        Random.InitState(seed);

        Vector2Int playerChunkCoord = GetChunkCoordinates(player.position);
        LoadChunksAroundPlayer(playerChunkCoord);
    }

    void Update()
    {
        ManageChunks();
    }

    void GenerateTerrain(Chunk chunk)
    {
        Dictionary<Vector3Int, GameObject> structurePositions = new Dictionary<Vector3Int, GameObject>();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                float perlinNoise = Mathf.PerlinNoise((chunk.coordinates.x * chunkSize + x + seed) * noiseScale, (chunk.coordinates.y * chunkSize + y + seed) * noiseScale);
                tileSprite = GetTileSprite(perlinNoise);
                CustomTile selectedTile = SelectCustomTile(perlinNoise);

                // Corrija o cálculo da posição do tile
                Vector3Int tilePosition = new Vector3Int((int)(chunk.coordinates.x * chunkSize) + x, (int)(chunk.coordinates.y * chunkSize) + y, 0);

                // Verifique se já existe um prefab na posição
                if (!IsPrefabAtPosition(tilePosition))
                {
                    if (selectedTile.prefab != null)
                    {
                        // Se não houver prefab na posição, adicione a posição do prefab de estrutura ao dicionário
                        structurePositions.Add(tilePosition, selectedTile.prefab);
                    }
                    else
                    {
                        // Se não for um prefab de estrutura, crie um bloco normal
                        tileSprite = selectedTile.sprite[0];
                        CreateTile(tileSprite, tilePosition, chunk);
                    }
                }
            }
        }

        foreach (var entry in structurePositions)
        {
            Vector3Int position = entry.Key;
            GameObject prefab = entry.Value;

            Instantiate(prefab, position, Quaternion.identity, noCollider);
            prefab.SetActive(true);
        }
    }

    bool IsPrefabAtPosition(Vector3Int position)
    {
        // Defina o tamanho da caixa de verificação de colisão
        Vector2 boxSize = new Vector2(1f, 1f);

        // Defina a posição central da caixa de verificação de colisão
        Vector2 boxCenter = new Vector2(position.x, position.y);

        // Verifique se há colisores na área ao redor da posição
        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);

        // Retorne verdadeiro se houver colliders na área
        return colliders.Length > 0;
    }

    void CreateTile(Sprite tileSprite, Vector3Int position, Chunk chunk)
    {
        GameObject tileObject;
        if (chunk.tiles.ContainsKey(position))
        {
            tileObject = chunk.tiles[position];
            tileObject.SetActive(true);
        }
        else
        {
            tileObject = Instantiate(tilePrefabWithNoCollider, new Vector3(position.x, position.y, 0), Quaternion.identity);
            tileObject.transform.SetParent(noCollider);

            // Adicione um Collider2D se necessário
            if (tileSprite != null && tileSprite.name == "")
            {
                BoxCollider2D collider = tileObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1f, 1f);
            }

            chunk.tiles[position] = tileObject;
        }

        SpriteRenderer renderer = tileObject.GetComponent<SpriteRenderer>();
        renderer.sprite = tileSprite;
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
                if (loadedChunks.ContainsKey(chunkCoord))
                {
                    ActivateChunk(chunkCoord);
                }
                else
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
            tiles = new Dictionary<Vector3Int, GameObject>(),
        };
        GenerateTerrain(newChunk);
        loadedChunks[chunkCoord] = newChunk;
    }

    void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            foreach (var tile in chunk.tiles.Values)
            {
                tile.SetActive(false);
            }
        }
    }

    void ActivateChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            foreach (var tile in chunk.tiles.Values)
            {
                tile.SetActive(true);
            }
        }
    }

    Vector2Int GetChunkCoordinates(Vector3 position)
    {
        int chunkX = Mathf.FloorToInt(position.x / chunkSize);
        int chunkY = Mathf.FloorToInt(position.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    Sprite GetTileSprite(float perlinNoise)
    {
        CustomTile selectedTile = SelectCustomTile(perlinNoise);

        // Selecione um sprite aleatório do array de sprites do CustomTile
        if (selectedTile.sprite.Length > 0)
        {
            return selectedTile.sprite[0];
        }
        return null;
    }

    CustomTile SelectCustomTile(float perlinNoise)
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
        else if (perlinNoise < 0.6001f)
        {
            return customTiles[6];
        }
        else
        {
            return customTiles[7];
        }
    }


    public class Chunk
    {
        public Vector2Int coordinates;
        public Dictionary<Vector3Int, GameObject> tiles;
    }
}
