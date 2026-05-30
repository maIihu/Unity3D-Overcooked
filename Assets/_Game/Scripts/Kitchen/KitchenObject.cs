using System.Collections.Generic;
using Counter;
using UnityEngine;
using DG.Tweening;
using Pooling;
using Fusion;

namespace Kitchen
{
    public class KitchenObject : NetworkBehaviour, IPoolable
    {
        protected IKitchenObjectParent KitchenObjectParent;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Collider col;

        public void Init()
        {
        }

        public virtual void OnSpawn()
        {
            // Reset state when spawned from pool
        }

        public virtual void OnDespawn()
        {
            if (KitchenObjectParent != null) KitchenObjectParent.ClearKitchenObject();
            transform.DOKill();
        }

        [Networked]
        [OnChangedRender(nameof(OnParentNetworkIdChanged))]
        public NetworkId ParentNetworkId { get; set; }

        private void OnParentNetworkIdChanged()
        {
            if (ParentNetworkId.IsValid)
            {
                if (Runner.TryFindObject(ParentNetworkId, out NetworkObject parentNetObj))
                {
                    if (parentNetObj.TryGetComponent(out IKitchenObjectParent parentObj))
                    {
                        SetKitchenObjectParentLocal(parentObj);
                    }
                }
            }
            else
            {
                // If it is currently held by a non-networked parent (like PlayerLocal), DO NOT drop it.
                // Otherwise, a networked player dropped it, so we apply the drop locally.
                if (KitchenObjectParent != null && KitchenObjectParent.GetNetworkObject() == null)
                {
                    // Retain the local parent (PlayerLocal)
                }
                else
                {
                    SetKitchenObjectParentLocal(null);
                }
            }
        }

        public virtual void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent)
        {
            SetKitchenObjectParentLocal(kitchenObjectParent);

            if (Object != null && Object.IsValid && HasStateAuthority)
            {
                if (kitchenObjectParent != null)
                {
                    var netObj = kitchenObjectParent.GetNetworkObject();
                    ParentNetworkId = netObj != null ? netObj.Id : default;
                }
                else
                {
                    ParentNetworkId = default;
                }
            }
        }

        private void SetKitchenObjectParentLocal(IKitchenObjectParent kitchenObjectParent)
        {
            if (this.KitchenObjectParent != null) this.KitchenObjectParent.ClearKitchenObject();

            this.KitchenObjectParent = kitchenObjectParent;

            if (kitchenObjectParent != null)
            {
                if (kitchenObjectParent.HasKitchenObject() && kitchenObjectParent.GetKitchenObject() != this)
                {
                    return;
                }
                kitchenObjectParent.SetKitchenObject(this);
                transform.parent = kitchenObjectParent.GetKitchenObjectToTransform();
                transform.localPosition = Vector3.zero;

                if (rb != null) rb.isKinematic = true;
                if (col != null) col.enabled = false;

                bool hasNetParent = kitchenObjectParent.GetNetworkObject() != null;
                var nt = GetComponent<NetworkTransform>();
                if (nt != null) nt.enabled = hasNetParent;
                
                var nr = GetComponent("NetworkRigidbody3D") as MonoBehaviour;
                if (nr != null) nr.enabled = hasNetParent;
            }
            else
            {
                transform.parent = null;
                if (rb != null) rb.isKinematic = false;
                if (col != null) col.enabled = true;

                var nt = GetComponent<NetworkTransform>();
                if (nt != null) nt.enabled = true;
                
                var nr = GetComponent("NetworkRigidbody3D") as MonoBehaviour;
                if (nr != null) nr.enabled = true;
            }
        }

        public Rigidbody GetRigidbody()
        {
            return rb;
        }

        public IKitchenObjectParent GetKitchenObjectParent()
        {
            return this.KitchenObjectParent;
        }

        public void DestroySelf()
        {
            if (Object != null && Object.IsValid)
            {
                if (HasStateAuthority)
                {
                    Runner.Despawn(Object);
                }
            }
            else
            {
                PoolManager.Instance.Release(this);
            }
        }

    }
    public enum KitchenType
    {
        Plate = 0,
        Pot = 1,
        Pan = 2,
    }

}

