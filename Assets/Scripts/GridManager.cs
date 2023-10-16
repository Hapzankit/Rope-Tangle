using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance; // Singleton for easy access
    public List<BoardSetup> AllGrids = new List<BoardSetup>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }

    public List<BoardSetup> GetPotentialTargets(Vector3 startPos)
    {
        List<BoardSetup> potentialTargets = new List<BoardSetup>();

        foreach (var board in AllGrids)
        {
            if (!board.hasBennUsed && FindObjectOfType<RopeManager>().IsValidConnection(startPos, board.transform.position))
            {
                potentialTargets.Add(board);
            }
        }

        return potentialTargets;
    }
}
