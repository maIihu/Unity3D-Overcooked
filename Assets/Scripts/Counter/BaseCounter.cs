using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BaseCounter : MonoBehaviour
{
    [SerializeField] private GameObject selectedVisualGO;
    [SerializeField] private Transform counterTopPoint;
    
    protected Transform CounterTopPoint => counterTopPoint;
    protected SoundManager SoundManagerScript;
    
    protected virtual void Awake()
    {
        //visualGameObject = transform.Find("Selected").gameObject;
        //CounterTopPoint = transform.Find("CounterTopPoint").transform;
    }

    protected virtual void Start()
    {
        Player.Instance.OnSelectedCounterChanged += PlayerChangeSelected;
        SoundManagerScript = SoundManager.Instance;
        Hide();
    }
    
    private void PlayerChangeSelected(object sender, Player.OnSelectedCounterChangedEventArgs e)
    {
        if (e.SelectedCounter == this) Show();
        else Hide();
    }
    private void Show()
    {
        selectedVisualGO.SetActive(true);
    }

    private void Hide()
    {
        selectedVisualGO.SetActive(false);
    }

    public virtual void Interact(Player player)
    {
        //Debug.Log("Interact " + this.name);
    }

    public virtual void InteractAlternate(Player player)
    {
       // Debug.Log("Interact Alternate " + this.name);
    }
}
