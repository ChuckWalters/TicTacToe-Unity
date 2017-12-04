using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DlgNewGameView : MonoBehaviour {
    public TMP_Dropdown[] DropDowns;
    private Animator _animator;
    private CanvasGroup _canvasGroup;

    public bool IsOpen
    {
        get { return _animator.GetBool("IsOpen"); }
        set
        {
            _animator.SetBool("IsOpen", value);
            _canvasGroup.blocksRaycasts = _canvasGroup.interactable = value;
        }
    }

    // Use this for initialization
    void Awake () {
        _animator = GetComponent<Animator>();
        _canvasGroup = GetComponent<CanvasGroup>();
        IsOpen = true;

    }

    // Update is called once per frame
    void Update () {
		
	}
    public int GetPlayerType(int playerNum)
    {
        return DropDowns[playerNum].value;
    }

    public void Activate(bool setActive)
    {
        IsOpen = setActive;
    }
}
