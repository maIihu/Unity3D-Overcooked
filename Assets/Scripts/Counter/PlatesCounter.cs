using System;
using UnityEngine;

public class PlatesCounter : BaseCounter, IKitchenObjectParent
{
    [SerializeField] private PlateObject plateObjectPrefab;

    private KitchenObject _kitchenObject;

    private float _spawnPlateTimer;
    private float _spawnPlateTimerMax = 4f;

    protected override void Start()
    {
        base.Start();
        // Không gọi SpawnPlate() ở đây nữa để tránh việc sinh đĩa đè lên nhau nếu chưa trễ 4 giây từ lúc start.
        // Hoặc giữ nguyên tuỳ ý, nhưng vòng lặp Update() sẽ lo việc sinh đĩa.
    }

    private void Update()
    {
        if (!HasKitchenObject())
        {
            _spawnPlateTimer += Time.deltaTime;
            if (_spawnPlateTimer > _spawnPlateTimerMax)
            {
                SpawnPlate();
                _spawnPlateTimer = 0;
            }
        }
    }

    public override void Interact(Player player)
    {
        base.Interact(player);
        if (HasKitchenObject())
        {
            // Trên bàn đang có đĩa
            if (!player.HasKitchenObject())
            {
                // Người chơi không cầm gì -> Lấy đĩa từ bàn
                _kitchenObject.SetKitchenObjectParent(player);
            }
            else
            {
                // Người chơi đang cầm 1 vật
                if (player.GetKitchenObject() is FoodObject food)
                {
                    // Ép kiểu đĩa trên bàn và thả thức ăn vào đĩa
                    PlateObject plate = _kitchenObject as PlateObject;
                    if (food.FoodState == FoodState.Cut)
                    {
                        food.SetKitchenObjectParent(plate);
                    }
                }
            }
        }
        else
        {
            // Bàn đang trống
            if (player.HasKitchenObject() && player.GetKitchenObject() is PlateObject)
            {
                // Chỉ cho phép đặt Đĩa trở lại bàn PlatesCounter
                player.GetKitchenObject().SetKitchenObjectParent(this);
            }
        }
    }

    private void SpawnPlate()
    {
        PlateObject plate = Instantiate(plateObjectPrefab);
        plate.SetKitchenObjectParent(this);
    }

    #region IKitchenObjectParent

    public Transform GetKitchenObjectToTransform()
    {
        return CounterTopPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this._kitchenObject = kitchenObject;
    }

    public KitchenObject GetKitchenObject()
    {
        return this._kitchenObject;
    }

    public void ClearKitchenObject()
    {
        this._kitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return this._kitchenObject != null;
    }

    #endregion

}
