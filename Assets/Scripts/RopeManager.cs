using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;
using Mono.Cecil.Cil;
using UnityEngine.UI;
using UnityEngine.SocialPlatforms;
using Unity.Burst.CompilerServices;
using static RopeManager;
using System.Runtime.InteropServices.WindowsRuntime;

public class RopeManager : MonoBehaviour
{
    [Header("GridType")]
    private GridType currentTargetColor;
    private GridType previousGridType;
    private GridType previousTempGridType;

    [Header("Bool")]
    private bool isDragging;
    private bool hasActiveLine = false; // Added to track active line
    private bool wormhole;
    private bool passedThroughEllipse = false;
    public bool gameOverBool;
    public bool starUse;
    public bool isUseUndo;
    public bool isUseUndotrack;
    public bool incorrectStartPos;
    bool isDiagonal;

    [Header("GameObject")]
    private GameObject tempObject;
    private GameObject nextStartObject;
    public GameObject gameOver;
    public GameObject confirmSubmission;
    [SerializeField]
    private GameObject[] Elipse;
    public GameObject start;
    public GameObject instruction;
    private GameObject ellipseTrack;
    private GameObject wormholeTrack;
    public GameObject[] allWormhole;

    [Header("Int")]
    public int moveTrack = 0;
    [SerializeField]
    public int sceneIndex;
    public int ellipsePassCount = 0;
    private float score;

    [Header("Other")]
    private Vector3 endPoint;
    private BoardSetup boardSetup;
    [SerializeField]
    private List<LineRenderer> activeLineRenderers = new List<LineRenderer>();

    [Header("Text")]
    public TMP_Text scoreText; // Assign this in the inspector by dragging your Text UI component

    public class LineData
    {
        public Vector3[] Positions { get; set; }
    }

    private Stack<Action> undoActions = new Stack<Action>();
    private Stack<Action> redoActions = new Stack<Action>();

    private List<GameObject> nextobj = new List<GameObject>();

    private Stack<LineData> redoLineDatas = new Stack<LineData>();  // Stack to store line data for redo

    private Stack<float> scoreStack = new Stack<float>();
    private Stack<float> scoreStackRedo = new Stack<float>();

    // Start is called before the first frame update
    void Start()
    {
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
        UpdateTargetColor();
        Time.timeScale = 1;
        //HighlightMatchingColorGrids(start.transform.position);
        confirmSubmission.GetComponent<Button>().onClick.AddListener(() => gameOver.SetActive(true));
    }


    /// <summary>
    /// Tarck RayCast
    /// </summary>
    private void Update()
    {
        if (gameOver.activeInHierarchy)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider == null)
            {
                DestroyCurrentLine();
                return;
            }

            if (confirmSubmission.activeInHierarchy)
            {
                DestroyCurrentLine();

                return;
            }


            //  If you are starting a new line on a correct tile:
            if (moveTrack == 0 && hit.collider != null && hit.collider.transform.parent.name == "6,6" && !starUse && !wormhole)
            {
                print("CC");
                StartNewLine(hit.collider);
                incorrectStartPos = false;
                hasActiveLine = true;
            }

            //WormHole
            else if (hit.collider != null && hit.collider.GetComponent<BoardSetup>().gridType == GridType.Wormhole && wormhole && hit.collider != nextStartObject)
            {
                if (hit.collider.tag == "wormhole" && hit.collider.GetComponent<BoardSetup>().hasBennUsedWormHole == false && !starUse)
                {
                    print(hit.collider.transform.parent.name + "       " + nextStartObject.transform.parent.name);
                    StartNewLine(hit.collider);
                    wormholeTrack = hit.collider.gameObject;
                    incorrectStartPos = false;
                    hasActiveLine = true;
                }
                else
                {
                    DestroyCurrentLine();
                }
            }

            //Star

            else if (hit.collider != null && hit.collider.GetComponent<BoardSetup>().gridType == GridType.Star && hit.collider.transform.parent.name == nextStartObject.transform.parent.name)
            {
                if (starUse)
                {
                    print("Ssssss");
                    StartNewLine(hit.collider);
                    incorrectStartPos = false;
                    hasActiveLine = true;
                }
            }

            //Any Other Grid
            // Or if you are continuing from the last correct tile:
            else if (moveTrack >= 1 && hit.collider != null && hit.collider.transform.parent.name == nextStartObject.transform.parent.name && !starUse)
            {
                print("DD");
                StartNewLine(hit.collider);
                incorrectStartPos = false;
                hasActiveLine = true;
            }

            else
            {
                print("Incorrect Starting Point");
                incorrectStartPos = true;
            }
        }

        if (incorrectStartPos)
        {
            return;
        }

        if (isDragging)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0f;
            activeLineRenderers[activeLineRenderers.Count - 1].SetPosition(1, mousePosition);
            endPoint = mousePosition;

            if(sceneIndex == 1)
            {
                Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition);

                if (hitCollider)
                {
                    if (hitCollider.CompareTag("Ellipse"))
                    {
                        passedThroughEllipse = true;
                        ellipseTrack = hitCollider.gameObject;
                    }
                }
            }
           
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider == null)
            {
                DestroyCurrentLine();
                return;
            }

            // If the current line ends on the correct tile:
            if (hit.collider != null && hit.collider.GetComponent<BoardSetup>().gridType == currentTargetColor
                || hit.collider.GetComponent<BoardSetup>().gridType == GridType.Star
                || hit.collider.GetComponent<BoardSetup>().gridType == GridType.Wormhole)
            {
                if (tempObject != null && hit.collider.GetComponent<BoardSetup>().gridType == GridType.Star)
                {
                    print("Star");
                    StarLine(hit.collider);
                }

                else if (tempObject != null && hit.collider.GetComponent<BoardSetup>().gridType == GridType.Wormhole 
                    && !hit.collider.CompareTag("UsedWormhole"))
                {
                    print("Wormhole");

                    WormholeLine(hit.collider);
                }

                else if (tempObject != null && hit.collider.GetComponent<BoardSetup>().gridType != GridType.Wormhole 
                    && hit.collider.GetComponent<BoardSetup>().gridType != GridType.Star)
                {
                    print("Finalize");

                    // Finalize the connection and prepare for the next rope
                    FinalizeLine(hit.collider);
                }

                else
                {
                    DestroyCurrentLine();
                }
            }

            // If it's an incorrect ending point:
            else
            {
                DestroyCurrentLine();
            }
        }

    }


    /// <summary>
    /// Create a line render from a target point
    /// </summary>
    /// <param name="collider"></param>
    private void StartNewLine(Collider2D collider)
    {
        if(sceneIndex == 1)
        {
            if(moveTrack == 10)
            {
                return;
            }
        }
        isUseUndo = false;
        Vector3 startPoss = collider.transform.position;
        HighlightMatchingColorGrids(startPoss);

        // If this isn't the very first move, validate the direction
        if (moveTrack != 0 && previousGridType != GridType.Wormhole)
        {
            Vector3 startPos = tempObject.transform.position;
            Vector3 endPos = collider.transform.position;
        }


        previousGridType = collider.GetComponent<BoardSetup>().gridType;
        tempObject = collider.gameObject;
        boardSetup = tempObject.GetComponent<BoardSetup>();

        boardSetup.hasBennUsed = true;

        if(wormholeTrack != null)
        {          
            wormholeTrack.GetComponent<BoxCollider2D>().enabled = false;
        }


        LineRenderer newLine = Instantiate(boardSetup.lineRenderer, boardSetup.transform);
        if (newLine.transform.childCount >= 2 && newLine.transform.GetChild(1) != null)
        {
            newLine.transform.GetChild(1).gameObject.SetActive(false);
        }
        int tilesCrossed = CalculateTilesCrossed(startPoss, endPoint);
        if (collider.CompareTag("wormhole"))
        {
            print("woooooooo");
            collider.gameObject.tag = "UsedWormhole";
            collider.transform.GetChild(2).tag = "UsedWormhole";
            collider.GetComponent<BoardSetup>().hasBennUsedWormHole = true;
            score += tilesCrossed - 0.5f;
        }
        

        activeLineRenderers.Add(newLine);

        // Record the undo action for this new line
        undoActions.Push(() =>
        {
            // Determine the number of positions in the LineRenderer
            int positionCount = newLine.positionCount;
            Vector3[] positions = new Vector3[positionCount];

            // Get the positions from the LineRenderer
            newLine.GetPositions(positions);
            Debug.Log($" Current Position after undo : {newLine.transform.parent.parent.name}");
            if (newLine.transform.parent.GetComponent<BoardSetup>().gridType == GridType.Wormhole)
            {
                newLine.transform.parent.GetComponent<BoardSetup>().enabled = true;
                ResetWormholeState(newLine.transform.parent.GetComponent<BoxCollider2D>());
            }
            if (newLine.transform.parent.GetComponent<BoardSetup>().gridType == GridType.Star)
            {
                starUse = true;
            }
            if (newLine.transform.parent.GetComponent<BoardSetup>().gridType != GridType.Star)
            {
                print("AA");
                newLine.transform.parent.GetComponent<BoardSetup>().enabled = true;
                newLine.transform.parent.transform.GetComponent<BoardSetup>().hasBennUsed = false;
                nextStartObject = newLine.transform.parent.gameObject;

                print("Nextttttt " + nextStartObject.transform.parent.name);
            }
            else
            {
                nextStartObject = newLine.transform.parent.gameObject;
            }
            //print("Next Start Object $$$ : " + nextStartObject.transform.parent.parent.name);

            // Store line data for potential redo
            print("entered");
            LineData lineData = new LineData
            {
                Positions = positions
            };
            redoLineDatas.Push(lineData);
            print("entered2");
            redoActions.Push(RedoAddLine);
            boardSetup.hasBennUsed = false;

            Vector2 endPoint = newLine.GetPosition(1); // assuming index 1 is the end point
            Vector2 startPoint = newLine.GetPosition(0); // assuming index 1 is the end point
            RaycastHit2D hit = Physics2D.Raycast(endPoint, Vector2.zero);
            RaycastHit2D hit1 = Physics2D.Raycast(startPoint, Vector2.zero);

            if (hit.collider != null || hit1.collider != null)
            {
                GameObject gameObjectName = hit.collider.gameObject;
                GameObject gameObjectNameStart = hit1.collider.gameObject;
                Debug.Log(gameObjectName.transform.parent.name + " " + gameObjectNameStart.transform.parent.parent.name);
                if (gameObjectName.GetComponent<BoardSetup>().gridType != GridType.Wormhole && gameObjectName.transform.parent.name != "1,4"
                && gameObjectName.transform.parent.name != "4,7" && gameObjectName.transform.parent.name != "6,2"
                && gameObjectName.transform.parent.name != "8,8")
                {
                    moveTrack--;
                }
                if (gameObjectName.GetComponent<BoardSetup>().gridType == GridType.Wormhole)
                {
                    ResetWormholeState(gameObjectName.GetComponent<BoxCollider2D>());
                }

                print("Typeeeee : " + gameObjectNameStart.GetComponent<BoardSetup>().gridType);
                if (gameObjectNameStart.GetComponent<BoardSetup>().gridType != GridType.Star)
                {
                    print("Bnjbcjgw : " + gameObjectNameStart.transform.parent.parent.name);
                    //gameObjectNameStart.transform.parent.transform.GetComponent<BoardSetup>().enabled = true;
                    //gameObjectNameStart.transform.parent.transform.GetComponent<BoardSetup>().hasBennUsed = false;
                    //nextStartObject = gameObjectNameStart.transform.parent.parent.gameObject;
                }
            }

            UpdateTargetColor();
            Destroy(newLine.gameObject);
            activeLineRenderers.Remove(newLine);

        });
        isDragging = true;
        Vector3 centerpoint = collider.transform.position;
        centerpoint.z = 0f;
        newLine.SetPosition(0, centerpoint);
    }


    /// <summary>
    /// Set the line onto the grid from start point to target point
    /// </summary>
    /// <param name="collider"></param>
    private void FinalizeLine(Collider2D collider)
    {
        if (isUseUndo)
            return;

        print("asdf");
        Vector3 startPos = tempObject.transform.position;
        Vector3 endPos = collider.transform.position;

        if (!IsValidConnection(startPos, endPos))
        {
            DestroyCurrentLine();
            return;
        }


        nextStartObject = collider.gameObject;

        if (sceneIndex == 1)
        {
            if (passedThroughEllipse)
            {
                ellipsePassCount++;
                if (ellipseTrack.CompareTag("Ellipse"))
                {
                    print("sdEllipse");
                    ellipseTrack.tag = "UsedEllipse";
                }
            }
        }

        if(wormholeTrack != null)
        {
            ResetWormholeState(wormholeTrack.GetComponent<BoxCollider2D>());
        }
       

        nextobj.Add(nextStartObject);
       
        Vector3 centerpoint = collider.transform.position;
        centerpoint.z = 0f;
        activeLineRenderers[activeLineRenderers.Count - 1].SetPosition(1, centerpoint);
        StartCoroutine(AddDelay());

        //HighlightMatchingColorGrids(startPos);

        hasActiveLine = false;
        wormhole = false;
        passedThroughEllipse = false;
        starUse = false;

        if (!isUseUndo)
        {
            moveTrack++;
        }

        int tilesCrossed = CalculateTilesCrossed(startPos, endPos);
        print("Tile Crossed : " + tilesCrossed);


        if (tempObject.transform.parent.name == "6,6" && !isDiagonal)
        {
            print("a ^^^");
            score += tilesCrossed + 1;
            scoreStack.Push(tilesCrossed + 1);
            scoreText.text = ("" + Mathf.Ceil(score));
        }

        else if(tempObject.transform.parent.name == "6,6" && isDiagonal)
        {
            print("b $$$");
            score += tilesCrossed;
            scoreStack.Push(tilesCrossed);
            scoreText.text = ("" + Mathf.Ceil(score));
        }

        else if(isDiagonal)
        {
            print("D >>>>");
            score += tilesCrossed - 1;
            scoreStack.Push(tilesCrossed - 1);
            scoreText.text = ("" + Mathf.Ceil(score));
        }

        else if (tempObject.GetComponent<BoardSetup>().gridType == GridType.Wormhole)
        {
            print("qqqqqqqqq");
            score += tilesCrossed - 2f;
            scoreStack.Push(tilesCrossed - 1f);
            scoreText.text = ("" + Mathf.Ceil(score));
            if (tempObject.transform.GetChild(2) != null)
            {
                for (int i = 0; i < allWormhole.Length; i++)
                {
                    if (tempObject.transform.parent.name != allWormhole[i].transform.parent.name)
                    {
                        print(allWormhole[i].transform.parent.name);
                        ResetWormholeState(allWormhole[i].transform.GetComponent<BoxCollider2D>());
                    }
                }
            }
        }

        else
        {
            print("c @@@");
            score += tilesCrossed;
            scoreStack.Push(tilesCrossed);
            scoreText.text = ("" + Mathf.Ceil(score));
        }

        var potentialTargets = GridManager.Instance.GetPotentialTargets(startPos);

        foreach (var board in GridManager.Instance.AllGrids)
        {
            if (board.gridType == currentTargetColor && potentialTargets.Contains(board))
            {
                board.Unhighlight();
            }
        }

        if (isUseUndotrack)
        {
            while (redoActions.Count > 0)
            {
                redoActions.Clear();
                scoreStackRedo.Clear();
            }
            isUseUndotrack = false;
        }
    }


    /// <summary>
    /// This function will call when we set line on star type grid
    /// </summary>
    /// <param name="collider"></param>
    private void StarLine(Collider2D collider)
    {
        print("AA");

        if (isUseUndo)
            return;

        Vector3 startPos = tempObject.transform.position;
        Vector3 endPos = collider.transform.position;

        print("BB");

        if (!IsValidDirection(startPos, endPos))
        {
            DestroyCurrentLine();
            return;
        }


        if (sceneIndex == 1)
        {
            if (passedThroughEllipse)
            {
                ellipsePassCount++;
                if (collider.CompareTag("Ellipse"))
                {
                    collider.gameObject.tag = "UsedEllipse";
                }
            }
        }

        if (collider.CompareTag("wormhole"))
        {
            collider.gameObject.tag = "UsedWormhole";
            collider.GetComponent<BoardSetup>().hasBennUsedWormHole = true;
        }

        boardSetup.hasBennUsed = true;
        nextStartObject = collider.gameObject;

        Vector3 centerpoint = collider.transform.position;
        centerpoint.z = 0f;
        activeLineRenderers[activeLineRenderers.Count - 1].SetPosition(1, centerpoint);
        StartCoroutine(AddDelay());
        hasActiveLine = false;
        passedThroughEllipse = false;
        starUse = true;
        int tilesCrossed = CalculateTilesCrossed(startPos, endPos);
        scoreStack.Push(tilesCrossed);
        score += tilesCrossed /*+ 0.5f*/;
        scoreText.text = ("" + Mathf.Ceil(score));
    }


    /// <summary>
    /// This function will call when we set line on Wormhole type grid
    /// </summary>
    /// <param name="collider"></param>
    private void WormholeLine(Collider2D collider)
    {
        if (isUseUndo)
            return;

        Vector3 startPos = tempObject.transform.position;
        Vector3 endPos = collider.transform.position;


        if (!IsValidDirection(startPos, endPos))
        {
            DestroyCurrentLine();
            return;
        }

        if (sceneIndex == 1)
        {
            if (passedThroughEllipse)
            {
                ellipsePassCount++;
                if (collider.CompareTag("Ellipse"))
                {
                    collider.gameObject.tag = "UsedEllipse";
                }
            }
        }

        wormhole = true;

        if (collider.CompareTag("wormhole"))
        {
            collider.gameObject.tag = "UsedWormhole";
            collider.GetComponent<BoardSetup>().hasBennUsedWormHole = true;
        }
        wormholeTrack = collider.gameObject;

        for (int i = 0; i < allWormhole.Length; i++)
        {
            if (wormholeTrack.transform.parent.name != allWormhole[i].transform.parent.name && allWormhole[i].transform.childCount <2)
            {
                ResetWormholeState(allWormhole[i].GetComponent<BoxCollider2D>());
            }
            if (wormholeTrack.transform.parent.name != allWormhole[i].transform.parent.name && allWormhole[i].transform.childCount > 2)
            {
                print("qwefdsaxz");
                if (allWormhole[i].transform.GetChild(2) != null)
                {
                    ResetWormholeState(allWormhole[i].transform.GetChild(2).GetComponent<BoxCollider2D>());
                }
            }
        }

        boardSetup.hasBennUsed = true;
        nextStartObject = collider.gameObject;

        Vector3 centerpoint = collider.transform.position;
        centerpoint.z = 0f;
        activeLineRenderers[activeLineRenderers.Count - 1].SetPosition(1, centerpoint);
        StartCoroutine(AddDelay());
        hasActiveLine = false;
        passedThroughEllipse = false;
        int tilesCrossed = CalculateTilesCrossed(startPos, endPos);
        scoreStack.Push(tilesCrossed - 1.5f);
        score += tilesCrossed - 1.5f;
        scoreText.text = ("" + Mathf.Ceil(score));
    }


    /// <summary>
    /// Reset the wormhole state by changing the tag
    /// </summary>
    /// <param name="collider"></param>
    private void ResetWormholeState(Collider2D collider)
    {
        print("sdx");
        collider.gameObject.tag = "wormhole";
        collider.GetComponent<BoardSetup>().hasBennUsedWormHole = false;
    }


    /// <summary>
    /// Destroy the line if we set the line on wrong target
    /// </summary>
    private void DestroyCurrentLine()
    {
        if (!hasActiveLine)
            return;

        LineRenderer lineToDestroy = activeLineRenderers[activeLineRenderers.Count - 1];

        // Record the undo action for removing the line
        undoActions.Pop();

        Destroy(activeLineRenderers[activeLineRenderers.Count - 1].gameObject);
        activeLineRenderers.RemoveAt(activeLineRenderers.Count - 1);
        hasActiveLine = false;
        if (tempObject.transform.CompareTag("UsedWormhole"))
        {
            tempObject.transform.tag = "wormhole";
            tempObject.GetComponent<BoardSetup>().hasBennUsed = false;
            tempObject.GetComponent<BoardSetup>().hasBennUsedWormHole = false;

        }
    }


    /// <summary>
    /// Score calculation
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    private int CalculateTilesCrossed(Vector3 startPos, Vector3 endPos)
    {
        int tilesCrossed = 0;

        // Calculate the distance between the start and end positions
        float distance = Vector3.Distance(startPos, endPos);

        // Given that tiles are 0.5x0.5 in size, adjust the tiles crossed calculation
        tilesCrossed = Mathf.CeilToInt(distance / 0.5f);  // Divided by tile size

        return tilesCrossed;
    }


    /// <summary>
    /// Updating target color after we connected to the correct target color
    /// </summary>
    public void UpdateTargetColor()
    {
        if (sceneIndex == 2)
        {
            if (moveTrack == 0)
            {
                currentTargetColor = GridType.Yellow;

                previousTempGridType = GridType.Red;
            }
            if (moveTrack == 1)
            {
                currentTargetColor = GridType.Blue;
                //    previousGridType = GridType.Red;
                previousTempGridType = GridType.Yellow;
            }
            if (moveTrack == 2)
            {
                currentTargetColor = GridType.Red;
                previousTempGridType = GridType.Blue;
            }
            if (moveTrack == 3)
            {
                currentTargetColor = GridType.Green;
                previousTempGridType = GridType.Red;
            }
            if (moveTrack == 4)
            {
                currentTargetColor = GridType.SkyBlue;
                previousTempGridType = GridType.Green;
            }
            if (moveTrack == 5)
            {
                confirmSubmission.SetActive(true);
                //gameOver.SetActive(true);
                //Time.timeScale = 0;
            }
            else if (moveTrack < 5)
            {
                confirmSubmission.SetActive(false);
            }
        }

        else if (sceneIndex == 1)
        {
            if (moveTrack == 0)
            {
                currentTargetColor = GridType.Yellow;
                previousTempGridType = GridType.Red;
            }
            if (moveTrack == 1)
            {
                currentTargetColor = GridType.Blue;
                previousTempGridType = GridType.Yellow;
            }
            if (moveTrack == 2)
            {
                currentTargetColor = GridType.Red;
                previousTempGridType = GridType.Blue;
            }
            if (moveTrack == 3)
            {
                currentTargetColor = GridType.Green;
                previousTempGridType = GridType.Red;
            }
            if (moveTrack == 4)
            {
                currentTargetColor = GridType.SkyBlue;
                previousTempGridType = GridType.Green;
            }
            if (moveTrack == 5)
            {
                currentTargetColor = GridType.Yellow;
                previousTempGridType = GridType.SkyBlue;
            }
            if (moveTrack == 6)
            {
                currentTargetColor = GridType.Blue;
                previousTempGridType = GridType.Yellow;
            }
            if (moveTrack == 7)
            {
                currentTargetColor = GridType.Red;
                previousTempGridType = GridType.Blue;
            }
            if (moveTrack == 8)
            {
                currentTargetColor = GridType.Green;
                previousTempGridType = GridType.Red;
            }
            if (moveTrack == 9)
            {
                currentTargetColor = GridType.SkyBlue;
                previousTempGridType = GridType.Green;
                confirmSubmission.SetActive(false);
            }
            if (moveTrack == 10)
            {
                currentTargetColor = GridType.None;
                if (ellipsePassCount >= 8)
                {
                    confirmSubmission.SetActive(true);
                }
            }
            if(moveTrack < 10)
            {
                confirmSubmission.SetActive(false);
            }
        }
    }


    /// <summary>
    /// Adding delay because we turn off script on the gameobject just after setting line
    /// </summary>
    /// <returns></returns>
    IEnumerator AddDelay()
    {
        yield return new WaitForSeconds(0.2f);
        print("Delayyyy");
        if ((previousGridType == previousTempGridType || previousGridType == GridType.Star || previousGridType == GridType.Wormhole) && boardSetup.hasBennUsed)
        {
            if (tempObject.transform.parent.name == "6,6")
            {
                if (tempObject.transform.name == "Square(Clone)")
                {
                    tempObject.transform.GetChild(1).GetComponent<BoardSetup>().enabled = true;
                    tempObject.transform.GetChild(1).GetComponent<BoardSetup>().hasBennUsed = false;
                }
                else
                {
                    UpdateTargetColor();
                    tempObject.GetComponent<BoardSetup>().enabled = false;
                }

            }

            if (tempObject.transform.parent.name == "1,4" || tempObject.transform.parent.name == "4,7" || tempObject.transform.parent.name == "6,2" ||
                tempObject.transform.parent.name == "8,8")
            {
                tempObject.transform.GetChild(2).GetComponent<BoardSetup>().enabled = true;
                tempObject.transform.GetChild(2).GetComponent<BoardSetup>().hasBennUsed = false;
                UpdateTargetColor();
            }
            //if (tempObject.GetComponent<BoardSetup>().gridType == GridType.Star)
            //{
            //    tempObject.transform.GetChild(1).GetComponent<BoardSetup>().enabled = true;
            //    tempObject.transform.GetChild(1).GetComponent<BoardSetup>().hasBennUsed = false;
            //    UpdateTargetColor();
            //}
            else
            {
                UpdateTargetColor();
                HighlightMatchingColorGrids(nextStartObject.transform.position);
                tempObject.GetComponent<BoardSetup>().enabled = false;

            }
        }
    }


    /// <summary>
    /// verify that our line isn't parallel to any other line near it
    /// </summary>
    /// <returns></returns>
    public bool AreLinesParallel(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
    {
        // Calculate directions of the lines

        Vector3 dir1 = (end1 - start1).normalized;
        Vector3 dir2 = (end2 - start2).normalized;
        // Calculate the dot product, if it's close to 1 or -1, then they're parallel
        float dotProduct = Vector3.Dot(dir1, dir2);
        //print("Dottttt " + dotProduct);

        const float epsilon = 0.0001f; // You can adjust this value based on your needs

        return Math.Abs(dotProduct - 0.5f) < epsilon || Math.Abs(dotProduct + 1f) < epsilon;
    }


    /// <summary>
    /// Check Valid Direction for connections
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    public bool IsValidDirection(Vector3 startPos, Vector3 endPos)
    {
        float deltaX = Mathf.Abs(endPos.x - startPos.x);
        float deltaY = Mathf.Abs(endPos.y - startPos.y);
        const float threshold = 0.1f;  // Slightly above half of the grid size
        bool isHorizontal = Mathf.Abs(deltaY/* - 0.5f*/) < threshold;
        bool isVertical = Mathf.Abs(deltaX/* - 0.5f*/) < threshold;
        isDiagonal = deltaX > 0 && deltaY > 0 && Mathf.Abs(1.0f - (deltaX / deltaY)) < threshold;
        return isHorizontal || isVertical || isDiagonal;
    }


    /// <summary>
    /// Common function for checking both condiction of parallel and direction 
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    public bool IsValidConnection(Vector3 startPos, Vector3 endPos)
    {
        if (!IsValidDirection(startPos, endPos))
        {
            return false;
        }
        foreach (var lineRenderer in activeLineRenderers)
        {
            if (lineRenderer.positionCount >= 2)
            {
                Vector3 existingStart = lineRenderer.GetPosition(0);
                Vector3 existingEnd = lineRenderer.GetPosition(1);
                if (AreLinesParallel(startPos, endPos, existingStart, existingEnd))
                {
                    float distance = Mathf.Abs((endPos.y - startPos.y) * existingStart.x - (endPos.x - startPos.x) * existingStart.y + endPos.x * startPos.y - endPos.y * startPos.x) / Mathf.Sqrt((endPos.y - startPos.y) * (endPos.y - startPos.y) + (endPos.x - startPos.x) * (endPos.x - startPos.x));
                    if (distance < 0.001f)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }


    /// <summary>
    /// Redo Adding line render
    /// </summary>
    private void RedoAddLine()
    {
        if (redoLineDatas.Count == 0) return;

        
        LineData lineData = redoLineDatas.Pop();
 
        LineRenderer newLine = Instantiate(boardSetup.lineRenderer, boardSetup.transform);
        newLine.SetPositions(lineData.Positions);
        activeLineRenderers.Add(newLine);
        //  nextStartObject = activeLineRenderers.
        print(newLine.transform.parent.parent.name);
        if (newLine.transform.parent.parent.name == "1,4" || newLine.transform.parent.parent.name == "4,7"
            || newLine.transform.parent.parent.name == "6,2" || newLine.transform.parent.parent.name == "8,8")
        {
            newLine.transform.GetChild(1).gameObject.SetActive(false);
        }

        Vector2 endPoint = newLine.GetPosition(1); // assuming index 1 is the end point
        RaycastHit2D hit = Physics2D.Raycast(endPoint, Vector2.zero);

        if (hit.collider != null)
        {
            string gameObjectName = hit.collider.gameObject.transform.parent.name;
            Debug.Log(gameObjectName);
            nextStartObject = hit.collider.gameObject;
        }

        scoreStack.Push(scoreStackRedo.Peek());
        score += scoreStackRedo.Pop();
        scoreText.text = ("" + Mathf.Ceil(score));
        boardSetup.hasBennUsed = true;
        moveTrack++;
        UpdateTargetColor();
        undoActions.Push(() =>
        {
            // Determine the number of positions in the LineRenderer
            int positionCount = newLine.positionCount;
            Vector3[] positions = new Vector3[positionCount];

            // Get the positions from the LineRenderer
            newLine.GetPositions(positions);
            Debug.Log($" Current Position after undo : {newLine.transform.parent.parent.name}");
            if (newLine.transform.parent.GetComponent<BoardSetup>().gridType == GridType.Wormhole)
            {
                newLine.transform.parent.GetComponent<BoardSetup>().enabled = true;
                ResetWormholeState(newLine.transform.parent.GetComponent<BoxCollider2D>());
            }
            if (newLine.transform.parent.GetComponent<BoardSetup>().gridType == GridType.Star)
            {
                starUse = true;
            }
            if (newLine.transform.parent.GetComponent<BoardSetup>().gridType != GridType.Star)
            {
                print("AA");
                newLine.transform.parent.GetComponent<BoardSetup>().enabled = true;
                newLine.transform.parent.transform.GetComponent<BoardSetup>().hasBennUsed = false;
                nextStartObject = newLine.transform.parent.gameObject;

                print("Nextttttt " + nextStartObject.transform.parent.name);
            }
            else
            {
                nextStartObject = newLine.transform.parent.gameObject;
            }
            //print("Next Start Object $$$ : " + nextStartObject.transform.parent.parent.name);

            // Store line data for potential redo
            print("entered");
            LineData lineData = new LineData
            {
                Positions = positions
            };
            redoLineDatas.Push(lineData);
            print("entered2");
            redoActions.Push(RedoAddLine);
            boardSetup.hasBennUsed = false;

            Vector2 endPoint = newLine.GetPosition(1); // assuming index 1 is the end point
            Vector2 startPoint = newLine.GetPosition(0); // assuming index 1 is the end point
            RaycastHit2D hit = Physics2D.Raycast(endPoint, Vector2.zero);
            RaycastHit2D hit1 = Physics2D.Raycast(startPoint, Vector2.zero);

            if (hit.collider != null || hit1.collider != null)
            {
                GameObject gameObjectName = hit.collider.gameObject;
                GameObject gameObjectNameStart = hit1.collider.gameObject;
                Debug.Log(gameObjectName.transform.parent.name + " " + gameObjectNameStart.transform.parent.parent.name);
                if (gameObjectName.GetComponent<BoardSetup>().gridType != GridType.Wormhole && gameObjectName.transform.parent.name != "1,4"
                && gameObjectName.transform.parent.name != "4,7" && gameObjectName.transform.parent.name != "6,2"
                && gameObjectName.transform.parent.name != "8,8")
                {
                    moveTrack--;
                }
                if (gameObjectName.GetComponent<BoardSetup>().gridType == GridType.Wormhole)
                {
                    ResetWormholeState(gameObjectName.GetComponent<BoxCollider2D>());
                }

                print("Typeeeee : " + gameObjectNameStart.GetComponent<BoardSetup>().gridType);
                if (gameObjectNameStart.GetComponent<BoardSetup>().gridType != GridType.Star)
                {
                    print("Bnjbcjgw : " + gameObjectNameStart.transform.parent.parent.name);
                    //gameObjectNameStart.transform.parent.transform.GetComponent<BoardSetup>().enabled = true;
                    //gameObjectNameStart.transform.parent.transform.GetComponent<BoardSetup>().hasBennUsed = false;
                    //nextStartObject = gameObjectNameStart.transform.parent.parent.gameObject;
                }
            }

            UpdateTargetColor();
            Destroy(newLine.gameObject);
            activeLineRenderers.Remove(newLine);

        });

    }


    /// <summary>
    /// Undo last Action perform by player Attach on button 
    /// </summary>
    public void UndoLastAction()
    {
        if (gameOver.activeInHierarchy)
        {
            return;
        }
        if (confirmSubmission.activeInHierarchy)
        {
            return;
        }
        if (undoActions.Count > 0)
        {
            isUseUndo = true;
            isUseUndotrack = true;
            var undo = undoActions.Pop();
            undo();
            scoreStackRedo.Push(scoreStack.Peek());
            score -= scoreStack.Pop();
            scoreText.text = ("" + Mathf.Ceil(score));
        }
    }


    /// <summary>
    /// Redo last action perform by player
    /// </summary>
    public void RedoLastUndoneAction()
    {
        if (gameOver.activeInHierarchy)
        {
            return;
        }
        if (confirmSubmission.activeInHierarchy)
        {
            return;
        }
        if (redoActions.Count > 0)
        {
            var redo = redoActions.Pop();
            redo();
            
        }
    }


    /// <summary>
    /// Responsiable for highlighting the Grid 
    /// </summary>
    /// <param name="startPos"></param>
    private void HighlightMatchingColorGrids(Vector3 startPos)
    {
        var potentialTargets = GridManager.Instance.GetPotentialTargets(startPos);

        foreach (var board in GridManager.Instance.AllGrids)
        {
            if (board.gridType == currentTargetColor && potentialTargets.Contains(board))
            {
                board.Highlight();
            }
            else
            {
                board.Unhighlight();
            }
        }
    }


    /// <summary>
    /// Check Game Is Over so we can optimize update function
    /// </summary>
    /// <param name="gameState"></param>
    public void IsGameOver(bool gameState)
    {
        gameOverBool = gameState;
    }

    public void UndoTrack()
    {

    }

}
