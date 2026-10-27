using UnityEngine;

// Plays the "Shoot" animation every time the tower fires.
// Put this on the SAME object as the Animator (the object that has the SpriteRenderer).
[RequireComponent(typeof(Animator))]
public class TowerAnimation : MonoBehaviour
{
    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private Animator animator;
    private Tower tower;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        tower = GetComponentInParent<Tower>();   // also finds a Tower on this same object

        if (tower == null)
        {
            Debug.LogWarning("TowerAnimation: no Tower found on this object or its parents.", this);
        }
    }

    private void OnEnable()
    {
        if (tower != null) tower.Fired += HandleFired;
    }

    private void OnDisable()
    {
        if (tower != null) tower.Fired -= HandleFired;
    }

    private void HandleFired()
    {
        animator.SetTrigger(ShootHash);
    }
}