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

        public virtual void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent)
        {
            // No State Authority request needed in Host-Client mode

            if (this.KitchenObjectParent != null) KitchenObjectParent.ClearKitchenObject();

            this.KitchenObjectParent = kitchenObjectParent;

            if (kitchenObjectParent != null)
            {
                if (kitchenObjectParent.HasKitchenObject())
                {
                    return;
                }
                kitchenObjectParent.SetKitchenObject(this);
                transform.parent = kitchenObjectParent.GetKitchenObjectToTransform();
                transform.localPosition = Vector3.zero;

                if (rb != null) rb.isKinematic = true;
                if (col != null) col.enabled = false;
            }
            else
            {
                transform.parent = null;
                if (rb != null) rb.isKinematic = false;
                if (col != null) col.enabled = true;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (KitchenObjectParent != null) return;

            if (collision.gameObject.TryGetComponent(out IKitchenObjectParent counter))
            {
                if (counter.HasKitchenObject()) return;
                bool canPlace = false;

                if (counter is ClearCounter || counter is ContainerCounter)
                {
                    canPlace = true;
                }
                else if (counter is CuttingCounter && this is FoodObject)
                {
                    canPlace = true;
                }
                else if (counter is StoveCounter && this is PotObject)
                {
                    canPlace = true;
                }
                else if (counter is PlatesCounter && this is PlateObject)
                {
                    canPlace = true;
                }

                if (canPlace)
                {
                    if (rb != null) rb.isKinematic = true;
                    if (col != null) col.enabled = false;

                    Transform targetPoint = counter.GetKitchenObjectToTransform();

                    transform.DOMove(targetPoint.position, 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
                    {
                        SetKitchenObjectParent(counter);
                        transform.localRotation = Quaternion.identity;
                        transform.localPosition = Vector3.zero;
                    });
                }
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

