using System;
using UnityEngine;

[Serializable]
public struct ResourceAmount
{
    public ResourceType type;
    [Min(0)] public int amount;
}