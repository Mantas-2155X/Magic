using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Tools
{
    // https://gist.github.com/sttz/c406aec3ace821738ecd4fa05833d21d?permalink_comment_id=4174541#gistcomment-4174541
    public static class ScrollToCenterHelper
    {
        private const int MaxCornersCount = 4;
        private const float ScrollTimeStep = 0.25f;
        private const int MaxScrollTimeSec = 2;

        private static readonly Vector3[] _corners = new Vector3[MaxCornersCount];
        private static readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
        private static Coroutine _coroutine;

        public static async UniTask ScrollToCenterDelayed(this ScrollRect scrollRect, RectTransform target, MonoBehaviour monoBehaviour)
        {
            await UniTask.NextFrame();
            
            if (scrollRect == null)
                return;
            
            ScrollToCenter(scrollRect, target, monoBehaviour);
        }
        
        public static void ScrollToCenter(this ScrollRect scrollRect, RectTransform target, MonoBehaviour monoBehaviour)
        {
            // The scroll rect's view's space is used to calculate scroll position
            var view = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

            // Calcualte the scroll offset in the view's space
            var viewRect = view.rect;
            var elementBounds = target.TransformBoundsTo(view);
            var offset = viewRect.center.y - elementBounds.center.y;

            // Normalize and apply the calculated offset
            var scrollPos = scrollRect.verticalNormalizedPosition - scrollRect.NormalizeScrollDistance(1, offset);

            if (_coroutine != null)
            {
                monoBehaviour.StopCoroutine(_coroutine);
            }

            _coroutine = monoBehaviour.StartCoroutine(VerticalNormalizedPositionSmooth(scrollRect, Mathf.Clamp(scrollPos, 0f, 1f)));
        }

        private static Bounds TransformBoundsTo(this RectTransform source, Transform target)
        {
            var bounds = new Bounds();

            if (source != null)
            {
                source.GetWorldCorners(_corners);

                var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                var matrix = target.worldToLocalMatrix;

                for (int j = 0; j < MaxCornersCount; j++)
                {
                    Vector3 v = matrix.MultiplyPoint3x4(_corners[j]);
                    vMin = Vector3.Min(v, vMin);
                    vMax = Vector3.Max(v, vMax);
                }

                bounds = new Bounds(vMin, Vector3.zero);
                bounds.Encapsulate(vMax);
            }

            return bounds;
        }

        private static float NormalizeScrollDistance(this ScrollRect scrollRect, int axis, float distance)
        {
            var viewport = scrollRect.viewport;
            var viewRect = viewport != null ? viewport : scrollRect.GetComponent<RectTransform>();
            var viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);

            var content = scrollRect.content;
            var contentBounds = content != null ? content.TransformBoundsTo(viewRect) : new Bounds();

            var hiddenLength = contentBounds.size[axis] - viewBounds.size[axis];

            return distance / hiddenLength;
        }

        private static IEnumerator VerticalNormalizedPositionSmooth(ScrollRect scrollRect, float position)
        {
            var maxTime = DateTime.Now.AddSeconds(MaxScrollTimeSec).Second;

            while (true)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, position, ScrollTimeStep);

                yield return _waitForEndOfFrame;

                if (scrollRect == null)
                    yield break;
                
                var pos1 = Mathf.Round(scrollRect.verticalNormalizedPosition * 1000.0f) * 0.001f;
                var pos2 = Mathf.Round(position * 1000.0f) * 0.001f;

                if (pos1 == pos2 || maxTime <= DateTime.Now.Second)
                {
                    scrollRect.verticalNormalizedPosition = position;

                    yield break;
                }
            }
        }
    }
}