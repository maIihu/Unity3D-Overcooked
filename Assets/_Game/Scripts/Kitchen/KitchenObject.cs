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

        private NetworkTransform _cachedNetworkTransform;
        private MonoBehaviour _cachedNetworkRigidbody;

        private void Awake()
        {
            _cachedNetworkTransform = GetComponent<NetworkTransform>();
            _cachedNetworkRigidbody = GetComponent("NetworkRigidbody3D") as MonoBehaviour;
        }

        public void Init()
        {
        }

        public override void Spawned()
        {
            OnSpawn();
        }

        public virtual void OnSpawn()
        {
            // Reset state when spawned from pool
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            OnDespawn();
        }

        public virtual void OnDespawn()
        {
            if (KitchenObjectParent != null) KitchenObjectParent.ClearKitchenObject();
            transform.DOKill();
        }

        protected bool _isOffline;

        private void Start()
        {
            if (GameCore.GameManager.Instance != null && GameCore.GameManager.Instance.IsOffline)
            {
                _isOffline = true;
            }
        }

        public override void Render()
        {
            if (_isOffline) return;

            if (ParentNetworkId.IsValid)
            {
                if (KitchenObjectParent == null || KitchenObjectParent.GetNetworkObject() == null || KitchenObjectParent.GetNetworkObject().Id != ParentNetworkId)
                {
                    OnParentNetworkIdChanged();
                }
            }
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

                if (KitchenObjectParent != null && KitchenObjectParent.GetNetworkObject() == null)
                {
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
                if (_cachedNetworkTransform != null) _cachedNetworkTransform.enabled = !hasNetParent;
                if (_cachedNetworkRigidbody != null) _cachedNetworkRigidbody.enabled = !hasNetParent;

                // Send pickup or drop message
                if (_Game.Scripts.DesignPattern.Observer.MessageManager.Instance != null)
                {
                    var msgType = (kitchenObjectParent is _Game.Scripts.Gameplay.IPlayer)
                        ? _Game.Scripts.DesignPattern.Observer.ProjectMessageType.OnPickupObject
                        : _Game.Scripts.DesignPattern.Observer.ProjectMessageType.OnDropObject;

                    _Game.Scripts.DesignPattern.Observer.MessageManager.Instance.SendMessage(
                        new _Game.Scripts.DesignPattern.Observer.Message(msgType, new object[] { transform.position })
                    );
                }
            }
            else
            {
                transform.parent = null;
                if (rb != null) rb.isKinematic = false;
                if (col != null) col.enabled = true;

                if (_cachedNetworkTransform != null) _cachedNetworkTransform.enabled = true;
                if (_cachedNetworkRigidbody != null) _cachedNetworkRigidbody.enabled = true;
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
            if (KitchenObjectParent != null) KitchenObjectParent.ClearKitchenObject();
            transform.DOKill();

            if (Object != null && Object.IsValid)
            {
                // Online: Despawn qua Fusion
                if (HasStateAuthority)
                {
                    Runner.Despawn(Object);
                }
            }
            else
            {
                // Offline: Destroy trực tiếp
                Destroy(gameObject);
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

