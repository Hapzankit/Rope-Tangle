using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;


public enum GridType
{
    Starting,
    Star,
    Elipse,
    Wormhole,
    Yellow,
    Blue,
    Red,
    Green,
    SkyBlue,
    None
}
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(LineRenderer))]
public class BoardSetup : MonoBehaviour
{
    [Header("Grid Type")]
    public GridType gridType;

    [Header("Line Render")]
    public LineRenderer lineRenderer;

    [Header("Bool")]
    public bool hasBennUsed;
    public bool hasBennUsedWormHole;

    [Header("Highlighting")]
    public Color defaultColor = Color.white;
    public GameObject highlightSprite;
    private GameObject ins;
    public RopeManager RpManager;

    private bool isHighlighted = false;

    void Start()
    {
        //GridManager.Instance.AllGrids.Add(this);
        ins = GameObject.Find("Instruction Panel");
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.25f;
        lineRenderer.endWidth = 0.25f;
        highlightSprite = this.transform.GetChild(0).gameObject;
        if(highlightSprite.transform.parent.parent.name == "2,2" || highlightSprite.transform.parent.parent.name == "2,6"
          || highlightSprite.transform.parent.parent.name == "5,6" || highlightSprite.transform.parent.parent.name == "6,4"
          || highlightSprite.transform.parent.parent.name == "6,10" || highlightSprite.transform.parent.parent.name == "7,7")
        {
            highlightSprite.GetComponent<SpriteRenderer>().enabled = true;
        }
        else
        {
            highlightSprite.GetComponent<SpriteRenderer>().enabled = false;
        }
     //   highlightSprite.GetComponent<SpriteRenderer>().enabled = false;
        RpManager = FindAnyObjectByType<RopeManager>();
    }


    private void Update()
    {
        if (RpManager.instruction.activeInHierarchy)
        {
            GetComponent<BoxCollider2D>().enabled = false;
        }
        if (RpManager.confirmSubmission.activeInHierarchy)
        {
            GetComponent<BoxCollider2D>().enabled = false;
        }
        else
        {
            GetComponent<BoxCollider2D>().enabled = true;
        }

        if (RpManager.gameOverBool || RpManager.instruction.activeInHierarchy)
        {
            if (RpManager.sceneIndex == 1)
            {
                if (RpManager.moveTrack == 10)
                {
                    if (RpManager.ellipsePassCount > 8)
                    {
                        if (RpManager.gameOver.activeInHierarchy)
                        {
                            GetComponent<BoxCollider2D>().enabled = false;
                        }
                    }
                }
            }
        }

        if (RpManager.gameOverBool || RpManager.instruction.activeInHierarchy)
        {
            if (RpManager.sceneIndex == 2)
            {
                if (RpManager.moveTrack == 5)
                {
                    if (RpManager.gameOver.activeInHierarchy)
                    {
                        GetComponent<BoxCollider2D>().enabled = false;
                    }
                }
            }
        }    
    }
    public void Highlight()
    {
        if (highlightSprite != null)
        {
            highlightSprite.GetComponent<SpriteRenderer>().enabled = true;
            isHighlighted = true;
        }
    }

    public void Unhighlight()
    {
        if (highlightSprite != null)
        {
            highlightSprite.GetComponent<SpriteRenderer>().enabled = false;
            isHighlighted = false;
        }
    }

    public bool IsHighlighted()
    {
        return isHighlighted;
    }
}
