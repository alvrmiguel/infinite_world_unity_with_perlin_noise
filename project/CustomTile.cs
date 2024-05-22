using UnityEngine;
using UnityEngine.Tilemaps;

public enum ConnectionType
{
    None, 
    Left,
    Right,  
    LeftRight,    
    TopBottom,    
    AllSides,
    LeftRightTop, 
    LeftRightBottom, 
    TopBottomLeft,  
    TopBottomRight,    
}

[CreateAssetMenu(fileName = "CustomTile", menuName = "Tiles/CustomTile")]
public class CustomTile : TileBase
{
    public TileBase[] tiles; // Tiles que compõem a área do objeto
    public Vector2Int size; // Dimensões da área (por exemplo, 5x6)
    public bool hasCollider;
    public int sortingOrder;
    public bool isAbovePlayer;
    public bool isInteractable;
    public GameObject associatedPrefab; // Prefab associado, como uma árvore
    public Vector2Int adjacencyOffsets; // Offsets para tiles adjacentes
    public ConnectionType connectionType; // Tipo de conexão com os vizinhos
    public Texture2D spriteSheet;
    public int spriteWidth;
    public int spriteHeight;
    public int rows;
    public int cols;
    public Sprite[] sprite;

    // Override necessário para desenhar o tile na tilemap
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        if (tiles != null && tiles.Length > 0)
        {
            tileData.sprite = ((Tile)tiles[0]).sprite; // Usando o primeiro tile como base
            tileData.colliderType = hasCollider ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            tileData.flags = TileFlags.LockColor;
        }
    }
}
