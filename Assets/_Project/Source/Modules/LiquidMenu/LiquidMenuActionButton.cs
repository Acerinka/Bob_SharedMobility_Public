using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Bob.SharedMobility
{
    public class LiquidMenuActionButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Action")]
        public UnityEvent onClick;

        [Header("Feedback")]
        public bool enableVisualFeedback = true;

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
            Trigger();
        }

        public void OnSubButtonClick()
        {
            Trigger();
        }

        private void Trigger()
        {
            if (enableVisualFeedback)
            {
                transform.DOKill();
                transform.localScale = Vector3.one;
                transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f);
            }

            onClick?.Invoke();
        }
    }
}
