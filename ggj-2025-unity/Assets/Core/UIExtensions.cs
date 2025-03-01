using UnityEngine;
using UnityEngine.UI;

public static class UIExtensions
{
  // Shared array used to receive result of RectTransform.GetWorldCorners
  private static Vector3[] _corners = new Vector3[4];

  /// <summary>
  /// Transform the bounds of the current rect transform to the space of another transform.
  /// </summary>
  /// <param name="source">The rect to transform</param>
  /// <param name="target">The target space to transform to</param>
  /// <returns>The transformed bounds</returns>
  public static Bounds TransformBoundsTo(this RectTransform source, Transform target)
  {
    // Based on code in ScrollRect's internal GetBounds and InternalGetBounds methods
    var bounds = new Bounds();
    if (source != null)
    {
      source.GetWorldCorners(_corners);

      var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
      var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

      var matrix = target.worldToLocalMatrix;
      for (int j = 0; j < 4; j++)
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

  public static void MoveToRectWithOffset(this RectTransform source, RectTransform target, Vector2 offset)
  {
    var bounds = target.TransformBoundsTo(source.parent);
    source.localPosition = bounds.center.XY() + offset;
  }

  /// <summary>
  /// Normalize a distance to be used in verticalNormalizedPosition or horizontalNormalizedPosition.
  /// </summary>
  /// <param name="axis">Scroll axis, 0 = horizontal, 1 = vertical</param>
  /// <param name="distance">The distance in the scroll rect's view's coordiante space</param>
  /// <returns>The normalized scoll distance</returns>
  public static float NormalizeScrollDistance(this ScrollRect scrollRect, int axis, float distance)
  {
    // Based on code in ScrollRect's internal SetNormalizedPosition method
    var viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform;
    var viewBounds = new Bounds(viewport.rect.center, viewport.rect.size);

    var content = scrollRect.content;
    var contentBounds = content != null ? content.TransformBoundsTo(viewport) : new Bounds();

    var hiddenLength = contentBounds.size[axis] - viewBounds.size[axis];
    return distance / hiddenLength;
  }

  /// <summary>
  /// Scroll the target element to the vertical center of the scroll rect's viewport.
  /// Assumes the target element is part of the scroll rect's contents.
  /// </summary>
  /// <param name="scrollRect">Scroll rect to scroll</param>
  /// <param name="target">Element of the scroll rect's content to center vertically</param>
  public static Vector2 GetCenterScrollPos(this ScrollRect scrollRect, RectTransform target)
  {
    // The scroll rect's view's space is used to calculate scroll position
    var view = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform;

    // Calcualte the scroll offset in the view's space
    var viewRect = view.rect;
    var elementBounds = target.TransformBoundsTo(view);
    var offsetX = viewRect.center.x - elementBounds.center.x;
    var offsetY = viewRect.center.y - elementBounds.center.y;

    // Normalize and apply the calculated offset
    var scrollPosX = scrollRect.horizontalNormalizedPosition - scrollRect.NormalizeScrollDistance(0, offsetX);
    var scrollPosY = scrollRect.verticalNormalizedPosition - scrollRect.NormalizeScrollDistance(1, offsetY);
    return new Vector2(scrollPosX, scrollPosY);
  }
}