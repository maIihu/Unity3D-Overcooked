using System;
using System.Collections.Generic;
using UnityEngine;
using Kitchen;

namespace GameCore
{
    public class DeliveryController : MonoBehaviour
    {
        public static DeliveryController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void DeliverPlate(PlateObject plate)
        {
            // Implementation
        }
    }
}
