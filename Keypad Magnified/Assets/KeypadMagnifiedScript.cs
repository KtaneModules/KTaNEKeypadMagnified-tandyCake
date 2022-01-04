﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class KeypadMagnifiedScript : MonoBehaviour {

    public KMBombInfo bomb;
    public KMAudio audio;
    public KMSelectable[] keys;
    public GameObject[] leds;
    public Material[] ledColors; // black green red blue cyan
    public SpriteRenderer[] buttonLabels;

    public Sprite[] copyright, dragon, euro, kitty, N, omega, six, star, wisp = new Sprite[4];
    private Sprite[][] allSprites = new Sprite[9][];
    
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private bool uninteractable;
    char[] symbols = new char[] { 'Ҋ', 'б', 'Ҩ', 'Ω', '©', 'Ѭ', 'Ӭ', '☆', 'Ѯ' };
    int[][] grids = new int[][] //4x9
    {
        new int[] {0, 2, 5, 7},
        new int[] {1, 3, 6, 4},
        new int[] {2, 1, 8, 3},
        new int[] {3, 4, 7, 2},
        new int[] {4, 6, 1, 8},
        new int[] {5, 7, 2, 0},
        new int[] {6, 8, 0, 5},
        new int[] {7, 5, 4, 6},
        new int[] {8, 0, 3, 1}
    };
    int[][] table = new int[][]
    {
        new int[] { 0, 4, 1, 2, 8, 5, 3, 6, 7 },
        new int[] { 7, 3, 6, 0, 4, 1, 5, 2, 8 },
        new int[] { 2, 5, 8, 6, 3, 7, 4, 0, 1 },
        new int[] { 3, 1, 4, 7, 5, 2, 0, 8, 6 },
        new int[] { 6, 8, 2, 1, 0, 4, 7, 3, 5 },
        new int[] { 5, 7, 0, 8, 6, 3, 2, 1, 4 },
        new int[] { 8, 6, 7, 4, 2, 0, 1, 5, 3 },
        new int[] { 4, 0, 3, 5, 1, 6, 8, 7, 2 },
        new int[] { 1, 2, 5, 3, 7, 8, 6, 4, 0 }
    };
    int[] chosenGrid = new int[4];
    int chosenGridIndex;
    int chosenPosition;
    char bigSymbol;
    string[] positions = { "top-left", "top-right", "bottom-left", "bottom-right" };
    int[] shufflingPositions = { 0, 1, 2, 3 };

    string tableUsing;
    int tableIndex;
    string tableDirection;
    bool reverse;

    List<int> pressSymbols = new List<int>();
    int[] pressPositions;

    int stage = 0;
    int keyIndexPressed; 
    bool[] isPressed = new bool[4];
    bool[] isAnimating = new bool[4];

    void Awake ()
    {
        moduleId = moduleIdCounter++;
        
        foreach (KMSelectable key in keys)
            key.OnInteract += delegate () { KeyPress(key); return false; };
        allSprites = new[] { N, six, wisp, omega, copyright, kitty, euro, star, dragon };
    }
    void Start()
    {
        GetGrid();
        DisplayInfo();
        GetTableSection();
        GetPresses();
    }
    void KeyPress(KMSelectable key)
    {
        keyIndexPressed = Array.IndexOf(keys, key);
        if (uninteractable || stage == 4 || isPressed[keyIndexPressed] || isAnimating[keyIndexPressed])
            return;
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, key.transform);
        keys[keyIndexPressed].AddInteractionPunch(1f);
        if (keyIndexPressed == pressPositions[stage])
        {
            StartCoroutine(CorrectPress(key, keyIndexPressed));
            Debug.LogFormat("[Keypad Magnified #{0}] You presed key {1}, that was correct.", moduleId, keyIndexPressed + 1);
            if (stage != 3)
                stage++;
            else
            {
                uninteractable = true;
                Debug.LogFormat("[Keypad Magnified #{0}] Module solved.", moduleId);
                StartCoroutine(SolveAnimation());
            }
        }
        else
        {
            StartCoroutine(IncorrectPress(key, keyIndexPressed));
            Debug.LogFormat("[Keypad Magnified #{0}] You presed key {1}, that was incorrect.", moduleId, keyIndexPressed + 1);
        }
    }

    void GetGrid()
    {
        chosenGridIndex = UnityEngine.Random.Range(0, 9);
        chosenPosition = UnityEngine.Random.Range(0, 4);
        chosenGrid = grids[chosenGridIndex];
        bigSymbol = symbols[chosenGrid[chosenPosition]];
        
        Debug.LogFormat("[Keypad Magnified #{0}] The large symbol is {1}. The lit LED is on the {2} key. The correct grid is grid {3}. ({4})", moduleId, bigSymbol, positions[chosenPosition], chosenGridIndex + 1, chosenGrid.Select(x => symbols[x]).Join());
    }
    void DisplayInfo()
    {
        shufflingPositions.Shuffle();
        leds[chosenPosition].GetComponent<MeshRenderer>().material = ledColors[3];
        for (int i = 0; i < 4; i++) // Randomizes the labels.
        {
            buttonLabels[shufflingPositions[i]].sprite = allSprites[chosenGrid[chosenPosition]][i];
        }
    }
    void GetTableSection()
    {
        bool firstEven = bomb.GetSerialNumberNumbers().First() % 2 == 0;
        bool lastEven = bomb.GetSerialNumberNumbers().Last() % 2 == 0;
        if (bomb.GetSerialNumberNumbers().Last() == 0)
            tableIndex = (bomb.GetSerialNumberLetters().First() - 65 ) % 9; 
        else tableIndex = bomb.GetSerialNumberNumbers().Last() - 1;

        if (!firstEven && lastEven)
        {
            tableUsing = "row";
            tableDirection = "left-to-right";
            reverse = false;
        }
        if (firstEven && !lastEven)
        {
            tableUsing = "column";
            tableDirection = "top-to-bottom";
            reverse = false;
        }
        if (firstEven && lastEven)
        {
            tableUsing = "row";
            tableDirection = "right-to-left";
            reverse = true;
        }
        if (!firstEven && !lastEven)
        {
            tableUsing = "column";
            tableDirection = "bottom-to-top";
            reverse = true;
        }
        Debug.LogFormat("[Keypad Magnified #{0}] The module is reading {1} {2} of the table from {3}", moduleId, tableUsing, tableIndex + 1, tableDirection); 
    }
    void GetPresses()
    {
        if (tableUsing == "row")
            for (int i = 0; i < 9; i++)
                if (chosenGrid.Contains(table[tableIndex][i]))
                    pressSymbols.Add(table[tableIndex][i]);
        if (tableUsing == "column")
            for (int i = 0; i < 9; i++)
                if (chosenGrid.Contains(table[i][tableIndex]))
                    pressSymbols.Add(table[i][tableIndex]);
        if (reverse)
            pressSymbols.Reverse();
        pressPositions = pressSymbols.Select(x => Array.IndexOf(chosenGrid, x)).ToArray();

        Debug.LogFormat("[Keypad Magnified #{0}] The correct symbol order is {1}, or keys {2} in reading order.", moduleId,
            pressSymbols.Select(x => symbols[x]).Join(", "), pressPositions.Select(x => x + 1).Join(", "));

    }


    IEnumerator CorrectPress(KMSelectable key, int pressedPos)
    {
        isPressed[pressedPos] = true;
        while (key.transform.localPosition.y > -0.01)
        {
            key.transform.localPosition -= new Vector3(0, 0.0015f, 0);
            yield return null;
        }
        if (pressedPos != chosenPosition)
            leds[pressedPos].GetComponent<MeshRenderer>().material = ledColors[1]; //sets led to green if not the blue LED
        else
            leds[pressedPos].GetComponent<MeshRenderer>().material = ledColors[4]; //if it's the blue LED it's set to cyan
        yield return null;
    }
    IEnumerator IncorrectPress(KMSelectable key, int pressedPos)
    {
        StartCoroutine(StrikeBounce(key, pressedPos));
        GetComponent<KMBombModule>().HandleStrike();
        leds[pressedPos].GetComponent<MeshRenderer>().material = ledColors[2];
        yield return new WaitForSecondsRealtime(0.75f);
        if (pressedPos == chosenPosition)
            leds[pressedPos].GetComponent<MeshRenderer>().material = ledColors[3];

        else leds[pressedPos].GetComponent<MeshRenderer>().material = ledColors[0];
        yield return null;
    }
    IEnumerator StrikeBounce(KMSelectable key, int pressedPos)
    {
        isAnimating[pressedPos] = true;
        while (keys[pressedPos].transform.localPosition.y > -0.005)
        {
            keys[pressedPos].transform.localPosition -= new Vector3(0, 0.00075f, 0);
            yield return null;
        }
        while (keys[pressedPos].transform.localPosition.y < 0)
        {
            keys[pressedPos].transform.localPosition += new Vector3(0, 0.00075f, 0);
            yield return null;
        }
        isAnimating[pressedPos] = false;
    }
    IEnumerator SolveAnimation()
    {
        yield return new WaitForSecondsRealtime(1f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        while (keys[0].transform.localPosition.y < 0)
        {
            for (int i = 0; i < 4; i++)
            {
                keys[i].transform.localPosition += new Vector3(0, 0.001f, 0);
            }
            yield return null;
        }
        for (int i = 0; i < 24; i++)
        {
            shufflingPositions.Shuffle();
            for (int j = 0; j < 4; j++)
            {
                leds[j].GetComponent<MeshRenderer>().material = ledColors[UnityEngine.Random.Range(0, 5)];
                buttonLabels[shufflingPositions[j]].sprite = allSprites[chosenGrid[chosenPosition]][j];
            }
                yield return new WaitForSecondsRealtime(0.1f);
        }
        for (int i = 0; i < 4; i++)
        {
            leds[i].GetComponent<MeshRenderer>().material = ledColors[1];
            buttonLabels[i].sprite = allSprites[chosenGrid[chosenPosition]][i];
        }
        yield return new WaitForSecondsRealtime(0.75f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        GetComponent<KMBombModule>().HandlePass();
        moduleSolved = true;
        yield return null;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} press 1234 to press the buttons in those positions.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command)
    {
        string[] parameters = Command.Trim().ToUpper().Split(' ');
        if (parameters.Length == 2 && parameters[0] == "PRESS" && parameters[1].All(x => '1' <= x && x <= '4'))
        {
            yield return null;
            for (int i = 0; i < parameters[1].Length; i++)
            {
                keys[int.Parse(parameters[1][i].ToString()) - 1].OnInteract();
                yield return new WaitForSecondsRealtime(0.1f);
                if (uninteractable)
                    yield return "solve";
            }
        }
        
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        while (!uninteractable)
        {
            keys[pressPositions[stage]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        while (!moduleSolved)
            yield return true;
    }
}
