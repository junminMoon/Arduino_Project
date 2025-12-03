using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject ballPrefab;
    public GameObject goalPrefab;
    private ArduinoPackage arduinoPackage;

    [Header("Maze Settings")]
    // ★ [중요] 외벽(19) 안에 넣으려면 8칸(16)으로 설정해야 합니다. (9칸은 18이라서 겹침)
    [Tooltip("가로 칸 수 (8 추천)")]
    public int mazeWidth = 8;
    [Tooltip("세로 칸 수 (8 추천)")]
    public int mazeHeight = 8;

    public float cellSize = 2.0f;

    [Header("Fine Tuning (미세 조정)")]
    [Tooltip("벽의 Y 높이 (실시간 조절 가능)")]
    public float wallY = 0.0f;

    [Tooltip("미로 전체 위치 미세 조정 (X, Z축 이동)")]
    public Vector3 mazeOffset = Vector3.zero; // ◀️ [추가] 이걸로 X축 쏠림 해결!

    Vector3 startPos;
    public GameObject Maze;
    public const float ResetAngleTolerance = 30.0f;

    // 내부 변수
    private Vector3 mazeStartPosition;
    private Cell[,] grid;
    private struct Cell
    {
        public bool isVisited;
        public bool wallBottom;
        public bool wallRight;
    }

    public GameObject CurrentBall { get; private set; }
    public GameObject CurrentGoal { get; private set; }

    public void InitMaze()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);

        // 중앙 정렬 기준점 계산
        float totalWidth = mazeWidth * cellSize;
        float totalHeight = mazeHeight * cellSize;
        // (0,0)을 중심으로 하기 위해 너비/높이의 절반만큼 이동
        mazeStartPosition = new Vector3(-totalWidth / 2.0f, 0, totalHeight / 2.0f);

        InitializeGrid();
        GenerateMaze(0, 0);
        DrawMaze();
        SetRandomStartAndExit();
    }

    void InitializeGrid()
    {
        grid = new Cell[mazeWidth, mazeHeight];
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                grid[x, y].isVisited = false;
                grid[x, y].wallBottom = true;
                grid[x, y].wallRight = true;
            }
        }
    }

    void GenerateMaze(int x, int y)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        grid[x, y].isVisited = true;
        stack.Push(new Vector2Int(x, y));

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current);

            if (neighbors.Count > 0)
            {
                stack.Push(current);
                Vector2Int chosen = neighbors[Random.Range(0, neighbors.Count)];
                RemoveWallBetween(current, chosen);
                grid[chosen.x, chosen.y].isVisited = true;
                stack.Push(chosen);
            }
        }
    }

    List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int x = cell.x;
        int y = cell.y;

        if (y > 0 && !grid[x, y - 1].isVisited) neighbors.Add(new Vector2Int(x, y - 1));
        if (y < mazeHeight - 1 && !grid[x, y + 1].isVisited) neighbors.Add(new Vector2Int(x, y + 1));
        if (x > 0 && !grid[x - 1, y].isVisited) neighbors.Add(new Vector2Int(x - 1, y));
        if (x < mazeWidth - 1 && !grid[x + 1, y].isVisited) neighbors.Add(new Vector2Int(x + 1, y));

        return neighbors;
    }

    void RemoveWallBetween(Vector2Int current, Vector2Int chosen)
    {
        if (chosen.x > current.x) grid[current.x, current.y].wallRight = false;
        else if (chosen.x < current.x) grid[chosen.x, chosen.y].wallRight = false;
        else if (chosen.y > current.y) grid[current.x, current.y].wallBottom = false;
        else if (chosen.y < current.y) grid[current.x, chosen.y].wallBottom = false;
    }

    void DrawMaze()
    {
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                // ★ [수정] 모든 좌표 계산에 'mazeOffset' 더하기

                // 1. 바닥 벽
                if (grid[x, y].wallBottom && y < mazeHeight - 1)
                {
                    Vector3 localPos = new Vector3(
                        mazeStartPosition.x + (x * cellSize) + (cellSize / 2),
                        wallY,
                        mazeStartPosition.z - (y * cellSize) - cellSize
                    ) + mazeOffset; // ◀️ 오프셋 적용

                    GameObject wall = Instantiate(wallPrefab, transform);
                    wall.transform.localPosition = localPos;
                    wall.transform.localRotation = Quaternion.Euler(0, 90, 0);
                }

                // 2. 오른쪽 벽
                if (grid[x, y].wallRight && x < mazeWidth - 1)
                {
                    Vector3 localPos = new Vector3(
                        mazeStartPosition.x + (x * cellSize) + cellSize,
                        wallY,
                        mazeStartPosition.z - (y * cellSize) - (cellSize / 2)
                    ) + mazeOffset; // ◀️ 오프셋 적용

                    GameObject wall = Instantiate(wallPrefab, transform);
                    wall.transform.localPosition = localPos;
                    wall.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }
    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackage>();
    }

    void Update()
    {
        float currentAngleDeviation = Quaternion.Angle(Maze.transform.rotation, Quaternion.identity);
        if ((arduinoPackage.IsButtonADown || Input.GetKeyDown(KeyCode.A)) && currentAngleDeviation < ResetAngleTolerance)
        {
            Destroy(CurrentBall);
            CurrentBall = Instantiate(ballPrefab, transform);
            CurrentBall.transform.localPosition = startPos;
        }  
    }
    void SetRandomStartAndExit()
    {
        Vector2Int[] corners = new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(mazeWidth - 1, 0),
            new Vector2Int(0, mazeHeight - 1),
            new Vector2Int(mazeWidth - 1, mazeHeight - 1)
        };

        int exitIndex = Random.Range(0, 4);
        Vector2Int exitCoords = corners[exitIndex];
        Vector2Int startCoords = corners[3 - exitIndex];

        Vector3 exitPos = GetCellCenterPosition(exitCoords.x, exitCoords.y);
        exitPos.y = 0.1f;
        CurrentGoal = Instantiate(goalPrefab, transform);
        CurrentGoal.transform.localPosition = exitPos; 

        startPos = GetCellCenterPosition(startCoords.x, startCoords.y);
        startPos.y = 3.0f;
        CurrentBall = Instantiate(ballPrefab, transform);
        CurrentBall.transform.localPosition = startPos;
    }

    Vector3 GetCellCenterPosition(int x, int y)
    {
        return new Vector3(
            mazeStartPosition.x + (x * cellSize) + (cellSize / 2),
            0,
            mazeStartPosition.z - (y * cellSize) - (cellSize / 2)
        ) + mazeOffset; 
    }
}