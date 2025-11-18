using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToBallMaze : MonoBehaviour
{
    public void LoadingNewScene()
    {
        SceneManager.LoadScene("BallMaze");
    }
}