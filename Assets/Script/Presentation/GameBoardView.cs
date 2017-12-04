using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameBoardView : MonoBehaviour {

    public GameObject[] Rows;
    private TextMeshProUGUI[,] GridTxt;
    private Image[,] GridImg;

    // Use this for initialization
    void Start ()
    {
        GridTxt = new TextMeshProUGUI[Rows.Length, Rows[0].transform.childCount];
        GridImg = new Image[Rows.Length, Rows[0].transform.childCount];

        for (int r = 0; r < Rows.Length; r++) {
            GameObject row = Rows[r];
            for(int c=0; c < row.transform.childCount; c++)
            {
                Transform t = row.transform.GetChild(c).GetChild(0);
                TextMeshProUGUI TMP = t.GetComponent<TextMeshProUGUI>();
                GridTxt[r, c] = TMP;

                t = row.transform.GetChild(c);
                GridImg[r, c] = t.GetComponent<Image>();
            }
        }
        Clear();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Clear()
    {
        foreach (TextMeshProUGUI t in GridTxt)
        {
            t.SetText("");
        }
    }

    public void SetHint(int row, int col)
    {
        Color c = GridImg[row, col].color;
        c.a = 1.0f;
        GridImg[row, col].color = c;
    }
    public void ClearHint(int row, int col)
    {
        Color c = GridImg[row, col].color;
        c.a = 0.0f;
        GridImg[row, col].color = c;
    }

    public void SetCell(int row, int col, string symbol)
    {
        GridTxt[row, col].SetText(symbol);
    }

    public void ShowWinMark(int startRow, int startCol, int endRow, int endCol)
    {

    }
}
