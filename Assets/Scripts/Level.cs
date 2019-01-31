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
    [SerializeField] private Tile m_wallTile,m_exitTile;

    private Tilemap _tmap;
    private Tilemap Tilemap => _tmap ?? (_tmap = GetComponentInChildren<Tilemap>());

    public const int FIELD_SIZE = 15;
    bool[,] field = new bool[FIELD_SIZE + 1, FIELD_SIZE + 1];

    /// <summary>
    /// true if cell is wall
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    public bool this[int i, int j]
    {
        get
        {
            if (i < 0 || i > FIELD_SIZE ||
                j < 0 || j > FIELD_SIZE) return true;

            return field[i, j];
        }
    }

    private Vector2? _wmin;
    public Vector2 WorldMinimum => (_wmin ?? (_wmin = Vector2.one * -1.5f)).Value;

    private Vector2? _wmax;
    public Vector2 WorldMaximum => (_wmax ?? (_wmax = Vector2.one * (FIELD_SIZE + .5f))).Value;

    struct Cell
    {
        public bool rBorder, dBorder;
        public int group;
    }

    public Vector2Int[] FreeCells
    {
        get
        {
            return Enumerable.Range(0, FIELD_SIZE + 1)
                .SelectMany(i => Enumerable.Range(0, FIELD_SIZE + 1)
                    .Select(j => Tuple.Create(j, field[i, j]))
                    .Where(x => !x.Item2)
                    .Select(x => new Vector2Int(i, x.Item1))
                )
                .ToArray();
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
        
        var stripeSz = (FIELD_SIZE + 1) / 2;
        var stripe = new Cell[stripeSz];
        var lastGroup = 0;

        var s = "LAB:\n";

        for (int j = 0; j < stripeSz; j++)
        {
            //запихиваем ячейки без групп в новые группы
            for (int i = 0; i < stripeSz; i++)
            {
                if (stripe[i].group < 1)
                    stripe[i].group = ++lastGroup;
            }

            //правые границы
            for (int i = 0; i < stripeSz - 1; i++)
            {
                if (stripe[i + 1].group == stripe[i].group || Random.value < .5f)
                    stripe[i].rBorder = true;
                else
                {
                    var mergeGroup = stripe[i + 1].group;
                    for (var k = 0; k < stripeSz; k++)
                        if (stripe[k].group == mergeGroup)
                            stripe[k].group = stripe[i].group;
                }
            }

            stripe[stripeSz - 1].rBorder = true;

            //нижние границы
            var openings = 0;
            for (int i = 0; i < stripeSz; i++)
            {
                stripe[i].dBorder = Random.value < .5f;
                if (!stripe[i].dBorder) openings++;

                if (i == stripeSz - 1 || stripe[i + 1].group != stripe[i].group)
                {
                    if (openings < 1)
                        stripe[i].dBorder = false;

                    openings = 0;
                }
            }

            //ластецкая строка
            if (j == stripeSz - 1)
                for (int i = 0; i < stripeSz; i++)
                {
                    stripe[i].dBorder = true;

                    if (i < stripeSz - 1 && stripe[i].group != stripe[i + 1].group)
                        stripe[i].rBorder = false;
                }

            //заполняем поле (лабиринт с толстыми стенками)
            for (int i = 0; i < stripeSz; i++)
            {
                var x = i * 2;
                var y = j * 2;

                field[x, y] = false;
                field[x + 1, y] = i==stripeSz-1 || stripe[i].rBorder && (!breakSomeWalls || Random.value<.75f);
                field[x, y + 1] = j==stripeSz-1 || stripe[i].dBorder && (!breakSomeWalls || Random.value<.75f);
                field[x + 1, y + 1] = true;

                if (i > 0)
                    field[x - 1, y + 1] |= stripe[i].dBorder;
            }

            //debug lab map
            for (int i = 0; i < stripeSz; i++)
            {
                var d = stripe[i].dBorder;
                var r = stripe[i].rBorder;

                s += $"{(d ? "_" : " ")}{stripe[i].group:00}{(d ? "_" : " ")}{(r ? "|" : " ")}";
            }
            s += "\n";

            for (int i = 0; i < stripeSz; i++)
            {
                stripe[i].rBorder = false;
                if (stripe[i].dBorder)
                    stripe[i].group = 0;
                stripe[i].dBorder = false;
            }
        }
        
        var middle = FIELD_SIZE / 2;
        for (int i = middle-1; i <= middle+1; i++)
            for (int j = middle - 1; j <= middle + 1; j++)
            {
                field[i, j] = false;
            }

        Vector2Int exit = new Vector2Int();
        do
        {
            exit.x = Random.Range(0, FIELD_SIZE + 1);
            exit.y = Random.Range(0, FIELD_SIZE + 1);

            var isMin = Random.value < .5f;

            if (Random.value < .5f)
                exit.x = isMin ? 0 : FIELD_SIZE;
            else
                exit.y = isMin ? 0 : FIELD_SIZE;

        } while (field[exit.x, exit.y]);
        ExitPosition = exit;

        Random.state = oldRandomState;

        Debug.Log(s);
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
                .Select(x=>current+x)
                .Where(x => x.x >= 0 && x.y >= 0 && x.x <= FIELD_SIZE && x.y <= FIELD_SIZE)
                .Where(x => !field[x.x, x.y])
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

        for (int i = 0; i <= FIELD_SIZE; i++)
        {
            for (int j = 0; j <= FIELD_SIZE; j++)
            {
                if (!field[i, j]) continue;
                Tilemap.SetTile(new Vector3Int(i, j, 0), m_wallTile);
            }

            Tilemap.SetTile(new Vector3Int(i, -1, 0), m_wallTile);
            Tilemap.SetTile(new Vector3Int(-1, i, 0), m_wallTile);
        }
        
        Tilemap.SetTile(new Vector3Int(-1, -1, 0), m_wallTile);
        Tilemap.SetTile(new Vector3Int(ExitPosition.x,ExitPosition.y,0), m_exitTile);
    }
}