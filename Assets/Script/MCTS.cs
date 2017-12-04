using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MCTS {
    public interface IGameState
    {
        bool HasMoves { get; }
        bool IsGameOver { get; }
        bool IsDraw { get; }
        int WinningPlayer { get; }
        int NumMovesMade { get; }

        int PlayerTurn { get; }
        IGameState 
            CloneState();
        void 
            AddMove(MoveMgr.Move move);
        bool
            WasMoveMade(MoveMgr.Move move);
        MoveMgr.Move
            GetRandomMove(int playerNum);
        List<MoveMgr.Move> 
            GetAvailableMoves(int playerNum);
        bool
            DoesMoveWin(MoveMgr.Move move);
        float 
            GetResult(MoveMgr.Move move, int playerNumPerspective);
    }

    class Node : IDisposable

    {
        public static int NodeCount { get; private set; }

        const int DRAW = -1;
        public bool IsDraw { get { return DRAW == Winner; } }
        public bool IsTerminal { get { return Winner > 0 || DRAW == Winner; } }
        public int Winner { get; set; }
        public bool HasUntriedMoves { get { return null != _untriedMoves && _untriedMoves.Count>0; } }
        public bool HasChildren { get { return null != _children && _children.Count > 0; } }
        public bool IsBlackListed { get { return -1 == Prioritize; } private set { Prioritize = value ? -1 : 0; } }
        public bool IsWhiteListed { get { return 1 == Prioritize; } private set { Prioritize = value ? 1 : 0; } }
        public int Prioritize { get; private set; } // -1 = blacklisted lose, 0 = no priority, 1 = whitelisted win move

        public int PlayerNum { get; private set; }
        public Node Parent { get; private set; }
        public MoveMgr.Move Move { get; private set; }

        List<MoveMgr.Move> _untriedMoves;
        List<Node> _children;
        int _visits;
        float _probablity; // TODO: This number highly coupled to a given player.  Consider storing as an array for each player.

        public Node(IGameState state, MoveMgr.Move move, Node parent)
        {
            ++NodeCount;
            Parent = parent;
            Move = move;
            PlayerNum = move.PlayerNum;
            if (state.IsDraw)
            {
                Winner = DRAW;
            } else
            {
                Winner = state.WinningPlayer;  // will return 0 if no current winner.
            }
            _untriedMoves = state.GetAvailableMoves(state.PlayerTurn);
            _children = new List<Node>(_untriedMoves.Count);

            _visits = 0;
            _probablity = 0f;
        }
        public int GetDepth()
        {
            int depth = 0;
            Node n = this;
            while(null != n.Parent)
            {
                n = n.Parent;
                ++depth;
            }
            return depth;
        }
        public Node UCTSelectChild()
        {
            Node bestChild = _children[0];
            float bestValue = 0f;
            float utcConstant = 1.75f; //TODO: fiddle with this to weight the unexplored.
            //string str = string.Empty;

            foreach(Node n in _children)
            {
                float winRatio = n._probablity / n._visits;
                float unExploredFactor = utcConstant * Mathf.Sqrt(Mathf.Log((float)_visits) / (float)n._visits);
                float utc = winRatio + unExploredFactor;

                //Debug.LogFormat("winRatio:{0}  unExploredFactor:{1})", winRatio, unExploredFactor);

                //str += "p/v:" + (n._probablity / (float)n._visits) + " utc:" + utc + ", ";
                if (utc > bestValue )// || n.IsWhiteListed)
                {
                    /*
                    if (n.IsBlackListed)
                    {
                        continue;
                    }
                    if (n.IsWhiteListed)
                    {
                        //Debug.LogFormat("Found Whitelisted move({0},{1})", n.Move.Row, n.Move.Col);
                    }
                    */
                    bestValue = utc;
                    bestChild = n;
                }
            };
            //Debug.LogFormat("UTC ({0},{1}) = {2}", bestChild.Move.Row, bestChild.Move.Col, bestValue);
            return bestChild;
        }

        public void BlackListParent()
        {
            if (null == Parent)
            {
                return;
            }

            if (Parent.IsBlackListed || Parent.IsWhiteListed)
            {
                return;
            }
            int depth = GetDepth();
            if (depth < 3)
            {
                Debug.LogFormat("Blacklisted ({0},{1}) because it leads to p1 win next move({2},{3})", Parent.Move.Row, Parent.Move.Col, Move.Row, Move.Col);
                Parent.IsBlackListed = true;
            }
            // if current node has two possible win moves, this is VERY BAD, prop up another move
            int possibleOppWins = 0;
            foreach(Node n in Parent._children)
            {
                if(n.Winner == Winner)
                {
                    ++possibleOppWins;
                }
            }
            if(possibleOppWins > 1)
            {
                if(null != Parent.Parent && null != Parent.Parent.Parent)
                {
                    if (Parent.Parent.Parent.IsBlackListed || Parent.Parent.Parent.IsWhiteListed)
                    {
                        return;
                    }
                    if (Parent.Parent.HasUntriedMoves)
                    {
                        return;
                    }
                    // If has whitelisted winning move before the double loss, then no need to prop blacklist.
                    foreach(Node n in Parent.Parent._children)
                    {
                        if (n.IsWhiteListed)
                        {
                            return;
                        }
                    }
                    Parent.Parent.Parent.IsBlackListed = true;
                    Debug.LogFormat("Propagated Blacklist ({0},{1}) because it leads to {2} p1 win choices at depth {3}", Parent.Parent.Parent.Move.Row, Parent.Parent.Parent.Move.Col, possibleOppWins, depth);
                }
            }
        }
        public void WhiteList()
        {
            if (IsWhiteListed)
            {
                return;
            }
            if(GetDepth() == 1)
            {
                Debug.LogFormat("Whitelisted this move({0},{1})", Move.Row, Move.Col);
            }
            //Debug.LogFormat("Whitelisted({0},{1})", Move.Row, Move.Col);
            IsWhiteListed = true;
        }

        public MoveMgr.Move GetRandomUntriedMove()
        {
            if(null == _untriedMoves || _untriedMoves.Count == 0)
            {
                return null;
            }
            return _untriedMoves[UnityEngine.Random.Range(0, _untriedMoves.Count)];
        }
        public Node AddChild(MoveMgr.Move move, IGameState state)
        {
            Node n = new Node(state, move, this);
            _untriedMoves.Remove(move);
            _children.Add(n);
            return n;
        }

        public void Update(float probablity)
        {
            ++_visits;
            _probablity += probablity;
        }

        public Node GetMostVisitedChild()
        {
            Node popularChild = _children[0];
            int visitCount = _children[0]._visits;

            foreach(Node n in _children)
            {
                Debug.LogFormat("({0},{1}) v:{2} p/v:{3} {4}", n.Move.Row, n.Move.Col, n._visits, (n._probablity / (float)n._visits),n.IsWhiteListed?"WhiteListed":(n.IsBlackListed?"BlackListed":string.Empty));
                if (popularChild.IsWhiteListed || n.IsBlackListed)
                {
                    continue;
                }
                if (n._visits > visitCount || n.IsWhiteListed || popularChild.IsBlackListed)
                {
                    visitCount = n._visits;
                    popularChild = n; 
                }
            }
            return popularChild;
        }

        /// <summary>
        /// Check to see where current state is in tree.  With 2 players, this will be one node past current
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Node AdvanceToCurrentState(IGameState state)
        {
            Node currentNode = null;

            //Check to see which move is on the board
            // Children first
            foreach (Node n in _children)
            {
                if (state.WasMoveMade(n.Move))
                {
                    currentNode = n;
                    break;
                }
            }
            // If not found, try untried moves
            if (null == currentNode)
            {
                foreach(MoveMgr.Move move in _untriedMoves)
                {
                    if (state.WasMoveMade(move))
                    {
                        currentNode = new Node(state, move, null);
                        break;
                    }
                }
            }

            Debug.Assert(null != currentNode, "Can't find current state node in tree");

            currentNode.TrimTreeTop();

            return currentNode;
        }

        /// <summary>
        /// Reclaim Parent of this node plus all it's child trees, except this child tree
        /// </summary>
        public void TrimTreeTop()
        {
            int cnt = NodeCount;

            Parent.DisposeExceptChildTree(this); // Keep this child tree

            Debug.Log("NodeCount: " + cnt + " Trimed: " + (cnt - NodeCount) + " Remaining: " + NodeCount);

            Parent = null;
        }
        public void Dispose()
        {
            if(null != _children)
            {
                foreach (Node n in _children)
                {
                    n.Dispose();
                }
                _children.Clear();
            }
            ReclaimNode();
        }

        /// <summary>
        /// Reclaim Node to ObjectPool
        /// </summary>
        /// <param name="keepChildTree"></param>
        public void DisposeExceptChildTree(Node keepChild)
        {
            foreach (Node n in _children)
            {
                if (n != keepChild)
                {
                    n.Dispose();
                }
            }
            ReclaimNode();
        }

        private void ReclaimNode()
        {
            //todo Reclaim Node to pool
            --NodeCount;
        }
    }

    /// <summary>
    /// Saving tree off.  Be sure to clear between games.
    /// </summary>
    Node _root = null;

    public void ClearState()
    {
        if (null == _root)
        {
            return;
        }
        int cnt = Node.NodeCount;

        _root.Dispose();
        _root = null;

        Debug.Log("ClearState NodeCount: " + cnt + " Trimed: " + (cnt - Node.NodeCount) + " Remaining: " + Node.NodeCount);
    }

    IEnumerator BuildMCTS(IGameState rootState)
    {
        int playerNum = rootState.PlayerTurn;
        yield return null;
    }


    public MoveMgr.Move GetMove(IGameState rootState, int playerNum, int maxIterations = 5000)
    {
        // Tree reuse
        /*
        if (null == _root)
        {
            _root = new Node(rootState, MoveMgr.Move.Fetch(playerNum, 0, 0), null);
        } else
        {
            _root = _root.AdvanceToCurrentState(rootState);
        }
        */

        // Fresh tree each time (clears visits and probablities)
        ClearState();
        _root = new Node(rootState, MoveMgr.Move.Fetch(playerNum, 0, 0), null);



        for (int i=0; i<maxIterations; ++i)
        {
            Node node = _root;  //TODO: may need to recreate tree each time until probablites array and visits array implemented.  Need to save state for each player.
            IGameState state = rootState.CloneState();

            // Select
            while(node.HasUntriedMoves==false && node.HasChildren) // Fully expanded and non-terminal
            {
                node = node.UCTSelectChild();
                state.AddMove(node.Move);
            }

            // Expand
            if (node.HasUntriedMoves)
            {
                MoveMgr.Move move = node.GetRandomUntriedMove();
                state.AddMove(move);
                node = node.AddChild(move, state);  // Add child and descend
            }

            // Simulate (Rollout - not adding nodes to Terminal state )
            while (!state.IsGameOver)
            {
                MoveMgr.Move move = state.GetRandomMove(state.PlayerTurn);
                state.AddMove(move);
            }

            // Prioritize if a win or lose, blacklist/whitelist
            if(node.IsTerminal && !node.IsDraw)
            {
                if (node.Winner != playerNum)
                {
                    // must blacklist parent which is 'this' players move.  
                    //This player cannot lose on their turn, but can play a move that will make them lose on opponents turn.
                    if (null != node.Parent)  
                    {
                        node.BlackListParent();
                    }
                } else
                {
                    node.WhiteList();
                }
            }

            // Backpropagate
            while (null != node)
            {
                node.Update(state.GetResult(node.Move, playerNum));
                node = node.Parent;
            }
        }
        _root = _root.GetMostVisitedChild();

        _root.TrimTreeTop();

        return _root.Move;
    }
}
/*
public class ObjectPool<T>
{
    private static Queue<T> _Pool;
    public static void LoadPool(int count)
    {
        if (null == _Pool)
        {
            _Pool = new Queue<T>(count);
        }
    }
    /// <summary>
    /// Return object to the pool
    /// </summary>
    public void Dispose(T obj)
    {
        if (null == _Pool)
        {
            _Pool = new Queue<T>();
        }
        if (_Pool.Contains(obj))
        {
            Debug.LogAssertion("Double Dispose called.");
            return;
        }
        _Pool.Enqueue(obj);
    }

    /// <summary>
    /// Fetch a recycled object out of the pool and set all new read only values.
    /// </summary>
    /// <returns>T</returns>
    public static T Fetch()
    {
        if (_Pool != null && _Pool.Count > 0)
        {
            return _Pool.Dequeue();
        }
        return default(T);
    }
}


        public class ObjectPool<T>
    {
        // requires assemblies System.Collections.Concurrent.dll, System.dll, netstandard.dll
        // requires using System.Collections.Concurrent;
        //private ConcurrentBag<T> _Objects;
        private Queue<T> _objects;
        private Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
            //_objects = new ConcurrentBag<T>();
            _objects = new Queue<T>();
            _objectGenerator = objectGenerator;
        }

        public T GetObject()
        {
            //if (_objects.TryTake(out item)) return item;
            if(_objects.Count > 0)
            {
                return _objects.Dequeue();
            }
            return _objectGenerator();
        }

        public void PutObject(T item)
        {
            //_objects.Add(item);
            _objects.Enqueue(item);
        }
    };


*/
