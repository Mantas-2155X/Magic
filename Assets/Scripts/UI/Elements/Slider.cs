using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Elements
{
	public class Slider : UnityEngine.UI.Slider, ISubmitHandler, ICancelHandler
	{
		[SerializeField]
		public float StepSizeMultiplier = 0.05f;

		private bool shouldControl;
		
		public override void OnMove(AxisEventData eventData)
		{
			if (!IsActive() || !IsInteractable() || !shouldControl)
			{
				base.OnMove(eventData);
				return;
			}

			var isHorizontal = direction is Direction.LeftToRight or Direction.RightToLeft;
			var isReverse = direction is Direction.RightToLeft or Direction.TopToBottom;
			
			var step = wholeNumbers ? 1 : (maxValue - minValue) * StepSizeMultiplier;
			
			switch (eventData.moveDir)
			{
				case MoveDirection.Left:
					if (isHorizontal)
					{
						Set(isReverse ? value + step : value - step);
						return;
					}
					break;
				case MoveDirection.Right:
					if (isHorizontal)
					{
						Set(isReverse ? value - step : value + step);
						return;
					}
					break;
				case MoveDirection.Up:
					if (!isHorizontal)
					{
						Set(isReverse ? value - step : value + step);
						return;
					}
					break;
				case MoveDirection.Down:
					if (!isHorizontal)
					{
						Set(isReverse ? value + step : value - step);
						return;
					}
					break;
			}
			
			base.OnMove(eventData);
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			base.OnDeselect(eventData);
			shouldControl = false;
		}
		
		public void OnSubmit(BaseEventData eventData)
		{
			shouldControl = !shouldControl;
		}
		
		public void OnCancel(BaseEventData eventData)
		{
			shouldControl = false;
		}
	}
}