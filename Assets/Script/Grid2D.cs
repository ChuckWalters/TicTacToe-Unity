using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid2D: MCTS.IGameState
{
    public enum PlayState
    {
        Draw = -1,
        InProgress = 0,
        p1Wins = 1,
        p2Wins = 2,
        p3Wins = 3,
        p4Wins = 4
    }
    public PlayState GameState { get; private set; }
    public bool IsGameOver { get { return PlayState.InProgress != GameState; } }
    public bool IsDraw { get { return PlayState.Draw == GameState; } }
    public int WinningPlayer { get { return (int)GameState; } }
    public int NumMovesMade { get; private set; }

    public int NumRows { get; private set; }
    public int NumCols { get; private set; }
    public int NumConnectionsToWin { get; private set; }
    public int NumCells { get { return NumRows * NumCols; } }

    public int MaxRowIndex { get { return NumRows - 1; } }
    public int MaxColIndex { get { return NumCols - 1; } }

    public int NumPlayers { get; private set; }
    public int LastPlayerTurn { get; private set; }
    public int PlayerTurn { get{ return (LastPlayerTurn + 1) > NumPlayers ? 1 : (LastPlayerTurn + 1); } }

    public bool HasMoves {
        get{
            if(PlayState.InProgress != GameState)
            {
                return false;
            }
            //TODO: keep move count and track win state!!!
            for (int row = 0; row < NumRows; row++)
            {
                for (int col = 0; col < NumCols; col++)
                {
                    if (_grid[row, col] == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }


    private int[,] _grid;

    public int this[int row, int col]
    {
        get { return _grid[row, col]; }
    }

    public Grid2D(int rows, int cols, int connectionsToWin, int numPlayers)
    {
        NumRows = rows;
        NumCols = cols;
        NumConnectionsToWin = connectionsToWin;
        NumPlayers = numPlayers;

        _grid = new int[NumRows, NumCols];
    }

    public MCTS.IGameState CloneState()
    {
        return Clone();
    }

    public Grid2D Clone()
    {
        Grid2D clone = (Grid2D)base.MemberwiseClone();
        clone._grid = (int[,])_grid.Clone();
        return clone;
    }

    public bool WasMoveMade(MoveMgr.Move move)
    {
        return _grid[move.Row, move.Col] == move.PlayerNum;
    }

    public bool IsCellOpen(MoveMgr.Move move)
    {
        return _grid[move.Row, move.Col] == 0;
    }
    public bool IsCellOpen(int row, int col)
    {
        return _grid[row, col] == 0;
    }

    /// <summary>
    /// Check if move is in a corner.
    /// </summary>
    /// <param name="move">A move to check</param>
    /// <returns>true if move is in any corner</returns>
    public bool IsCorner(MoveMgr.Move move)
    {
        return
            (move.Row == 0 && move.Col == 0) ||
            (move.Row == 0 && move.Col == (NumCols - 1)) ||
            (move.Row == (NumRows - 1) && move.Col == 0) ||
            (move.Row == (NumRows - 1) && move.Col == (NumCols - 1));
    }

    public void AddMove(MoveMgr.Move move)
    {
        if(move.PlayerNum == 0)
        {
            ClearCell(move);
            return;
        }

        _grid[move.Row, move.Col] = move.PlayerNum;
        LastPlayerTurn = move.PlayerNum;
        ++NumMovesMade;

        if (DoesMoveWin(move))
        {
            GameState = (PlayState)move.PlayerNum;
        }else if (!HasMoves)
        {
            GameState = PlayState.Draw;
        }

        return;
    }

    public float GetResult(MoveMgr.Move move, int playerNumPerspective)
    {
        Debug.Assert(IsGameOver);

        if (IsDraw)
        {
            return 0.75f;
        }
        return WinningPlayer == playerNumPerspective ? 1.0f : 0.0f;
    }

    public List<MoveMgr.Move> GetAvailableMoves(int playerNum)
    {
        List<MoveMgr.Move> moves = new List<MoveMgr.Move>(NumRows * NumCols);
        for (int row = 0; row < NumRows; row++)
        {
            for (int col = 0; col < NumCols; col++)
            {
                if (_grid[row, col] == 0)
                {
                    moves.Add(MoveMgr.Move.Fetch(playerNum, row, col));
                }
            }
        }
        return moves;
    }

    public MoveMgr.Move GetRandomMove(int playerNum) {
        List<MoveMgr.Move> moves = GetAvailableMoves(playerNum);
        return moves[Random.Range(0, moves.Count)];
    }

    /// <summary>
    /// Undo a move and rollback state
    /// </summary>
    /// <param name="move">Only check row,col and sets to empty</param>
    public void ClearCell(MoveMgr.Move move)
    {
        _grid[move.Row, move.Col] = 0;

        --NumMovesMade;
        GameState = PlayState.InProgress;
        LastPlayerTurn = LastPlayerTurn <= 1 ? NumPlayers : LastPlayerTurn - 1;
    }

    public bool HasHorzWin(MoveMgr.Move move)
    {
        int maxConnected = 0;
        int counter = 0;
        for (int col = 0; col < NumCols; col++)
        {
            if (move.PlayerNum == _grid[move.Row, col])
            {
                ++counter;
                if (counter > maxConnected)
                {
                    maxConnected = counter;
                }
            } else
            {
                counter = 0;
            }
        }
        return maxConnected >= NumConnectionsToWin;
    }

    public bool HasVertWin(MoveMgr.Move move)
    {
        int maxConnected = 0;
        int counter = 0;
        for (int row = 0; row < NumRows; row++)
        {
            if (move.PlayerNum == _grid[row, move.Col])
            {
                ++counter;
                if (counter > maxConnected)
                {
                    maxConnected = counter;
                }
            } else
            {
                counter = 0;
            }
        }
        return maxConnected >= NumConnectionsToWin;
    }

    /// <summary>
    /// Checks for diagonal win Rising from left to right.
    /// </summary>
    /// <param name="move">Last move made</param>
    /// <returns>true if move wins</returns>
    /// <remarks>Works with any size grid</remarks>
    public bool HasDiagWinFromCell_Rise(MoveMgr.Move move)
    {
        int counter = 1;

        // Up
        for (int r = move.Row - 1, c = move.Col + 1; 
            r >= 0 && c < NumCols; 
            r--, c++)
        {
            if (move.PlayerNum != _grid[r, c])
            {
                break;
            }
            ++counter;
        }
        // Down
        for (int r = move.Row + 1, c = move.Col - 1; 
            r < NumRows && c >= 0; 
            r++, c--)
        {
            if (move.PlayerNum != _grid[r, c])
            {
                break;
            }
            ++counter;
        }

        return counter >= NumConnectionsToWin;
    }
    /// <summary>
    /// Checks for diagonal win Falling from left to right.
    /// </summary>
    /// <param name="move">Last move made</param>
    /// <returns>true if move wins</returns>
    /// <remarks>Works with any size grid</remarks>
    public bool HasDiagWinFromCell_Fall(MoveMgr.Move move)
    {
        int counter = 1;

        // Up
        for (int r = move.Row - 1, c = move.Col - 1; r >= 0 && c >= 0; r--, c--)
        {
            if (move.PlayerNum != _grid[r, c])
            {
                break;
            }
            ++counter;
        }
        // Down
        for (int r = move.Row + 1, c = move.Col + 1; r < NumRows && c < NumCols; r++, c++)
        {
            if (move.PlayerNum != _grid[r, c])
            {
                break;
            }
            ++counter;
        }

        return counter >= NumConnectionsToWin;
    }

    public bool HasDiagWinFromCell(MoveMgr.Move move)
    {
        return HasDiagWinFromCell_Rise(move) || HasDiagWinFromCell_Fall(move);
    }

    public bool DoesMoveWin(MoveMgr.Move move)
    {
        bool wasTmpValuePushed = false;
        if (IsCellOpen(move))
        {
            AddMove(move);
            wasTmpValuePushed = true;
        }

        bool isWin =
            HasHorzWin(move) ||
            HasVertWin(move) ||
            HasDiagWinFromCell(move);

        if (wasTmpValuePushed)
        {
            ClearCell(move);
        }
        return isWin;
    }

}
