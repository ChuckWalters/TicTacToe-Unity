using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogController : MonoBehaviour {

    public DlgNewGameView DlgNewGame;
    public DlgGameResultsView DlgResults;


    // Use this for initialization
    void Awake () {
	}
	
	// Update is called once per frame
	void Update () {
    }

    public int GetPlayerType(int playerNum)
    {
        return DlgNewGame.GetPlayerType(playerNum);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="winningSymbol">"X", "O", or ""</param>
    public void ActivateGameResultsDlg(string winningSymbol)
    {
        Debug.Assert(winningSymbol == "X" || winningSymbol == "O" || winningSymbol.Length == 0);

        DlgResults.SetWinResults(winningSymbol);
    }
    public void DeactivateGameResultsDlg()
    {
        DlgResults.DeActivate();
    }

    public void ActivateNewGameDlg(bool show)
    {
        DeactivateGameResultsDlg();
        DlgNewGame.Activate(show);
    }
}
