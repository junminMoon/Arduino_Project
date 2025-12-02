using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuCanvas : MonoBehaviour
{

    public void toGame1()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "BallMaze")
        {
            return;
        }
        SceneManager.LoadScene("BallMaze");
    }

    public void toGame2()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "PhysicsPlayground")
        {
            return;
        }
        SceneManager.LoadScene("PhysicsPlayground");
    }

    public void toGame3()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "DartScene")
        {
            return;
        }
        SceneManager.LoadScene("DartScene");
    }

    public void toDebug()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "TestScene 2")
        {
            return;
        }
        SceneManager.LoadScene("TestScene 2");
    }
}
