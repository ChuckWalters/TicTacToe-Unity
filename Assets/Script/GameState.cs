using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour, IPlayerMoveObserver
{
    public enum PlayerType
    {
        Human=0,
        AI_Hard_MCTS,
        AI_Hard,
        AI_Medium,
        AI_Easy
    }

    public InfoBarView InfoBar;
    public ActionBarView ActionBar;
    public GameBoardView BoardView;
    public DialogController DlgController;

    public float AiTimeDelay = 1.0f;
    public int NumPlayers = 2;
    public int FirstPlayer = 1;
    public int NumRows = 3;
    public int NumCols = 3;
    public int NumConnectionsToWin = 3;

    public enum PlayState
    {
        Init,
        ShowNewGameDlg,
        Playing,
        ShowResults
    }
    private PlayState _State = PlayState.Init;

    public bool IsHumanTurn { get { return _Players[_BoardState.PlayerTurn - 1] == PlayerType.Human; } }
    public PlayerType CurrentPlayerType { get { return _Players[_BoardState.PlayerTurn - 1]; } }

    private PlayerType[] _Players;
    private BoardState _BoardState;

    private MoveMgr.Move NextAIMove = null;
    private float AiStartMoveTime;
    private MoveMgr.Move _Hint;

    // Use this for initialization
    void Start () {
        MoveMgr.Move.LoadPool(NumRows * NumCols * 5);

        //TODO: Instead of showing New Game dialog, start with A.I. autoplay.
        //ShowNewGameDialog();
    }


    /// <summary>
    /// Update is called once per frame
    /// </summary>
    /// <TODO>Consider centeralizing all UI updates here.</TODO>
    void Update () {
        switch(_State)
        {
            case PlayState.Init: break;
            case PlayState.Playing:
            {
                ExecuteLastAIMove();
                break;
            }
            case PlayState.ShowNewGameDlg: break;
            case PlayState.ShowResults: break;
        }
	}

    private void ExecuteLastAIMove()
    {
        if (null == NextAIMove){
            return;
        }

        if (Time.fixedTime > (AiStartMoveTime + AiTimeDelay))
        {
            MoveMgr.Move tmpMove = NextAIMove;
            NextAIMove = null;
            _BoardState.MakeMove(tmpMove);
        }
    }

    /// <summary>
    /// Visually shows a suggested move for the current active player.
    /// </summary>
    public void ShowHint()
    {
        BoardState.AIMove.GetMove(_BoardState.PlayerTurn, PlayerType.AI_Hard, HintCallback);
    }

    private void HintCallback(MoveMgr.Move move)
    {
        _Hint = move;
        BoardView.SetHint(_Hint.Row, _Hint.Col);
    }

    /// <summary>
    /// Only available on Human turn in a Human vs AI game.
    /// Removes last A.I. Move, then last Human move.
    /// </summary>
    public void UndoMove()
    {
        if (!IsHumanVsAIGame())
        {
            Debug.LogError("Only Human vs AI games allow undo.");
            return;
        }
        if (!IsHumanTurn)
        {
            Debug.LogError("Can only undo on a Human turn.");
            return;
        }

        for(int p=0; p < NumPlayers; p++)
        {
            MoveMgr.Move move = _BoardState.Undo();
            if(null == move)
            {
                return;
            }
            BoardView.SetCell(move.Row, move.Col, string.Empty);
        }
    }

    public void ShowNewGameDialog()
    {
        _State = PlayState.ShowNewGameDlg;

        ActionBar.ShowHintBtn = false;
        ActionBar.ShowNewGameBtn = false;
        ActionBar.ShowUndoBtn = false;

        InfoBar.Clear();

        DlgController.ActivateNewGameDlg(true);
    }

    public void GameOver(int winnerNumber)
    {
        _State = PlayState.ShowResults;

        DlgController.ActivateGameResultsDlg(GetPlayerSymbol(winnerNumber));
    }
    public void ExitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Callback from mouse click on a cell in the Tic-Tac-Toe Grid2D
    /// </summary>
    /// <param name="rowcol">Comma delimited string with row,col of cell clicked</param>
    public void OnClickSpot(string rowcol)
    {
        if (!IsHumanTurn)
        {
            Debug.LogWarning("Not a Human's turn");
            return;
        }

        if(null != _Hint)
        {
            BoardView.ClearHint(_Hint.Row, _Hint.Col);
            _Hint = null;
        }

        // Parse row,col clicked
        string[] rc = rowcol.Split(',');
        int row = System.Convert.ToInt32(rc[0]);
        int col = System.Convert.ToInt32(rc[1]);

        _BoardState.MakeMove(MoveMgr.Move.Fetch(_BoardState.PlayerTurn, row, col));
    }

    /// <summary>
    /// Return a character symbol for player.
    /// </summary>
    /// <TODO>Currently only supports 2 players (X & O).  Add symbol list.</TODO>
    /// <param name="playerNum">1-NumPlayers</param>
    /// <returns>string symbol for player</returns>
    private string GetPlayerSymbol(int playerNum)
    {
        Debug.Assert(playerNum >= 0 && playerNum <= NumPlayers);
        if (0 == playerNum)
        {
            return string.Empty;
        }
        return playerNum == 1 ? "X" : "O";
    }

    /// <summary>
    /// Callback from "play" button on NewGame Dialog
    /// </summary>
    public void OnPlayNewGame()
    {
        _State = PlayState.Playing;

        _Players = new PlayerType[NumPlayers];
        for(int i=0; i< _Players.Length; i++)
        {
            int playerNum = i + 1;
            _Players[i] = (PlayerType)DlgController.GetPlayerType(i);
            InfoBar.SetPlayerName(playerNum, GetPlayerSymbol(playerNum) + " - " + _Players[i].ToString());
        }

        DlgController.ActivateNewGameDlg(false);
        ActionBar.ShowNewGameBtn = true;

        _BoardState = new BoardState(NumRows, NumCols, NumConnectionsToWin, NumPlayers, FirstPlayer);
        _BoardState.Subscribe(this);
        BoardView.Clear();

        UpdatePlayerTurn();
    }

    public void OnRematch()
    {
        _State = PlayState.Playing;

        DlgController.DeactivateGameResultsDlg();

        _BoardState = new BoardState(NumRows, NumCols, NumConnectionsToWin, NumPlayers, FirstPlayer);
        _BoardState.Subscribe(this);
        BoardView.Clear();

        UpdatePlayerTurn();
    }

    /// <summary>
    /// Check if current game is a single human vs any number of ai.
    /// </summary>
    /// <returns>true if only 1 human in the current game.</returns>
    private bool IsHumanVsAIGame()
    {
        int numHumans = 0;
        for(int i=0; i<NumPlayers; i++)
        {
            if(_Players[i] == PlayerType.Human)
            {
                ++numHumans;
            }
        }
        return (1 == numHumans);
    }

    /// <summary>
    /// Handles Turn Change logic.
    /// Visual updates based on active player.
    /// AI moves.
    /// </summary>
    /// <param name="playerNum"></param>
    private void UpdatePlayerTurn()
    {
        InfoBar.SetActivePlayer(_BoardState.PlayerTurn);

        bool isHumanTurn = IsHumanTurn;

        ActionBar.ShowHintBtn = isHumanTurn;
        ActionBar.ShowUndoBtn = isHumanTurn && IsHumanVsAIGame();

        if (!isHumanTurn)
        {
            AiStartMoveTime = Time.fixedTime;

            StartCoroutine( BoardState.AIMove.GetMove(_BoardState.PlayerTurn, CurrentPlayerType, AIMovedCallback));
        }
    }

    private void AIMovedCallback(MoveMgr.Move move)
    {
        NextAIMove = move;
    }

    void IPlayerMoveObserver.Update(MoveMgr.Move move)
    {
        // Visual Update
        BoardView.SetCell(move.Row, move.Col, GetPlayerSymbol(move.PlayerNum));

        // Check for win
        if (_BoardState.IsWinningMove(move))
        {
            GameOver(move.PlayerNum);
            return;
        }

        // Check for Draw
        if (_BoardState.IsBoardFull)
        {
            GameOver(0);
            return;
        }

        UpdatePlayerTurn();
    }
}
