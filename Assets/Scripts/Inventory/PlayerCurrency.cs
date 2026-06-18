using System;
using UnityEngine;

namespace VoidBound.Inventory
{
    public class PlayerCurrency : MonoBehaviour
    {
        [SerializeField] private int gold;
        [SerializeField] private int voidShards;

        public int Gold => gold;
        public int VoidShards => voidShards;
        public event Action OnCurrencyChanged;

        public void AddGold(int amount)
        {
            gold += amount;
            OnCurrencyChanged?.Invoke();
        }

        public void AddVoidShards(int amount)
        {
            voidShards += amount;
            OnCurrencyChanged?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount) return false;
            gold -= amount;
            OnCurrencyChanged?.Invoke();
            return true;
        }
    }
}
