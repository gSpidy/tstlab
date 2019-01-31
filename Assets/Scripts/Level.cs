using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class Level : MonoBehaviourSingleton<Level>
{
    [SerializeField] private Tile m_wallTile,m_wallRTile,m_wallDTile,m_exitTile;

    private Tilemap _tmap;
    private Tilemap Tilemap => _tmap ?? (_tmap = GetComponentInChildren<Tilemap>());

    public const int FIELD_SIZE = 15;

    private Cell[,] cells = new Cell[FIELD_SIZE, FIELD_SIZE];

    private Vector2? _wmin;
    public Vector2 WorldMinimum => (_wmin ?? (_wmin = Vector2.one * -1.5f)).Value;

    private Vector2? _wmax;
    public Vector2 WorldMaximum => (_wmax ?? (_wmax = Vector2.one * (FIELD_SIZE + .5f))).Value;

    struct Cell
    {
        public bool rBorder, dBorder;
        public int group;
    }

    public static IEnumerable<Vector2Int> CellsEnumerable
    {
        get
        {
            return Enumerable.Range(0, FIELD_SIZE)
                .SelectMany(i => Enumerable.Range(0, FIELD_SIZE)
                    .Select(j => new Vector2Int(i, j))
                );
        }
    }
    
    public Vector2Int ExitPosition { get; private set;}

    /// <summary>
    /// генерация лабиринта
    /// </summary>
    /// <param name="levelNum">#уровня</param>
    /// <param name="breakSomeWalls">разбивать ли некоторые стены чтоб лабиринт не был идеальным</param>
    public void Generate(int levelNum, bool breakSomeWalls = true)
    {
        var oldRandomState = Random.state;
        Random.InitState(levelNum*1000);
        
        var stripe = new Cell[FIELD_SIZE];
        var lastGroup = 0;

        var s = "LAB:\n";

        for (int j = 0; j < FIELD_SIZE; j++)
        {
            //запихиваем ячейки без групп в новые группы
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                if (stripe[i].group < 1)
                    stripe[i].group = ++lastGroup;
            }

            //правые границы
            for (int i = 0; i < FIELD_SIZE - 1; i++)
            {
                if (stripe[i + 1].group == stripe[i].group || Random.value < .5f)
                    stripe[i].rBorder = true;
                else
                {
                    var mergeGroup = stripe[i + 1].group;
                    for (var k = 0; k < FIELD_SIZE; k++)
                        if (stripe[k].group == mergeGroup)
                            stripe[k].group = stripe[i].group;
                }
            }

            stripe[FIELD_SIZE - 1].rBorder = true;

            //нижние границы
            var openings = 0;
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                stripe[i].dBorder = Random.value < .5f;
                if (!stripe[i].dBorder) openings++;

                if (i == FIELD_SIZE - 1 || stripe[i + 1].group != stripe[i].group)
                {
                    if (openings < 1)
                        stripe[i].dBorder = false;

                    openings = 0;
                }
            }

            //ластецкая строка
            if (j == FIELD_SIZE - 1)
                for (int i = 0; i < FIELD_SIZE; i++)
                {
                    stripe[i].dBorder = true;

                    if (i < FIELD_SIZE - 1 && stripe[i].group != stripe[i + 1].group)
                        stripe[i].rBorder = false;
                }
            
            //заполняем поле (лабиринт с толстыми стенками)
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                cells[i, j] = stripe[i];

                if (breakSomeWalls)
                {
                    cells[i, j].rBorder &= Random.value < .9f;
                    cells[i, j].dBorder &= Random.value < .9f;
                }
                
                cells[i, j].rBorder |= i==FIELD_SIZE-1;
                cells[i, j].dBorder |= j==FIELD_SIZE-1;
            }
            

            //debug lab map
            for (int i = 0; i < FIELD_SIZE; i++)
            {
                var d = stripe[i].dBorder;
                var r = stripe[i].rBorder;

                s += $"{(d ? "_" : " ")}{stripe[i].group:00}{(d ? "_" : " ")}{(r ? "|" : " ")}";
            }
            s += "\n";

            for (int i = 0; i < FIELD_SIZE; i++)
            {
                stripe[i].rBorder = false;
                if (stripe[i].dBorder)
                    stripe[i].group = 0;
                stripe[i].dBorder = false;
            }
        }
        
        Vector2Int exit = new Vector2Int();
        
        exit.x = Random.Range(0, FIELD_SIZE);
        exit.y = Random.Range(0, FIELD_SIZE);

        var isMin = Random.value < .5f;

        if (Random.value < .5f)
            exit.x = isMin ? 0 : FIELD_SIZE-1;
        else
            exit.y = isMin ? 0 : FIELD_SIZE-1;
        
        ExitPosition = exit;

        Random.state = oldRandomState;

        Debug.Log(s);
    }

    public bool CanStep(Vector2Int current, Vector2Int direction)
    {
        if (direction == Vector2Int.zero) return true;
        if (direction.x!=0 && direction.y!=0) return false;
        
        var p = current + direction;
        
        if (p.x < 0 || p.y < 0 || p.x >= FIELD_SIZE || p.y >= FIELD_SIZE) return false;

        if (p.y == current.y)
        {
            return !(p.x>current.x? 
                cells[current.x,current.y].rBorder:
                cells[p.x,p.y].rBorder);
        }

        return !(p.y>current.y? 
            cells[current.x,current.y].dBorder:
            cells[p.x,p.y].dBorder);
    }

    private List<Vector2Int> AStarPath(Vector2Int origin, Vector2Int destination)
    {
        var frontier = new Queue<Vector2Int>();
        frontier.Enqueue(origin);

        var cameFrom = new Dictionary<Vector2Int, Vector2Int?>();
        var costSoFar = new Dictionary<Vector2Int, int>();

        cameFrom[origin] = null;
        costSoFar[origin] = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();

            if (current == destination)
                break;

            new[] //surroundings
                {
                    Vector2Int.left,
                    Vector2Int.right,
                    Vector2Int.down,
                    Vector2Int.up,
                }
                .Where(dir =>CanStep(current,dir))
                .Select(x=>current+x)
                .OrderBy(x => (destination - x).sqrMagnitude)
                .ForEach(next =>
                {
                    var newCost = costSoFar[current] + 1;
                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        frontier.Enqueue(next);
                        cameFrom[next] = current;
                    }
                });
        }

        if (!cameFrom.ContainsKey(destination)) return null;

        //reconstruct path
        var path = new List<Vector2Int> {destination};

        var tmp = cameFrom[destination];
        while (tmp.HasValue)
        {
            path.Insert(0, tmp.Value);
            tmp = cameFrom[tmp.Value];
        }

        return path;
    }

    public int PathLength(Vector2Int origin, Vector2Int destination) => AStarPath(origin, destination)?.Count ?? -1;

    public Vector2Int PathStep(Vector2Int origin, Vector2Int destination)
    {
        var path = AStarPath(origin, destination);
        if (path == null || path.Count < 2) return origin;
        return path[1];
    }

    public Vector2Int PathStepDir(Vector2Int origin, Vector2Int destination) => PathStep(origin, destination) - origin;

    public void RenderField()
    {
        Tilemap.ClearAllTiles();

        for (int i = 0; i < FIELD_SIZE; i++)
        {
            for (int j = 0; j < FIELD_SIZE; j++)
            {
                if(cells[i,j].rBorder)
                    Tilemap.SetTile(new Vector3Int(i, FIELD_SIZE-1-j, 1), m_wallRTile);
                
                if(cells[i,j].dBorder)
                    Tilemap.SetTile(new Vector3Int(i, FIELD_SIZE-1-j, 2), m_wallDTile);
            }

            Tilemap.SetTile(new Vector3Int(i, FIELD_SIZE, 0), m_wallDTile);
            Tilemap.SetTile(new Vector3Int(-1, i, 0), m_wallRTile);
        }
        
        Tilemap.SetTile(new Vector3Int(ExitPosition.x,FIELD_SIZE-1-ExitPosition.y,0), m_exitTile);
    }
}