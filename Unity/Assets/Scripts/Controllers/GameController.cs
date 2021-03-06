﻿using UnityEngine;

// Controller for managing in-game data and events
public class GameController : MonoBehaviour
{

    // Scoring variables, globally readable but only settable here
    [HideInInspector]
    public int highscore { get; private set; }
    [HideInInspector]
    public int score { get; private set; }
    [HideInInspector]
    public int level { get; private set; }
    [HideInInspector]
    public int lines { get; private set; }

    // Constant array of numbers for scoring
    private int[] linescores = { 0, 40, 100, 300, 1200 };

    // Reference to the current hexel's GameObject
    private GameObject currenthexel;

    // Reference to the currently active HexelController
    private HexelController hexelcontroller;

    // Reference to the next hexel preview's GameObject
    private GameObject hexelpreview;

    // RNG for spawning hexels
    private static System.Random random = new System.Random();

    // Variables for maintaining the state engine
    private string gamestate;
    private bool[] rows = new bool[15];
    private int timer = 0;
    private int counter = 0;
    private int hexelcolor = 0;
    private int nexthexel = 0;
    private int gameovercount = 0;
    private int previouslevel = 0;
    private int originalhighscore = 0;
    private bool newhighscore = false;

    void Awake()
    {

        gamestate = "stopped";

    }

    void Update()
    {

        if (Input.GetButtonDown("Back"))
        {

            GameManager.savecontroller.SaveGame(new GameSaveDataObject(
                gamestate, score, level, lines, rows, hexelcolor, nexthexel, gameovercount,
                previouslevel, originalhighscore, newhighscore,
                GameManager.boardcontroller.GetGrid()));

            gamestate = "stopped";

            Application.LoadLevel("Pause");

        }

        switch (gamestate)
        {

            case "stopped":

                break;

            case "start":

                // Retrieve the highscore data object
                HighScoreDataObject highscoredata = GameManager.savecontroller.LoadHighScores();

                originalhighscore = highscoredata.highscore;

                highscore = originalhighscore;

                hexelcolor = random.Next(0, GameManager.hexelprefabs.Length);

                score = 0;
                level = 0;
                lines = 0;

                gamestate = "spawnhexel";

                break;

            case "load":

                gamestate = "opensave";

                break;

            case "opensave":

                // Retrieve the save data object
                GameSaveDataObject savedata = GameManager.savecontroller.LoadGame();

                // Unpack the object
                gamestate = savedata.gamestate;
                score = savedata.score;
                level = savedata.level;
                rows = savedata.rows;
                hexelcolor = savedata.hexelcolor;
                nexthexel = savedata.nexthexel;
                gameovercount = savedata.gameovercount;
                previouslevel = savedata.previouslevel;
                originalhighscore = savedata.originalhighscore;
                newhighscore = savedata.newhighscore;
                GameManager.boardcontroller.LoadGrid(savedata.grid);

                // Retrieve the highscore data object
                HighScoreDataObject loadhighscoredata = GameManager.savecontroller.LoadHighScores();

                highscore = loadhighscoredata.highscore;

                switch (gamestate)
                {

                    case "running":

                        currenthexel = Instantiate(GameManager.hexelprefabs[hexelcolor], new
                            Vector3(0, 0), Quaternion.identity) as GameObject;

                        hexelcontroller = currenthexel.GetComponent<HexelController>();

                        break;

                    case "lineblink":

                        timer = 0;
                        counter = 0;

                        break;

                    case "waitfordrop":

                        timer = 0;

                        break;

                    case "fill":

                        counter = 0;
                        rows = new bool[20];

                        break;

                    case "wipe":

                        rows = new bool[20];
                        counter = 0;

                        break;

                    default:
                        break;

                }

                UIController.UpdateUI(highscore, score, lines, level, nexthexel);

                break;

            case "spawnhexel":

                hexelcolor = nexthexel;
                nexthexel = random.Next(0, GameManager.hexelprefabs.Length);

                currenthexel = Instantiate(GameManager.hexelprefabs[hexelcolor], new
                    Vector3(0, 0), Quaternion.identity) as GameObject;

                hexelcontroller = currenthexel.GetComponent<HexelController>();

                UIController.UpdateUI(highscore, score, lines, level, nexthexel);

                if (score >= originalhighscore && !newhighscore)
                {

                    GameManager.soundcontroller.PlaySound(9);

                    newhighscore = true;

                }

                gamestate = "running";

                break;

            case "running":

                break;

            case "linecheck":

                score += hexelcontroller.softdrops;

                Destroy(currenthexel);
                currenthexel = null;
                hexelcontroller = null;

                rows = GameManager.rowcontroller.CheckForRows();

                int numrows = 0;

                for (int row = 0; row < 15; row++)
                {

                    if (rows[row])
                        numrows++;

                }

                previouslevel = level;

                score += linescores[numrows] * (level + 1);
                lines += numrows;
                level = lines / 10;

                if (level > previouslevel)
                    GameManager.soundcontroller.PlaySound(8);

                if (score > highscore)
                {

                    highscore = score;

                    GameManager.savecontroller.SaveHighScores(new HighScoreDataObject(highscore));

                }

                if (numrows != 0)
                {

                    GameManager.soundcontroller.PlaySound(0);

                    timer = 0;
                    counter = 0;
                    gamestate = "lineblink";

                }
                else
                {

                    GameManager.soundcontroller.PlaySound(1);

                    gamestate = "spawnhexel";

                }

                break;

            case "lineblink":

                if (counter == 2)
                {

                    gamestate = "lineclear";

                }
                else
                {

                    if (timer == 0)
                        GameManager.rowcontroller.HideRows(rows);

                    if (timer == 25)
                        GameManager.rowcontroller.ShowRows(rows);

                    if (timer == 50)
                    {

                        timer = 0;
                        counter++;

                    }
                    else
                    {

                        timer++;

                    }

                }

                break;

            case "lineclear":

                GameManager.rowcontroller.ClearRows(rows);

                timer = 0;
                gamestate = "waitfordrop";

                break;

            case "waitfordrop":

                timer++;

                if (timer == 60)
                    gamestate = "droplines";

                break;

            case "droplines":

                GameManager.soundcontroller.PlaySound(2);

                GameManager.rowcontroller.DropRows(rows);

                gamestate = "spawnhexel";

                break;

            case "gameover":
                
                Destroy(currenthexel);
                currenthexel = null;
                hexelcontroller = null;

                counter = 0;
                rows = new bool[20];

                gamestate = "fill";

                break;

            case "fill":

                GameManager.soundcontroller.PlaySound(7);

                rows[counter] = true;

                counter++;

                GameManager.rowcontroller.FillRows(rows, 0);

                if (counter >= 20)
                {

                    rows = new bool[20];
                    counter = 0;

                    gamestate = "wipe";

                }

                break;

            case "wipe":

                rows[counter] = true;

                counter++;

                GameManager.rowcontroller.ClearRows(rows);

                if (counter >= 20)
                {

                    counter = 0;

                    gamestate = "end";

                }

                break;

            case "end":

                GameManager.savecontroller.DeleteSave();

                gamestate = "stopped";

                Application.LoadLevel("GameOver");

                break;

            default:
                break;

        }

    }

    void OnDisable()
    {

        if (gamestate != "stopped")
        {

            GameManager.savecontroller.SaveGame(new GameSaveDataObject(
                 gamestate, score, level, lines, rows, hexelcolor, nexthexel, gameovercount,
                 previouslevel, originalhighscore, newhighscore,
                 GameManager.boardcontroller.GetGrid()));

        }

    }

    public void NewGame()
    {

        gamestate = "start";

    }

    public void LoadGame()
    {

        gamestate = "load";

    }

    // Callback triggered by HexelController when the hexel sets
    // Passed true if the hexel did not move, indicating a gameover
    public void SetHexel(bool fullgrid)
    {

        gamestate = "linecheck";

        if (fullgrid)
        {

            gameovercount++;

            if (gameovercount >= 2)
                gamestate = "gameover";

        }

    }

}
