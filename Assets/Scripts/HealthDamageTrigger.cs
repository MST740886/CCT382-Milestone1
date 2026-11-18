using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HealthDamageTrigger : MonoBehaviour
{
    [SerializeField] private FloatVariableSO healthValue;
    [SerializeField, Min(0f)] private float damageAmount = 10f;
    [SerializeField] private GameEventSO onHealthChanged;
    public UnityEvent onDamaged = new UnityEvent();

    private readonly HashSet<int> playersInside = new HashSet<int>();
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        // CharacterController contacts can be inconsistent with trigger callbacks,
        // so poll the trigger volume as a fallback and damage only on entry.
        if (healthValue == null || triggerCollider == null || !triggerCollider.enabled)
            return;

        Bounds bounds = triggerCollider.bounds;
        Collider[] overlaps = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity,
            Physics.AllLayers, QueryTriggerInteraction.Collide);
        HashSet<int> currentlyInside = new HashSet<int>();

        foreach (Collider other in overlaps)
        {
            if (other == triggerCollider)
                continue;

            CharacterController controller = FindPlayerController(other);
            if (controller == null)
                continue;

            int playerId = controller.GetInstanceID();
            currentlyInside.Add(playerId);
            if (playersInside.Add(playerId))
                ApplyDamage("volume overlap");
        }

        playersInside.RemoveWhere(playerId => !currentlyInside.Contains(playerId));
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamagePlayer(other, "OnTriggerEnter");
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamagePlayer(other, "OnTriggerStay");
    }

    private void OnTriggerExit(Collider other)
    {
        CharacterController controller = FindPlayerController(other);
        if (controller != null)
            playersInside.Remove(controller.GetInstanceID());
    }

    private void TryDamagePlayer(Collider other, string callback)
    {
        CharacterController controller = FindPlayerController(other);
        if (controller == null)
            return;

        if (playersInside.Add(controller.GetInstanceID()))
            ApplyDamage(callback);
    }

    private void ApplyDamage(string callback)
    {
        if (healthValue == null)
        {
            Debug.LogError("Damage trigger detected the player, but FloatVariableSO is not assigned.", this);
            return;
        }

        healthValue.value = Mathf.Max(healthValue.value - damageAmount, 0f);
        onDamaged?.Invoke();
        onHealthChanged?.Raise();
        Debug.Log($"Damage trigger fired via {callback}. Applied {damageAmount} damage; health is now {healthValue.value}.", this);
    }

    private static CharacterController FindPlayerController(Collider other)
    {
        CharacterController controller = other.GetComponentInParent<CharacterController>();
        return controller != null ? controller : other.transform.root.GetComponent<CharacterController>();
    }
}
