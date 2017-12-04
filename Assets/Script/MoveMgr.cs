using System;
using System.Collections.Generic;
using UnityEngine;


public class MoveMgr : PlayerMoveSubject
{
    public class Move : IDisposable
    {
        public int PlayerNum { get; private set; }
        public int Row { get; private set; }
        public int Col { get; private set; }

        public Move(int playerNum, int row, int col)
        {
            PlayerNum = playerNum;
            Row = row;
            Col = col;
        }
        public Move(int playerNum, Move move)
        {
            PlayerNum = playerNum;
            Row = move.Row;
            Col = move.Col;
        }
        private static Queue<Move> _Pool;
        public static void LoadPool(int count)
        {
            if (null == _Pool)
            {
                _Pool = new Queue<Move>(count);
            }
        }
        /// <summary>
        /// Return object to the pool
        /// </summary>
        public void Dispose()
        {
            if (null == _Pool)
            {
                _Pool = new Queue<Move>();
            }
            if (_Pool.Contains(this))
            {
                Debug.LogAssertion("Double Dispose called.");
                return;
            }
            _Pool.Enqueue(this);
        }

        /// <summary>
        /// Fetch a recycled object out of the pool and set all new read only values.
        /// </summary>
        /// <param name="playerNum"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static Move Fetch(int playerNum, int row, int col)
        {
            if (_Pool != null && _Pool.Count > 0)
            {
                Move obj = _Pool.Dequeue();
                obj.PlayerNum = playerNum;
                obj.Row = row;
                obj.Col = col;
                return obj;
            }
            return new Move(playerNum, row, col);
        }

        /// <summary>
        /// Fetch a recycled object out of the pool and set all new read only values.
        /// </summary>
        /// <param name="playerNum">Overrides the playerNum in move</param>
        /// <param name="move"></param>
        /// <returns></returns>
        public static Move Fetch(int playerNum, Move move)
        {
            if (_Pool != null && _Pool.Count > 0)
            {
                Move obj = _Pool.Dequeue();
                obj.PlayerNum = playerNum;
                obj.Row = move.Row;
                obj.Col = move.Col;
                return obj;
            }
            return new Move(playerNum, move);
        }
    }

    public int NumPlayers { get; private set; }
    public int NumRows { get; private set; }
    public int NumCols { get; private set; }

    public int MoveCount { get { return MoveStack.Count; } }

    private int FirstPlayer { get; set; }

    /// <summary>
    /// Get the active player number (1-NumPlayers)
    /// </summary>
    public int PlayerTurn { get; private set; }
    private Stack<Move> MoveStack;

    public MoveMgr(int numRows, int numCols, int numPlayers, int firstPlayer)
    {
        Debug.Assert(numRows > 0);
        Debug.Assert(numCols > 0);
        Debug.Assert(numPlayers > 1);
        Debug.Assert(firstPlayer > 0);

        NumRows = numRows;
        NumCols = numCols;
        NumPlayers = numPlayers;
        FirstPlayer = firstPlayer;
        MoveStack = new Stack<Move>(NumRows * NumCols);
    }

    public virtual void Clear()
    {
        PlayerTurn = FirstPlayer;
        MoveStack.Clear();
    }

    /// <summary>
    /// Records a move.
    /// </summary>
    /// <param name="move">Encapsulated move info</param>
    public virtual void MakeMove(Move move)
    {
        IncPlayerTurn();

        MoveStack.Push(move);
        Notify(move);
    }

    /// <summary>
    /// Undo last move from BoardState.
    /// </summary>
    /// <returns>The Move that was undone, or null if no moves exist.</returns>
    public virtual Move Undo()
    {
        if (MoveStack.Count == 0)
        {
            return null;
        }

        DecPlayerTurn();

        return MoveStack.Pop();
    }

    /// <summary>
    /// Peek at the last move.
    /// </summary>
    /// <returns>The last Move played.</returns>
    public Move PeekLastMove()
    {
        if (MoveStack.Count == 0)
        {
            return null;
        }
        return MoveStack.Peek();
    }



    private void IncPlayerTurn()
    {
        PlayerTurn = (NumPlayers == PlayerTurn) ? 1 : PlayerTurn + 1;
    }
    private void DecPlayerTurn()
    {
        PlayerTurn = 1 == PlayerTurn ? NumPlayers : PlayerTurn - 1;
    }

}

#region Player Move Observer

/// <summary>
/// Observer pattern for move made.
/// </summary>
public class PlayerMoveSubject
{
    private List<IPlayerMoveObserver> _observers = new List<IPlayerMoveObserver>(1);

    public void Subscribe(IPlayerMoveObserver observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void Unsubscribe(IPlayerMoveObserver observer)
    {
        _observers.Remove(observer);
    }

    protected void Notify(BoardState.Move move)
    {
        _observers.ForEach(x => x.Update(move));
    }
}

/// <summary>
/// Callback interface for PlayerMoveSubject subscribers.
/// </summary>
public interface IPlayerMoveObserver
{
    /// <summary>
    /// A player has moved
    /// </summary>
    /// <param name="move">Last move made</param>
    void Update(BoardState.Move move);
}

#endregion