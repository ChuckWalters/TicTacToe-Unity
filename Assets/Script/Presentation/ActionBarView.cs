using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionBarView : MonoBehaviour {
    public GameObject HintBtn;
    public GameObject UndoBtn;
    public GameObject NewGameBtn;
    public GameObject ExitBtn;

    public bool ShowHintBtn { set { HintBtn.SetActive(value); } }
    public bool ShowUndoBtn { set { UndoBtn.SetActive(value); } }
    public bool ShowNewGameBtn { set { NewGameBtn.SetActive(value); } }
    public bool ShowExitBtn { set { ExitBtn.SetActive(value); } }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
