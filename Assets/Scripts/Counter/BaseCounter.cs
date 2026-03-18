using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BaseCounter : MonoBehaviour
{
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
        SoundManagerScript = SoundManager.Instance;
        Hide();
    }
    
    private void Show()
    {
    }

    private void Hide()
    {
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
