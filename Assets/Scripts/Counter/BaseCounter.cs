using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BaseCounter : MonoBehaviour
{
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private GameObject selectedCounter;
    
    protected Transform CounterTopPoint => counterTopPoint;
    protected SoundManager SoundManagerScript;
    
    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {
        SoundManagerScript = SoundManager.Instance;
        Hide();
    }
    
    public void Show()
    {
        selectedCounter.SetActive(true);
    }

    public void Hide()
    {
        selectedCounter.SetActive(false);
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
