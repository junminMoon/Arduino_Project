using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Tooltip("미로 생성을 위해 사용할 벽 프리팹")]
    public GameObject wallPrefab;

    [Tooltip("미로의 가로 크기 (칸 수)")]
    public int mazeWidth = 8;

    [Tooltip("미로의 세로 크기 (칸 수)")]
    public int mazeHeight = 8;

    // 미로의 한 칸(셀)의 크기. 벽 프리팹의 길이와 일치해야 합니다.
    private float cellSize = 2.0f;
    // 미로가 생성될 기준 위치 (가장 왼쪽 위) - y축 값을 1로 변경
    private Vector3 mazeStartPosition = new Vector3(-8, 1, 8); // <-- Y 값을 1로 변경

    // 미로의 데이터를 저장할 2차원 배열
    private Cell[,] grid;

    // Cell 구조체: 미로의 한 칸에 대한 정보를 저장
    private struct Cell
    {
        public bool isVisited;
        public bool wallBottom; // 아래쪽 벽 존재 여부
        public bool wallRight;  // 오른쪽 벽 존재 여부
    }

    void Start()
    {
        InitializeGrid();
        GenerateMaze(0, 0);
        DrawMaze();
        CreateExit();
    }

    // 미로 데이터를 저장할 그리드를 초기화하는 함수
    void InitializeGrid()
    {
        grid = new Cell[mazeWidth, mazeHeight];
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                grid[x, y].isVisited = false;
                grid[x, y].wallBottom = true; // 모든 벽을 일단 생성
                grid[x, y].wallRight = true;
            }
        }
    }

    // 재귀적 백트래킹 알고리즘으로 미로 생성
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

    // 방문하지 않은 이웃 셀들을 찾는 함수
    List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int x = cell.x;
        int y = cell.y;

        // 위쪽 이웃
        if (y > 0 && !grid[x, y - 1].isVisited) neighbors.Add(new Vector2Int(x, y - 1));
        // 아래쪽 이웃
        if (y < mazeHeight - 1 && !grid[x, y + 1].isVisited) neighbors.Add(new Vector2Int(x, y + 1));
        // 왼쪽 이웃
        if (x > 0 && !grid[x - 1, y].isVisited) neighbors.Add(new Vector2Int(x - 1, y));
        // 오른쪽 이웃
        if (x < mazeWidth - 1 && !grid[x + 1, y].isVisited) neighbors.Add(new Vector2Int(x + 1, y));

        return neighbors;
    }

    // 두 셀 사이의 벽을 제거하는 함수
    void RemoveWallBetween(Vector2Int current, Vector2Int chosen)
    {
        // chosen이 current의 오른쪽에 있을 때
        if (chosen.x > current.x) grid[current.x, current.y].wallRight = false;
        // chosen이 current의 왼쪽에 있을 때
        else if (chosen.x < current.x) grid[chosen.x, chosen.y].wallRight = false;
        // chosen이 current의 아래쪽에 있을 때
        else if (chosen.y > current.y) grid[current.x, current.y].wallBottom = false;
        // chosen이 current의 위쪽에 있을 때
        else if (chosen.y < current.y) grid[current.x, chosen.y].wallBottom = false;
    }


    // 그리드 데이터를 바탕으로 실제 벽 오브젝트를 생성하는 함수
    void DrawMaze()
    {
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                // 아래쪽 벽 생성
                if (grid[x, y].wallBottom) // if (grid[x, y].wallBottom && y < mazeHeight -1) 에서 조건문 삭제 (외곽 하단 벽도 스크립트가 그리도록)
                {
                    Vector3 position = new Vector3(mazeStartPosition.x + x * cellSize, mazeStartPosition.y, mazeStartPosition.z - y * cellSize - cellSize / 2);
                    // 벽의 높이를 맞추기 위해 Y축 Scale을 wallPrefab의 Y Scale과 동일하게 유지하거나, wallPrefab 자체의 Y scale을 조정합니다.
                    // 현재 wallPrefab의 y scale은 5입니다.
                    GameObject wall = Instantiate(wallPrefab, position, Quaternion.Euler(0, 90, 0), transform);
                    // wall.transform.localScale = new Vector3(1, 5, 2); // 프리팹에서 이미 설정되어 있으므로 필요 없을 수 있습니다.
                }
                // 오른쪽 벽 생성
                if (grid[x, y].wallRight) // if (grid[x, y].wallRight && x < mazeWidth - 1) 에서 조건문 삭제 (외곽 우측 벽도 스크립트가 그리도록)
                {
                    Vector3 position = new Vector3(mazeStartPosition.x + x * cellSize + cellSize / 2, mazeStartPosition.y, mazeStartPosition.z - y * cellSize);
                    GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, transform);
                    // wall.transform.localScale = new Vector3(1, 5, 2); // 프리팹에서 이미 설정되어 있으므로 필요 없을 수 있습니다.
                }
            }
        }
    }

    // 출구를 만드는 함수
    void CreateExit()
    {
        // 출구 위치: x=2, y=7 (가장 아래쪽 줄, 왼쪽에서 3번째 칸)
        // 사용자가 만든 출구 위치(-1, 1, -9)와 유사한 지점입니다.
        grid[2, mazeHeight - 1].wallBottom = false;

        // 해당 위치의 벽을 실제로 제거
        Vector3 exitWallPosition = new Vector3(mazeStartPosition.x + 2 * cellSize, mazeStartPosition.y, mazeStartPosition.z - (mazeHeight - 1) * cellSize - cellSize / 2);

        // 해당 위치에 있을 수 있는 벽 오브젝트를 찾아서 제거
        foreach (Transform child in transform)
        {
            if (Vector3.Distance(child.position, exitWallPosition) < 0.1f)
            {
                Destroy(child.gameObject);
            }
        }
    }
}