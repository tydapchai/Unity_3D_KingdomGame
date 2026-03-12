using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(ThirdPersonController))]
public class PlayerHitReceiver : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;

    [Header("Hit Reaction")]
    [SerializeField] private Animator anim;
    [SerializeField] private ThirdPersonController thirdPersonController;
    [SerializeField] private PlayerControl playerControl;
    [SerializeField] private float hitInvulnerability = 0.2f;
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private bool debug;

    private float _nextAllowedHitTime;

    private void Start()
    {
        currentHealth = maxHealth;

        if (anim == null) anim = GetComponent<Animator>();
        if (thirdPersonController == null) thirdPersonController = GetComponent<ThirdPersonController>();
        if (playerControl == null) playerControl = GetComponent<PlayerControl>();
    }

    public void TakeHit(Vector3 attackerPos, float damage, float knockbackForce, float knockbackDuration)
    {
        if (isDead || Time.time < _nextAllowedHitTime)
        {
            return;
        }

        _nextAllowedHitTime = Time.time + hitInvulnerability;
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        Vector3 knockbackDirection = transform.position - attackerPos;
        knockbackDirection.y = 0f;
        if (knockbackDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            knockbackDirection = -transform.forward;
        }
        knockbackDirection.Normalize();

        if (playerControl != null)
        {
            playerControl.InterruptAttack();
        }

        if (thirdPersonController != null)
        {
            thirdPersonController.ApplyExternalKnockback(knockbackDirection, knockbackForce, knockbackDuration);
        }

        TrySetTrigger(hitTriggerName);

        if (debug)
        {
            Debug.Log($"{gameObject.name} nhận {damage} sát thương. Máu còn: {currentHealth}");
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (playerControl != null)
        {
            playerControl.enabled = false;
        }

        if (thirdPersonController != null)
        {
            thirdPersonController.SetCombatLock(true);
        }

        if (debug)
        {
            Debug.Log($"{gameObject.name} đã gục.");
        }
    }

    private void TrySetTrigger(string triggerName)
    {
        if (anim == null || string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
            {
                anim.ResetTrigger(triggerName);
                anim.SetTrigger(triggerName);
                return;
            }
        }
    }
}
