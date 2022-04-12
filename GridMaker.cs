using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CoordinateSystem;

public class GridMaker : MonoBehaviour
{
    [Header("Scripts")]
    [SerializeField]
    GameManager gManager;

    [SerializeField]
    private GameObject tilePrefab;
    public int gridSize;
    public float perEnemies;
    private GameObject[,] grid;
    public float branchOutPercentage;
    public float branchIterationModifer;

    List<Coordinate> road;
    [SerializeField]
    List<Coordinate> posDirections;
    Coordinate startPoint;
    List<Coordinate> availableCoordinates;
    List<Coordinate> forestTiles;
    List<Coordinate> enemyTiles;
   
    [SerializeField]
    Image blackScreen;

    public enum DirectionState {Up,Down,Left,Right,Default};
   
    public void InitializeScript(int gridS, float pEnemies)
    {
        gridSize = gridS;
        perEnemies = pEnemies;
       
    }
    // Start is called before the first frame update
    public IEnumerator Start()
    {
        gridSize = Difficulty.Instance.gridSize;
        perEnemies = Difficulty.Instance.enemies;
        //Random Range to create more variance of the road pattern
        branchOutPercentage = UnityEngine.Random.Range(25, 51);
        branchIterationModifer = UnityEngine.Random.Range(5, 31);
        CreateGrid();
        road = new List<Coordinate>();
        forestTiles = new List<Coordinate>();
        enemyTiles = new List<Coordinate>();
        List<Coordinate> neighbours;
        int ranSpawn = UnityEngine.Random.Range(0, 4);
        //Initializes the startPoint randomly from an option of 4
        switch (ranSpawn)
        {
            case 0:
                {
                    startPoint = new Coordinate(gridSize / 2, 0);
                    break;
                }
            case 1:
                {
                    startPoint = new Coordinate(0, gridSize / 2);
                    break;
                }
            case 2:
                {
                    startPoint = new Coordinate(gridSize / 2, gridSize -1);
                    break;
                }
            case 3:
                {
                    startPoint = new Coordinate(gridSize-1, gridSize / 2);
                    break;
                }
        }
       
        neighbours = GetNeighbours(startPoint,posDirections[0]);
        Coordinate ranDirection = neighbours[UnityEngine.Random.Range(0, neighbours.Count)];
        //Recursive algorithm
        CreateRoad(startPoint,ranDirection,0);

        PopulateGrid();
        foreach (var item in road)
        {
            grid[item.x, item.y].GetComponent<Tile>().SetRoadEndTile(GetSorroundingRoads(item));
        }
       
        gManager.StartGame(forestTiles[UnityEngine.Random.Range(0,forestTiles.Count)],grid,enemyTiles);
        SceneTransition.Instance.FadeScreen(1f, blackScreen, false);
        yield return new WaitForSeconds(1f);
    }

    public bool [] GetSorroundingRoads(Coordinate temp)
    {
        bool [] b = new bool [4];

        for (int i = 0; i < CMaths.roadDirections.Count; i++)
        {
            Coordinate neighbor = CMaths.Add(temp, CMaths.roadDirections[i]);
            if (CMaths.CheckCoodInsideGrid(neighbor, grid.GetLength(0)))
            {
                b[i] = grid[neighbor.x, neighbor.y].GetComponent<Tile>().ReturnTypeIndex() == 0;              
            }

        }

        return b;
    }
  
    public void PopulateGrid()
    {
        //spawns n amount of the given tile types according to its given ratio
        int total, enemies, forest;
        total = availableCoordinates.Count;
        enemies = (int)(total * perEnemies);
        forest = total - enemies;
        foreach (var temp in availableCoordinates)
        {
            int random = UnityEngine.Random.Range(0, total);
            if(random <= enemies)
            {
                total--;
                enemies--;
                grid[temp.x, temp.y].GetComponent<Tile>().SetTile(2,temp);
                enemyTiles.Add(temp);
            }
            else
            {
                total--;
                grid[temp.x, temp.y].GetComponent<Tile>().SetTile(1, temp);
                forestTiles.Add(temp);

            }
        }
        SpawnKeyAndHouse(); 
    }
    public void SpawnKeyAndHouse()
    {
        int ran = UnityEngine.Random.Range(0, forestTiles.Count);
        Coordinate house = forestTiles[ran];
        forestTiles.RemoveAt(ran);
        ran = UnityEngine.Random.Range(0, forestTiles.Count);
        Coordinate  key = forestTiles[ran];
        grid[house.x, house.y].GetComponent<Tile>().SetTile(3, house);
        grid[key.x, key.y].GetComponent<Tile>().SetTile(4, key);

    }
    public void CreateGrid()
    {
        availableCoordinates = new List<Coordinate>();
        grid = new GameObject[gridSize, gridSize];

        for (int i = 0; i < gridSize; i++)
        {
            for (int b = 0; b < gridSize; b++)
            {
                Vector3 pos = new Vector3(tilePrefab.transform.localScale.x * i, tilePrefab.transform.localScale.y, tilePrefab.transform.localScale.z * b);
                grid[i, b] = GameObject.Instantiate(tilePrefab, pos, tilePrefab.transform.rotation, transform);
                availableCoordinates.Add(new Coordinate(i, b));
            }
        }
    }


    public void CreateRoad(Coordinate cood, Coordinate direction, int branch)
    {
        Coordinate temp = cood;
        //Reduces the chances to branch out according to the depth of the recursive algorithm
        float currentBranchPer = branchOutPercentage - (branch * branchIterationModifer);
 
        if (CheckCoodInsideGrid(cood))
        {
            if (!AlreadyInList(cood) && !AdjacentRoads(cood,direction))
            {
                road.Add(cood);
                RemoveFromAvailableCoordinates(cood);
                grid[temp.x, temp.y].GetComponent<Tile>().SetRoadTile(0,temp,direction);

                //adds a little of variation by limiting the lenght of the loop and hence reducing the amount of road tiles and possible branches
                int spaces = AvailableSpacesInDirection(cood, direction);
                int spaceMin = (int)(spaces * .8f);
                int spaceMax = (int)(spaces);
               
                spaces = UnityEngine.Random.Range(spaceMin, spaceMax);

                for (int i = 0; i < spaces; i++)
                {
                    if(currentBranchPer > UnityEngine.Random.Range(0, 101))
                    {
                        currentBranchPer = branchOutPercentage;
                        List<Coordinate> neighbours;
                        neighbours = GetNeighbours(temp, direction);
                        if (neighbours.Count > 0)
                        {
                            int b = branch;
                            if (neighbours.Count == 1)
                            {
                                CreateRoad(CMaths.Add(temp, neighbours[0]), neighbours[0], ++b);
                            }
                            else if (neighbours.Count == 2)
                            {
                                if (UnityEngine.Random.Range(0, 2) == 0)
                                {
                                    CreateRoad(CMaths.Add(temp, neighbours[0]), neighbours[0], ++b);

                                }
                                else
                                {

                                    CreateRoad(CMaths.Add(temp, neighbours[1]), neighbours[1], ++b);

                                }
                            }
                        }

                    }


                    temp = CMaths.Add(temp, direction);
                    if (AdjacentRoads(temp, direction)) break;

                    if (!AlreadyInList(temp))
                    {
                        road.Add(temp);
                        RemoveFromAvailableCoordinates(temp);
                        currentBranchPer += branchOutPercentage;
                        grid[temp.x, temp.y].GetComponent<Tile>().SetRoadTile(0, temp, direction);
                    }
                }
            }
        }
    }
    public bool AdjacentRoads(Coordinate point, Coordinate dir)
    {
        //Check to not add the current position current trajectory and opposite trajectory
        foreach (var item in posDirections)
        {
            if (!CMaths.IsEqual(dir,item) && !CMaths.IsEqual(CMaths.Reverse(dir),item))
            {
                if (AlreadyInList(CMaths.Add(point, item)))
                    return true;
            }
        }
        return false;
    }
    public int AvailableSpacesInDirection(Coordinate point, Coordinate direction)
    {
        int spaces = 0;
        Coordinate temp = Add(point, direction);
        while (CheckCoodInsideGrid(temp))
        {
            spaces++;
            temp = Add(temp, direction);
        }

        return spaces;
    }
    public void RemoveFromAvailableCoordinates(Coordinate coordinate)
    {
        foreach (var item in availableCoordinates)
        {
            if(item.x == coordinate.x && item.y == coordinate.y)
            {
                availableCoordinates.Remove(item);
                break;
            }
        }
    }
    public List<Coordinate> GetNeighbours(Coordinate cood, Coordinate currentDirection)
    {
        //Gives the possible directions the algorithm can branch off too
        //It also excludes adding its current direction and the opposite
        List<Coordinate> nb = new List<Coordinate>();

            foreach (var item in posDirections)
            {
                Coordinate temp = Add(cood, item);
                if (CheckCoodInsideGrid(Add(cood, item)))
                {
                     if (!CMaths.IsEqual(currentDirection, item) && !CMaths.IsEqual(CMaths.Reverse(currentDirection), item))
                     {
                         if (!AlreadyInList(temp))
                         {
                            nb.Add(item);
                         }
                           
                     }

                }
            }
       
        return nb;
    }

    public DirectionState SetState(Coordinate currentDirection)
    {
        DirectionState state = DirectionState.Default;
        if (currentDirection.x == 0 && currentDirection.y == 1 || currentDirection.x == 1 && currentDirection.y == 1 
            || currentDirection.x == -1 && currentDirection.y == 1 || currentDirection.x == 1 && currentDirection.y == 0
            || currentDirection.x == -1 && currentDirection.y == 0)
        {
            state = DirectionState.Up;
            return state;
        }
        else if (currentDirection.x == 0 && currentDirection.y == -1 || currentDirection.x == 1 && currentDirection.y == -1
            || currentDirection.x == -1 && currentDirection.y == -1 || currentDirection.x == 1 && currentDirection.y == 0
            || currentDirection.x == -1 && currentDirection.y == 0)
        {
            state = DirectionState.Down;
            return state;
        }
        else if (currentDirection.x == -1 && currentDirection.y == -1 || currentDirection.x == -1 && currentDirection.y == 1
            || currentDirection.x == -1 && currentDirection.y == 0 || currentDirection.x == 0 && currentDirection.y == -1
            || currentDirection.x == 0 && currentDirection.y == 1)
        {
            state = DirectionState.Left;
            return state;
        }
        else if (currentDirection.x == 1 && currentDirection.y == -1 || currentDirection.x == 1 && currentDirection.y == 1
           || currentDirection.x == 1 && currentDirection.y == 0 || currentDirection.x == 0 && currentDirection.y == -1
           || currentDirection.x == 0 && currentDirection.y == 1)
        {
            state = DirectionState.Right;
            return state;
        }
        return state;
    }
    public bool AlreadyInList(Coordinate cood)
    {
        foreach (var item in road)
        {
            if(item.x == cood.x && item.y == cood.y)
            {
                return true;
            }
        }
        return false;
    }
    public bool CheckCoodInsideGrid(Coordinate currentPoint)
    {
        Coordinate minPoint = new Coordinate(0, 0);
        Coordinate maxPoint = new Coordinate(gridSize, gridSize);
        if ((currentPoint.x >= minPoint.x && currentPoint.x < maxPoint.x) && (currentPoint.y >= minPoint.y && currentPoint.y < maxPoint.y))
        {
            return true;
        }
        return false;
    }

    public Coordinate Add(Coordinate a, Coordinate b)
    {
        return new Coordinate(a.x + b.x, a.y+ b.y);
    }

}

[Serializable]
public class Coordinate
{
    public Coordinate(int a, int b)
    {
        x = a;
        y = b;
    }
   public int x;
   public int y;
}

