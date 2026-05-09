using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    internal sealed class MapFragmentVisibilityPresenter
    {
        private readonly Dictionary<GameObject, Vector3> _originalScales = new Dictionary<GameObject, Vector3>();
        private readonly Dictionary<SpriteRenderer, Color> _spriteOriginalColors = new Dictionary<SpriteRenderer, Color>();
        private readonly Dictionary<Graphic, Color> _graphicOriginalColors = new Dictionary<Graphic, Color>();
        private readonly Dictionary<CanvasGroup, float> _canvasGroupOriginalAlphas = new Dictionary<CanvasGroup, float>();

        public void Cache(
            MapViewController.ViewStateConfig smallConfig,
            MapViewController.ViewStateConfig mediumConfig,
            MapViewController.ViewStateConfig fullConfig)
        {
            _originalScales.Clear();
            _spriteOriginalColors.Clear();
            _graphicOriginalColors.Clear();
            _canvasGroupOriginalAlphas.Clear();

            CacheConfig(smallConfig);
            CacheConfig(mediumConfig);
            CacheConfig(fullConfig);
        }

        public void Apply(MapViewController.ViewStateConfig config, float hiddenScaleMultiplier)
        {
            SetObjectsVisible(config.objectsToHide, false, hiddenScaleMultiplier);
            SetObjectsVisible(config.objectsToShow, true, hiddenScaleMultiplier);
        }

        public void InsertTransitionTweens(
            Sequence sequence,
            MapViewController.ViewStateConfig targetConfig,
            float showTime,
            float duration,
            float hiddenScaleMultiplier)
        {
            InsertHideTweens(sequence, targetConfig, duration, hiddenScaleMultiplier);
            InsertShowTweens(sequence, targetConfig, showTime, duration, hiddenScaleMultiplier);
        }

        private void CacheConfig(MapViewController.ViewStateConfig config)
        {
            CacheObjects(config.objectsToShow);
            CacheObjects(config.objectsToHide);
        }

        private void CacheObjects(List<GameObject> objects)
        {
            if (objects == null) return;

            foreach (GameObject target in objects)
            {
                CacheObject(target);
            }
        }

        private void CacheObject(GameObject target)
        {
            if (!target || _originalScales.ContainsKey(target)) return;

            _originalScales[target] = target.transform.localScale;

            foreach (SpriteRenderer spriteRenderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (spriteRenderer && !_spriteOriginalColors.ContainsKey(spriteRenderer))
                {
                    _spriteOriginalColors[spriteRenderer] = spriteRenderer.color;
                }
            }

            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic && !_graphicOriginalColors.ContainsKey(graphic))
                {
                    _graphicOriginalColors[graphic] = graphic.color;
                }
            }

            foreach (CanvasGroup canvasGroup in target.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (canvasGroup && !_canvasGroupOriginalAlphas.ContainsKey(canvasGroup))
                {
                    _canvasGroupOriginalAlphas[canvasGroup] = canvasGroup.alpha;
                }
            }
        }

        private void SetObjectsVisible(List<GameObject> objects, bool isVisible, float hiddenScaleMultiplier)
        {
            if (objects == null) return;

            foreach (GameObject target in objects)
            {
                ForceObjectVisible(target, isVisible, hiddenScaleMultiplier);
            }
        }

        private void InsertHideTweens(
            Sequence sequence,
            MapViewController.ViewStateConfig targetConfig,
            float duration,
            float hiddenScaleMultiplier)
        {
            HashSet<GameObject> showSet = BuildObjectSet(targetConfig.objectsToShow);

            if (targetConfig.objectsToHide == null) return;

            foreach (GameObject target in targetConfig.objectsToHide)
            {
                if (!target || showSet.Contains(target)) continue;
                if (!target.activeSelf)
                {
                    ForceObjectVisible(target, false, hiddenScaleMultiplier);
                    continue;
                }

                sequence.Insert(0f, CreateVisibilityTween(target, false, duration, hiddenScaleMultiplier));
            }
        }

        private void InsertShowTweens(
            Sequence sequence,
            MapViewController.ViewStateConfig targetConfig,
            float startTime,
            float duration,
            float hiddenScaleMultiplier)
        {
            if (targetConfig.objectsToShow == null) return;

            foreach (GameObject target in targetConfig.objectsToShow)
            {
                if (!target) continue;
                if (IsObjectFullyVisible(target)) continue;

                sequence.Insert(startTime, CreateVisibilityTween(target, true, duration, hiddenScaleMultiplier));
            }
        }

        private static HashSet<GameObject> BuildObjectSet(List<GameObject> objects)
        {
            HashSet<GameObject> result = new HashSet<GameObject>();
            if (objects == null) return result;

            foreach (GameObject target in objects)
            {
                if (target) result.Add(target);
            }

            return result;
        }

        private Tween CreateVisibilityTween(GameObject target, bool isVisible, float duration, float hiddenScaleMultiplier)
        {
            CacheObject(target);

            target.transform.DOKill(false);
            KillAlphaTweens(target);

            Vector3 originalScale = GetOriginalScale(target);
            Vector3 hiddenScale = originalScale * Mathf.Clamp01(hiddenScaleMultiplier);

            if (duration <= 0f)
            {
                ForceObjectVisible(target, isVisible, hiddenScaleMultiplier);
                return DOVirtual.DelayedCall(0f, () => { });
            }

            Sequence sequence = DOTween.Sequence();

            if (isVisible)
            {
                target.SetActive(true);

                if (target.transform.localScale.sqrMagnitude <= 0.0001f)
                {
                    target.transform.localScale = hiddenScale;
                }

                SetAlpha(target, 0f);
                sequence.Join(target.transform.DOScale(originalScale, duration).SetEase(Ease.OutCubic));
                JoinAlphaTweens(sequence, target, true, duration);
            }
            else
            {
                sequence.Join(target.transform.DOScale(hiddenScale, duration).SetEase(Ease.InCubic));
                JoinAlphaTweens(sequence, target, false, duration);
                sequence.OnComplete(() => ForceObjectVisible(target, false, hiddenScaleMultiplier));
            }

            return sequence;
        }

        private Vector3 GetOriginalScale(GameObject target)
        {
            if (target && _originalScales.TryGetValue(target, out Vector3 scale))
            {
                return scale;
            }

            return target ? target.transform.localScale : Vector3.one;
        }

        private void ForceObjectVisible(GameObject target, bool isVisible, float hiddenScaleMultiplier)
        {
            if (!target) return;

            CacheObject(target);
            target.transform.DOKill(false);
            KillAlphaTweens(target);

            target.SetActive(isVisible);
            target.transform.localScale = isVisible
                ? GetOriginalScale(target)
                : GetOriginalScale(target) * Mathf.Clamp01(hiddenScaleMultiplier);

            SetAlpha(target, isVisible ? 1f : 0f);
        }

        private void JoinAlphaTweens(Sequence sequence, GameObject target, bool isVisible, float duration)
        {
            foreach (SpriteRenderer spriteRenderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!spriteRenderer) continue;

                Color original = GetSpriteOriginalColor(spriteRenderer);
                sequence.Join(spriteRenderer.DOFade(isVisible ? original.a : 0f, duration));
            }

            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic) continue;

                Color original = GetGraphicOriginalColor(graphic);
                sequence.Join(graphic.DOFade(isVisible ? original.a : 0f, duration));
            }

            foreach (CanvasGroup canvasGroup in target.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (!canvasGroup) continue;

                float original = GetCanvasGroupOriginalAlpha(canvasGroup);
                sequence.Join(canvasGroup.DOFade(isVisible ? original : 0f, duration));
            }
        }

        private void SetAlpha(GameObject target, float normalizedAlpha)
        {
            foreach (SpriteRenderer spriteRenderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!spriteRenderer) continue;

                Color original = GetSpriteOriginalColor(spriteRenderer);
                Color color = original;
                color.a = original.a * normalizedAlpha;
                spriteRenderer.color = color;
            }

            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic) continue;

                Color original = GetGraphicOriginalColor(graphic);
                Color color = original;
                color.a = original.a * normalizedAlpha;
                graphic.color = color;
            }

            foreach (CanvasGroup canvasGroup in target.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (!canvasGroup) continue;

                canvasGroup.alpha = GetCanvasGroupOriginalAlpha(canvasGroup) * normalizedAlpha;
            }
        }

        private static void KillAlphaTweens(GameObject target)
        {
            foreach (SpriteRenderer spriteRenderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (spriteRenderer) DOTween.Kill(spriteRenderer, false);
            }

            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic) DOTween.Kill(graphic, false);
            }

            foreach (CanvasGroup canvasGroup in target.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (canvasGroup) DOTween.Kill(canvasGroup, false);
            }
        }

        private bool IsObjectFullyVisible(GameObject target)
        {
            if (!target || !target.activeSelf) return false;

            Vector3 originalScale = GetOriginalScale(target);
            if (originalScale.sqrMagnitude > 0.0001f
                && target.transform.localScale.sqrMagnitude < originalScale.sqrMagnitude * 0.9f)
            {
                return false;
            }

            foreach (SpriteRenderer spriteRenderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!spriteRenderer) continue;

                Color original = GetSpriteOriginalColor(spriteRenderer);
                float expected = Mathf.Max(0.001f, original.a);
                if (spriteRenderer.color.a < expected * 0.95f) return false;
            }

            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic) continue;

                Color original = GetGraphicOriginalColor(graphic);
                float expected = Mathf.Max(0.001f, original.a);
                if (graphic.color.a < expected * 0.95f) return false;
            }

            foreach (CanvasGroup canvasGroup in target.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (!canvasGroup) continue;

                float expected = Mathf.Max(0.001f, GetCanvasGroupOriginalAlpha(canvasGroup));
                if (canvasGroup.alpha < expected * 0.95f) return false;
            }

            return true;
        }

        private Color GetSpriteOriginalColor(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer && _spriteOriginalColors.TryGetValue(spriteRenderer, out Color color))
            {
                return color;
            }

            return spriteRenderer ? spriteRenderer.color : Color.white;
        }

        private Color GetGraphicOriginalColor(Graphic graphic)
        {
            if (graphic && _graphicOriginalColors.TryGetValue(graphic, out Color color))
            {
                return color;
            }

            return graphic ? graphic.color : Color.white;
        }

        private float GetCanvasGroupOriginalAlpha(CanvasGroup canvasGroup)
        {
            if (canvasGroup && _canvasGroupOriginalAlphas.TryGetValue(canvasGroup, out float alpha))
            {
                return alpha;
            }

            return canvasGroup ? canvasGroup.alpha : 1f;
        }
    }
}
