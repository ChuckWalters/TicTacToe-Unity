using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InfoBarView : MonoBehaviour {
    public Color ActiveColor;
    public Color WaitingColor;

    [System.Serializable]
    public class PlayerTurn
    {
        public RawImage Bkg;
        public TextMeshProUGUI Name;

    }
    public PlayerTurn[] Players;

    // Use this for initialization
    void Start () {
        Clear();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// Clears the Display but preserves any stored player name/order.
    /// </summary>
    public void Clear()
    {
        foreach(PlayerTurn p in Players)
        {
            p.Bkg.color = WaitingColor;
            p.Name.SetText("");
        }
    }

    /// <summary>
    /// Display which player's turn it is.
    /// </summary>
    /// <TODO>Animate Turn switch</TODO>
    /// <param name="playerNum">Active player number. 1-NumPlayers</param>
    public void SetActivePlayer(int playerNum)
    {
        for (int i=0; i<Players.Length; i++)
        {
            Players[i].Bkg.color = i==(playerNum-1) ?ActiveColor:WaitingColor;
        }
        
    }
    /// <summary>
    /// Store the name and order of each player.
    /// </summary>
    /// <param name="playerNum">1-NumPlayers</param>
    /// <param name="name">Display name in turn indicator</param>
    public void SetPlayerName(int playerNum, string name)
    {
        Players[playerNum-1].Name.SetText(name);
    }
}
