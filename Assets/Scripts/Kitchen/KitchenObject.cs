using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class KitchenObject : MonoBehaviour
{
    protected IKitchenObjectParent KitchenObjectParent;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;

    public void Init()
    {
    }

    public virtual void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent)
    {
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
        // Chỉ xử lý nếu vật phẩm đang không có chủ (rơi tự do)
        if (KitchenObjectParent != null) return;

        // Kiểm tra xem vật thể va chạm có thể chứa đồ không
        if (collision.gameObject.TryGetComponent(out IKitchenObjectParent counter))
        {
            // Bàn đã có đồ rồi thì thôi
            if (counter.HasKitchenObject()) return;

            // Kiểm tra tính phù hợp của vật phẩm với từng loại bàn
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
                // Bắt đầu quá trình hít vào bàn mượt mà
                if (rb != null) rb.isKinematic = true;
                if (col != null) col.enabled = false;

                Transform targetPoint = counter.GetKitchenObjectToTransform();
                
                // Di chuyển mượt mà tới tâm bàn
                transform.DOMove(targetPoint.position, 0.15f).SetEase(Ease.OutQuad).OnComplete(() => {
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
        if (KitchenObjectParent != null) KitchenObjectParent.ClearKitchenObject();
        Destroy(this.gameObject);
    }


}