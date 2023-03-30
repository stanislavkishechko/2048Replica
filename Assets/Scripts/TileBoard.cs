using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBoard : MonoBehaviour
{


    [SerializeField] private Tile tilePrefab;
	[SerializeField] TileStateSO[] tileStateArray;
	[SerializeField] private GameManager gameManager;

    private TileGrid grid; 
    private List<Tile> tiles;
	private bool waiting;


	private void Awake()
	{
		grid = GetComponentInChildren<TileGrid>();

		int maxCapacity = 16;
		tiles = new List<Tile>(maxCapacity);
	}	

	private void Update()
	{
		if (!waiting)
		{
			if (Input.GetKeyDown(KeyCode.W))
			{
				MoveTiles(Vector2Int.up, 0, 1, 1, 1);
			}
			if (Input.GetKeyDown(KeyCode.S))
			{
				MoveTiles(Vector2Int.down, 0, 1, grid.height - 2, -1);
			}
			if (Input.GetKeyDown(KeyCode.A))
			{
				MoveTiles(Vector2Int.left, 1, 1, 0, 1);
			}
			if (Input.GetKeyDown(KeyCode.D))
			{
				MoveTiles(Vector2Int.right, grid.width - 2, -1, 0, 1);
			}
		}
	}

	public void ClearBoard()
	{
		foreach (TileCell cell in grid.cells)
		{
			cell.tile = null;
		}

		foreach (Tile tile in tiles)
		{
			Destroy(tile.gameObject);
		}

		tiles.Clear();
	}

	public void CreateTile()
	{				
		Tile tile = Instantiate(tilePrefab, grid.transform);
		
		tile.SetState(tileStateArray[0], 2);
		tile.Spawn(grid.GetRandomEmptyCell());
		tiles.Add(tile);
	}

	private void MoveTiles(Vector2Int direction, int startX, int incrementX, int startY, int incrementY)
	{
		bool changed = false;

		for (int x = startX; x >= 0 && x < grid.width; x += incrementX)
		{
			for (int y = startY; y >= 0 && y < grid.height; y += incrementY)
			{
				TileCell cell = grid.GetCell(x, y);

				if (cell.occupied)
				{
					changed |= MoveTile(cell.tile, direction);
				}
			}
		}

		if (changed)
		{
			StartCoroutine(WaitForChanges());
		}
	}

	private bool CanMerge(Tile firstTile, Tile secondTile)
	{
		return firstTile.number == secondTile.number && !secondTile.locked;
	}

	private void Merge(Tile firstTile, Tile secondTile)
	{
		tiles.Remove(firstTile);
		firstTile.Merge(secondTile.cell);

		int index = Mathf.Clamp(IndexOf(secondTile.state) + 1, 0, tileStateArray.Length - 1);
		int number = secondTile.number * 2;

		secondTile.SetState(tileStateArray[index], number);

		gameManager.IncreaseScore(number);
	}

	private int IndexOf(TileStateSO stateSO)
	{
		for (int i = 0; i < tileStateArray.Length; i++)
		{
			if (stateSO == tileStateArray[i])
			{
				return i;
			}
		}

		return -1;
	}

	private bool MoveTile(Tile tile, Vector2Int direction)
	{
		TileCell newCell = null;
		TileCell adjacent = grid.GetAdjacentCell(tile.cell, direction);

		while (adjacent != null)
		{
			if (adjacent.occupied)
			{
				if (CanMerge(tile, adjacent.tile))
				{
					Merge(tile, adjacent.tile);
					return true;
				}

				break;
			}

			newCell = adjacent;
			adjacent = grid.GetAdjacentCell(adjacent, direction);
		}

		if (newCell != null)
		{
			tile.MoveTo(newCell);
			return true;
		}

		return false;
	}

	private IEnumerator WaitForChanges()
	{
		waiting = true;

		float waitingTime = .1f;
		yield return new WaitForSeconds(waitingTime);

		waiting = false;

		foreach (var tile in tiles)
		{
			tile.locked = false;
		}

		if (tiles.Count != grid.size)
		{
			CreateTile();
		}

		if (CheckForGameOver())
		{
			gameManager.GameOver();
		}
	}

	private bool CheckForGameOver()
	{
		if (tiles.Count != grid.size)
		{
			return false;
		}

		foreach (Tile tile in tiles)
		{
			TileCell up = grid.GetAdjacentCell(tile.cell, Vector2Int.up);
			TileCell down = grid.GetAdjacentCell(tile.cell, Vector2Int.down);
			TileCell left = grid.GetAdjacentCell(tile.cell, Vector2Int.left);
			TileCell right = grid.GetAdjacentCell(tile.cell, Vector2Int.right);

			if (up != null && 
				down != null && 
				left != null && 
				right != null && CanMerge(tile, up.tile))
			{
				return false;
			}
		}

		return true;
	}
}
