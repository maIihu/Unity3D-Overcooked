using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerCounter : BaseCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    private Animator _ani;

    protected override void Awake()
    {
        base.Awake();
        _ani = GetComponentInChildren<Animator>();

    }
    
    
    public override void Interact(Player player)
    {
        if (!player.HasKitchenObject())
        {
            _ani.SetTrigger(ContainString.OpenClose);
            var go = Instantiate(kitchenObjectSO.prefab);
            go.Init(kitchenObjectSO);
            go.SetKitchenObjectParent(player);
        }
    }
    
    
}
