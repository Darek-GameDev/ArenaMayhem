using UnityEngine;

[DisallowMultipleComponent]
public class BowHandVisualAnimationEvents : MonoBehaviour
{
    [Header("Bow Visual")]
    [SerializeField] private GameObject bowInHandObject;

    // Animation Event: call this when taking the bow into hand.
    public void AnimationEvent_ActivateBowInHand()
    {
        SetBowInHandActive(true);
    }

    // Animation Event: call this when putting away/releasing the bow visual.
    public void AnimationEvent_DeactivateBowInHand()
    {
        SetBowInHandActive(false);
    }

    private void SetBowInHandActive(bool isActive)
    {
        if (bowInHandObject == null)
        {
            return;
        }

        if (bowInHandObject.activeSelf != isActive)
        {
            bowInHandObject.SetActive(isActive);
        }
    }
}
