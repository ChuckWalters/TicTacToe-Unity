using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 
/// </summary>
public class BoardState : MoveMgr
{
    public int NumConnectionsToWin { get; private set; }
    public bool IsBoardFull { get { return MoveCount == (NumRows * NumCols); } }
    public int RemainingMoveCount { get { return (NumRows * NumCols) - MoveCount; } }

    /// <summary>
    /// Stores which player owns which cell
    /// </summary>
    private Grid2D _LiveGrid;

    public static AI AIMove { get; private set; }

    /// <summary>
    /// Get the playerNum of a grid cell
    /// </summary>
    /// <param name="row">0-(NumRows-1)</param>
    /// <param name="col">0-(NumCols-1)</param>
    /// <returns></returns>
    public int this[int row, int col]
    {
        get { return _LiveGrid[row, col]; }        
    }
    public int GetCellOwner(MoveMgr.Move move)
    {
        return _LiveGrid[move.Row, move.Col];
    }

    public Grid2D CloneGrid()
    {
        return _LiveGrid.Clone();
    }
    /// <summary>
    /// Setup a new empty BoardState.
    /// </summary>
    /// <param name="numRows">1+</param>
    /// <param name="numCols">1+</param>
    /// <param name="numConnectionsToWin">2+</param>
    /// <param name="numPlayers">2+</param>
    /// <param name="firstPlayer">number of first player (1-numPlayers)</param>
    public BoardState(int numRows, int numCols, int numConnectionsToWin, int numPlayers, int firstPlayer) : base(numRows, numCols, numPlayers, firstPlayer)
    {
        Debug.Assert(numConnectionsToWin >= 2);
        NumConnectionsToWin = numConnectionsToWin;

        Clear();

    }

    public override void Clear()
    {
        base.Clear();

        if(null == AIMove)
        {
            AIMove = new AI(this);
        } else
        {
            AIMove.Reset(this);
        }

        _LiveGrid = new Grid2D(NumRows, NumCols, NumConnectionsToWin, NumPlayers);
    }

    public bool IsCellOpen(int row, int col)
    {
        return _LiveGrid.IsCellOpen(row, col);
    }

    public bool IsWinningMove(MoveMgr.Move move)
    {
        return _LiveGrid.DoesMoveWin(move);
    }
    /// <summary>
    /// Checks if move is valid in current state.
    /// </summary>
    /// <param name="playerNum">1-NumPlayers</param>
    /// <param name="row">0-(NumRows-1)</param>
    /// <param name="col">0-(NumCols-1)</param>
    /// <returns>true if move is valid</returns>
    public bool CanMove(MoveMgr.Move move)
    {
        if (PlayerTurn != move.PlayerNum)
        {
            Debug.LogError("player: " + move.PlayerNum + " tried to move but it's #" + PlayerTurn + "'s turn.");
            return false;
        }
        if (!IsCellOpen(move.Row, move.Col))
        {
            Debug.LogError("Tried to play in an occupied cell (" + move.Row + "," + move.Col + ").");
            return false;
        }
        return true;
    }
    /// <summary>
    /// Records a move if valid.
    /// </summary>
    /// <param name="move">1-NumPlayers</param>
    /// <remarks>Use CanMove to determine if MakeMove will record the move</remarks>
    public override void MakeMove(MoveMgr.Move move)
    {
        if (!CanMove(move))
        {
            Debug.LogWarning("Invalid move, not recording move.");
            return;
        }
        _LiveGrid.AddMove(move);

        base.MakeMove(move);
    }

    /// <summary>
    /// Records a move if valid.
    /// </summary>
    /// <param name="playerNum">1-NumPlayers</param>
    /// <param name="row">0-(NumRows-1)</param>
    /// <param name="col">0-(NumCols-1)</param>
    /// <remarks>Use CanMove to determine if MakeMove will record the move</remarks>
    /// <TODO>Depricate this method and pass "MoveMgr.Move" obj's around.</TODO>
    public void MakeMove(int playerNum, int row, int col)
    {
        MakeMove(MoveMgr.Move.Fetch(playerNum, row, col));
    }

    public override Move Undo()
    {
        Move move = base.Undo();
        _LiveGrid.ClearCell(move);
        return move;
    }

}
