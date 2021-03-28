using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectEnsureVisible : MonoBehaviour
{
	private RectTransform maskTransform;

	private ScrollRect mScrollRect;

	private RectTransform mScrollTransform;

	private RectTransform mContent;

	private bool mInitialized;

	private void Awake()
	{
		if (!mInitialized)
		{
			Initialize();
		}
	}

	private void Initialize()
	{
		mScrollRect = GetComponent<ScrollRect>();
		mScrollTransform = ((Component)(object)mScrollRect).transform as RectTransform;
		mContent = mScrollRect.get_content();
		Reset();
		mInitialized = true;
	}

	public void CenterOnItem(RectTransform target)
	{
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		if (!mInitialized)
		{
			Initialize();
		}
		Vector3 worldPointInWidget = GetWorldPointInWidget(mScrollTransform, GetWidgetWorldPoint(target));
		Vector3 vector = GetWorldPointInWidget(mScrollTransform, GetWidgetWorldPoint(maskTransform)) - worldPointInWidget;
		vector.z = 0f;
		if (!mScrollRect.get_horizontal())
		{
			vector.x = 0f;
		}
		if (!mScrollRect.get_vertical())
		{
			vector.y = 0f;
		}
		Vector2 b = new Vector2(vector.x / (mContent.rect.size.x - mScrollTransform.rect.size.x), vector.y / (mContent.rect.size.y - mScrollTransform.rect.size.y));
		Vector2 normalizedPosition = mScrollRect.get_normalizedPosition() - b;
		if ((int)mScrollRect.get_movementType() != 0)
		{
			normalizedPosition.x = Mathf.Clamp01(normalizedPosition.x);
			normalizedPosition.y = Mathf.Clamp01(normalizedPosition.y);
		}
		mScrollRect.set_normalizedPosition(normalizedPosition);
	}

	private void Reset()
	{
		if (!(maskTransform == null))
		{
			return;
		}
		Mask componentInChildren = GetComponentInChildren<Mask>(includeInactive: true);
		if ((bool)(Object)(object)componentInChildren)
		{
			maskTransform = componentInChildren.get_rectTransform();
		}
		if (maskTransform == null)
		{
			RectMask2D componentInChildren2 = GetComponentInChildren<RectMask2D>(includeInactive: true);
			if ((bool)(Object)(object)componentInChildren2)
			{
				maskTransform = componentInChildren2.get_rectTransform();
			}
		}
	}

	private Vector3 GetWidgetWorldPoint(RectTransform target)
	{
		Vector3 b = new Vector3((0.5f - target.pivot.x) * target.rect.size.x, (0.5f - target.pivot.y) * target.rect.size.y, 0f);
		Vector3 position = target.localPosition + b;
		return target.parent.TransformPoint(position);
	}

	private Vector3 GetWorldPointInWidget(RectTransform target, Vector3 worldPoint)
	{
		return target.InverseTransformPoint(worldPoint);
	}
}
