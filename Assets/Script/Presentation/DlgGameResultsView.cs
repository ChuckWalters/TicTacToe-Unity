using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using TMPro;

public class DlgGameResultsView : MonoBehaviour {
    [Header("Symbols:")]
    public GameObject Symbol;
    public GameObject Win;
    public GameObject Draw;
    [Header("Animation:")]
    public PlayableDirector DlgResultsDirector;
    public PlayableAsset OpenDlg;
    public PlayableAsset CloseDlg;

    private TextMeshProUGUI SymbolTMP;
    private string SymbolString;
    private Animator _animator;
    private CanvasGroup _canvasGroup;
    private bool _isOpen = false;

    public bool IsOpen
    {
#if USE_ANIMATOR
        get { return _animator.GetBool("IsOpen"); }
#endif
        set
        {
#if USE_ANIMATOR
            _animator.SetBool("IsOpen", value);
#endif
            if (value == _isOpen)
            {
                return;
            }
            _isOpen = value;
            DlgResultsDirector.Play(value ? OpenDlg : CloseDlg);
            _canvasGroup.blocksRaycasts = _canvasGroup.interactable = value;
        }
    }

    // Use this for initialization
    void Awake () {
        SymbolTMP = Symbol.GetComponent<TextMeshProUGUI>();

        //SymbolTMP.SetText(SymbolString);

        _animator = GetComponent<Animator>();
        _canvasGroup = GetComponent<CanvasGroup>();
#if USE_ANIMATOR
        IsOpen = false;
#endif
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void DeActivate()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Configures the Results dialog and activates it.
    /// </summary>
    /// <param name="symbol"></param>
    public void SetWinResults(string symbol)
    {
        SymbolString = symbol;

        if (symbol.Length == 0)
        {
            SetDrawResults();
            IsOpen = true;
            return;
        }

        IsOpen = true;

        Draw.SetActive(false);
        Win.SetActive(true);
        Symbol.SetActive(true);

        if (SymbolTMP)
        {
            SymbolTMP.SetText(SymbolString);
        }
    }

    private void SetDrawResults()
    {
        Draw.SetActive(true);
        Win.SetActive(false);
        Symbol.SetActive(false);
    }
}
