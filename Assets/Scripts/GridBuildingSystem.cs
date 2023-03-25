using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

public class GridBuildingSystem : MonoBehaviour
{
    public static GridBuildingSystem Instance;

    public GridLayout gridLayout;
    public Tilemap mainTilemap;
    public Tilemap tempTilemap;

    public static Dictionary<TileType, TileBase> tileBases = new Dictionary<TileType, TileBase>();

    private Building temp;
    private Vector3 prevPos;
    private BoundsInt prevArea;

    private static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap) {
      TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z];
      int i = 0;

      foreach (Vector3Int position in area.allPositionsWithin) {
        Vector3Int pos = new Vector3Int(position.x, position.y, 0);
        array[i] = tilemap.GetTile(position);
        i++;
      }
      
      return array;
    }

    private static void SetTilesBlock(BoundsInt area, TileType type, Tilemap tilemap) {
      int size = area.size.x * area.size.y * area.size.z;
      TileBase[] array = new TileBase[size];
      FillTiles(array, type);
      tilemap.SetTilesBlock(area, array);
    }

    private static void FillTiles(TileBase[] array, TileType type) {
      for (int i = 0; i < array.Length; i++) {
        array[i] = tileBases[type];
      }
    }

    // Start is called before the first frame update
    private void Start()
    {

        string tilePath = @"Tiles\";
        tileBases.Add(TileType.Empty, null);
        tileBases.Add(TileType.White, Resources.Load<TileBase>(tilePath + "white"));
        tileBases.Add(TileType.Green, Resources.Load<TileBase>(tilePath + "green"));
        tileBases.Add(TileType.Red, Resources.Load<TileBase>(tilePath + "red"));
    }

    // Update is called once per frame
    private void Update()
    {
        if (!temp) return;

        if (Input.GetMouseButtonDown(0)) {
          if (EventSystem.current.IsPointerOverGameObject(0)) {
            return;
          }

          if (!temp.Placed) {
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = gridLayout.LocalToCell(touchPos);

            if (prevPos != cellPos) {
              temp.transform.localPosition = gridLayout.CellToLocalInterpolated(cellPos + new Vector3(0.5f, 0.5f, 0f));
              prevPos = cellPos;
              FollowBuilding();
            }
          }
        } else if (Input.GetKeyDown(KeyCode.Space)) {
          if (!temp.Placed) {
            if (temp.CanBePlaced()) {
              temp.Place();
              SetTilesBlock(prevArea, TileType.White, mainTilemap);
            }
          }
        }
        else if (Input.GetKeyDown(KeyCode.Escape)) {
          ClearArea();
          Destroy(temp.gameObject);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void ClearArea()  {
      TileBase[] toClear = new TileBase[prevArea.size.x * prevArea.size.y * prevArea.size.z];
      FillTiles(toClear, TileType.Empty);
      tempTilemap.SetTilesBlock(prevArea, toClear);
    }

    private void FollowBuilding() {
      ClearArea();

      temp.area.position = gridLayout.WorldToCell(temp.transform.position);
      BoundsInt buildingArea = temp.area;

      TileBase[] baseArray = GetTilesBlock(buildingArea, mainTilemap);

      int size = baseArray.Length;
      TileBase[] tileArray = new TileBase[size];

      for (int i = 0; i < size; i++) {
        if (baseArray[i] == tileBases[TileType.White]) {
          tileArray[i] = tileBases[TileType.Green];
        } else {
          FillTiles(tileArray, TileType.Red);
          break;
        }
      }

      tempTilemap.SetTilesBlock(buildingArea, tileArray);
      prevArea = buildingArea;
    }

    public void InitializeWithBuild(GameObject building) {
      temp = Instantiate(building, Vector3.zero, Quaternion.identity).GetComponent<Building>();
      FollowBuilding();
    }

    public bool CanTakeArea(BoundsInt area) {
      TileBase[] tilesArray = GetTilesBlock(area, mainTilemap);
      foreach (TileBase tile in tilesArray) {
        if (tile != tileBases[TileType.White]) {
          Debug.Log("Cannot place here");
          return false;
        }
      }

      return true;
    }

    public void TakeArea(BoundsInt area) {
      SetTilesBlock(area, TileType.Empty, tempTilemap);
      SetTilesBlock(area, TileType.Green, mainTilemap);
    }

}

public enum TileType {
    Empty,
    White,
    Green,
    Red
}
