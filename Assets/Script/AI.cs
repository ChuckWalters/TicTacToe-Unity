//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class AI
{
    public delegate void AIMoveCallback(MoveMgr.Move move);

    // This is the "Live" board current state which contains a private Grid2D with limited access.
    // Clone the Grid2D from the board when AI needs to manipulate the Grid2D to make determinations.
    private BoardState _Board;
    private MCTS _MCTS;

    public AI(BoardState board)
    {
        Reset(board);
    }

    public void Reset(BoardState board)
    {
        _Board = board;
        if(null == _MCTS)
        {
            _MCTS = new MCTS();
        }
        _MCTS.ClearState();
    }

    /// <summary>
    /// Get list of available moves (Empty Cells).
    /// Fetches memory from the move pool.
    /// </summary>
    /// <param name="playerNum">Player to make the move for</param>
    /// <returns>List<MoveMgr.Move> of available moves</returns>
    private List<MoveMgr.Move> GetPlayableCells(int playerNum)
    {
        List<MoveMgr.Move> moves = new List<MoveMgr.Move>(_Board.NumRows * _Board.NumCols);
        for(int row=0; row< _Board.NumRows; row++)
        {
            for(int col=0; col< _Board.NumCols; col++)
            {
                if (_Board[row, col] == 0)
                {
                    moves.Add(MoveMgr.Move.Fetch(playerNum, row, col));
                }
            }
        }
        return moves;
    }

    private MoveMgr.Move GetWinMove(List<MoveMgr.Move> availableMoves, Grid2D grid)
    {
        foreach(MoveMgr.Move move in availableMoves)
        {
            if (grid.DoesMoveWin(move))
            {
                return move;
            }
        }
        return null;
    }
    /// <summary>
    /// Check if opponent about to win, and block their win
    /// </summary>
    /// <param name="availableMoves">List of available moves</param>
    /// <param name="opponentNums">List of oppenent numbers</param>
    /// <param name="grid">The grid to test against</param>
    /// <returns>A blocking move or null if no wins possible in next play</returns>
    private MoveMgr.Move GetBlockOpponentWinMove(List<MoveMgr.Move> availableMoves, List<int> opponentNums, Grid2D grid)
    {
        foreach(int oppNum in opponentNums)
        {
            foreach (MoveMgr.Move move in availableMoves)
            {
                using (MoveMgr.Move oppMove = MoveMgr.Move.Fetch(oppNum, move))
                {
                    if (grid.DoesMoveWin(oppMove))
                    {
                        return move;
                    }
                }
            }
        }
        return null;
    }

    private MoveMgr.Move GetCenterMove(List<MoveMgr.Move> availableMoves)
    {
        foreach(MoveMgr.Move m in availableMoves)
        {
            if(m.Row==1 && m.Col == 1)
            {
                return m;
            }
        }
        return null;
    }

    /// <summary>
    /// Get list of opponent numbers
    /// </summary>
    /// <param name="playerNum">Self playerNum</param>
    /// <returns>int List of opponent #'s</returns>
    private List<int> GetOpponents(int playerNum)
    {
        List<int> opponents = new List<int>();
        for (int i = 1; i <= _Board.NumPlayers; i++)
        {
            if (i != playerNum)
            {
                opponents.Add(i);
            }
        }
        return opponents;
    }

    private bool DoesPlayerHaveOpposingCorners(int playerNum, Grid2D grid)
    {
        return 
            (grid[0, 0] == playerNum && grid[grid.MaxRowIndex, grid.MaxColIndex] == playerNum) ||
            (grid[grid.MaxRowIndex, 0] == playerNum && grid[0, grid.MaxColIndex] == playerNum);
    }

    private MoveMgr.Move GetAnySideCenterMove(List<MoveMgr.Move> availableMoves)
    {
        foreach (MoveMgr.Move m in availableMoves)
        {
            if (m.Row == 1 ^ m.Col == 1)
            {
                return m;
            }
        }
        return null;
    }


    /// <summary>
    /// Searches for 3 corner win move.  If that doesn't exist, random any corner.
    /// </summary>
    /// <param name="availableMoves">Available moves for the current player</param>
    /// <param name="grid">The grid to test against</param>
    /// <returns>A valid corner move or null if no corner available</returns>
    private MoveMgr.Move Get3CornerMove(List<MoveMgr.Move> availableMoves, Grid2D grid)
    {
        List<MoveMgr.Move> cornerMoves = new List<MoveMgr.Move>(4);

        foreach(MoveMgr.Move m in availableMoves)
        {
            if (grid.IsCorner(m))
            {
                cornerMoves.Add(m);
            }
        }
        if(cornerMoves.Count == 0){
            return null;
        }else if (cornerMoves.Count == 1){
            return cornerMoves[0];
        }else if(cornerMoves.Count < 4)
        {
            // 2nd move, counter via center move
            if (availableMoves.Count == (grid.NumCells - 1))
            {
                MoveMgr.Move bestMove = GetCenterMove(availableMoves);
                if (null != bestMove)
                {
                    return bestMove;
                }
            }
            // 4th move, counter
            if (availableMoves.Count == (grid.NumCells - 1))
            {
                List<int> opponents = GetOpponents(availableMoves[1].PlayerNum);
                if(opponents.Count == 1)
                {
                    if (DoesPlayerHaveOpposingCorners(opponents[0], grid))
                    {
                        MoveMgr.Move bestMove = GetAnySideCenterMove(availableMoves);
                        if (null != bestMove)
                        {
                            return bestMove;
                        }
                    }
                }
            }

            // Find opposing corner move which:
            // 1. Prevents 3 corner win by opponent.
            // 2. Setup for 3 corner win.
            foreach (MoveMgr.Move m in cornerMoves)
            {
                int row = m.Row == 0 ? grid.MaxRowIndex : 0;
                int col = m.Col == 0 ? grid.MaxColIndex : 0;
                if (grid[row, col] != 0) {
                    return m;
                }
            }
        } 

        // Any random corner.  Random to prevent same play each game
        int index = UnityEngine.Random.Range(0, cornerMoves.Count);
        return cornerMoves[index];
    }

    private MoveMgr.Move BestMove(int playerNum, List<MoveMgr.Move> availableMoves)
    {
        Grid2D grid = _Board.CloneGrid();
        MoveMgr.Move bestMove = null;

        bestMove = GetWinMove(availableMoves, grid);
        if (null != bestMove)
        {
            return bestMove;
        }

        bestMove = GetBlockOpponentWinMove(availableMoves, GetOpponents(playerNum), grid);
        if (null != bestMove)
        {
            return bestMove;
        }

        bestMove = Get3CornerMove(availableMoves, grid);
        if (null != bestMove)
        {
            return bestMove;
        }

        return RandomMove(playerNum, availableMoves);
    }

    /// <summary>
    /// Try to eliminate BestMoves from availableMoves.  If only good moves exist, return the lesser good.
    /// </summary>
    /// <param name="playerNum"></param>
    /// <param name="availableMoves"></param>
    /// <returns></returns>
    private MoveMgr.Move WorstMove(int playerNum, List<MoveMgr.Move> availableMoves)
    {
        Grid2D grid = _Board.CloneGrid();
        MoveMgr.Move bestMove = null;

        // Skip all winning moves
        do
        {
            if (availableMoves.Count == 1)
            {
                return availableMoves[0];
            } 
            else if (null != bestMove)
            {
                availableMoves.Remove(bestMove);
                bestMove.Dispose();
            }
            bestMove = GetWinMove(availableMoves, grid);
        }
        while (null != bestMove);

        // Skip all blocking moves
        List<int> opponents = GetOpponents(playerNum);
        do
        {
            if (availableMoves.Count == 1)
            {
                return availableMoves[0];
            } 
            else if (null != bestMove)
            {
                availableMoves.Remove(bestMove);
                bestMove.Dispose();
            }
            bestMove = GetBlockOpponentWinMove(availableMoves, opponents, grid);
        }
        while (null != bestMove);

        // Skip all 3 corner
        do
        {
            if (availableMoves.Count == 1)
            {
                return availableMoves[0];
            } else if (null != bestMove)
            {
                availableMoves.Remove(bestMove);
                bestMove.Dispose();
            }
            bestMove = Get3CornerMove(availableMoves, grid);
        }
        while (null != bestMove);

        return RandomMove(playerNum, availableMoves);
    }

    private MoveMgr.Move RandomMove(int playerNum, List<MoveMgr.Move> availableMoves)
    {
        int index = UnityEngine.Random.Range(0, availableMoves.Count);
        return availableMoves[index];
    }

    private MoveMgr.Move BestMove_MCTS(int playerNum, List<MoveMgr.Move> availableMoves)
    {
        Grid2D grid = _Board.CloneGrid();

        MoveMgr.Move bestMove = _MCTS.GetMove(grid, playerNum);
        return bestMove;
    }

    const float Win = 1.0f;
    const float Lose = 0f;
    const float Draw = 0.5f;

    private float GetProbability(int playerNum, MoveMgr.Move move, List<MoveMgr.Move> availableMoves, Grid2D grid)
    {
        // TODO design a "Grid2D" class that allows board to be manipulated without side effects (notifing others and yada)
        if (grid.DoesMoveWin(move))
        {
            return Win;
        }
        if(availableMoves.Count == 0)
        {
            return Draw;
        }

        // Apply move to local grid copy.  New grid class should have grid.Add(move) and grid.Remove(move)
        grid.AddMove(move);

        // TODO: apply opponent moves from available moves
                // Recurse GetProbabablity() for each opponent move

        float probablity = 0f;

        for (int i = 0; i < availableMoves.Count-1; i++)
        {
            List<MoveMgr.Move> nextMoves = availableMoves.GetRange(0, availableMoves.Count);
            nextMoves.Remove(move);
           // probablity += GetOpponetProbability(opponentNum, availableMoves[i], nextMoves, grid);
        }
        return probablity / (float)(availableMoves.Count-1);
    }

    private void AIMoved(MoveMgr.Move move, List<MoveMgr.Move> availableMoves, AIMoveCallback callback)
    {
        try
        {
            Debug.Assert(null != move);
            availableMoves.Remove(move);
            callback(move);
        }
        finally
        {
            // Release to pool
            availableMoves.ForEach(delegate (MoveMgr.Move m)
            {
                m.Dispose();
            });
        }

    }

    public IEnumerator GetMove(int playerNum, GameState.PlayerType playerType, AIMoveCallback callback)
    {
        MoveMgr.Move bestMove = null;
        List<MoveMgr.Move> availableMoves = GetPlayableCells(playerNum);

        yield return null;

        switch (playerType)
        {
            case GameState.PlayerType.AI_Easy:
            {
                bestMove = WorstMove(playerNum, availableMoves);
                break;
            }
            case GameState.PlayerType.AI_Medium:
            {
                bestMove = RandomMove(playerNum, availableMoves);
                break;
            }
            case GameState.PlayerType.AI_Hard:
            {
                bestMove = BestMove(playerNum, availableMoves);
                break;
            }
            case GameState.PlayerType.AI_Hard_MCTS:
            {
                bestMove = BestMove_MCTS(playerNum, availableMoves);
                break;
            }
            default:
            {
                Debug.LogAssertion("Unhandled AI type: " + playerType.ToString());
                break;
            }
        }

        AIMoved(bestMove, availableMoves, callback);
    }

}
