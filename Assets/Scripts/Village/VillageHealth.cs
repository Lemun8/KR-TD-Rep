using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class VillageHealth : MonoBehaviour
{
    public static VillageHealth Instance { get; private set; }

    public Health Health { get; private set; }

    // True only at 100%. This will drive the end-of-wave bonus resources
    public bool IsPerfectCondition => Health.Current >= Health.Max;

    public event Action Destroyed; // the Game Over / lose condition will listen to this

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Health = GetComponent<Health>();
        Health.Died += HandleDied;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Health != null) Health.Died -= HandleDied;
    }

    public void TakeDamage(float amount)
    {
        Health.TakeDamage(amount);
        Debug.Log($"Village hit! Health: {Health.Current}/{Health.Max}");
    }

    private void HandleDied()
    {
        Debug.Log("Village destroyed - GAME OVER");
        Destroyed?.Invoke();
    }
}