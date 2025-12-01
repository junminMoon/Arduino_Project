using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private MazeGameManager manager;

    void Start()
    {
        manager = FindObjectOfType<MazeGameManager>();
    }
    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Ball"))
        {
            if (manager != null) manager.OnGameClear();
        }
    }
}