using System.Globalization;
using TMPro;
using UI.Events;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements
{
	public class InputSlider : MonoBehaviour
	{
		[SerializeField]
		public Slider Slider;
		
		[SerializeField]
		public TMP_InputField InputField;

		[SerializeField]
		public OnValueChangedEvent OnValueChangedEvent = new ();

		[SerializeField]
		public string IntFormat = "#";
		
		[SerializeField]
		public string FloatFormat = "#.##";
		
		public void SetValue(float value)
		{
			Slider.value = value;
		}

		public void SetValueWithoutNotify(float value)
		{
			Slider.SetValueWithoutNotify(value);
			updateInputField();
		}

		public float GetValue()
		{
			return Slider.value;
		}

		public void OnSliderChanged(float value)
		{
			OnValueChangedEvent?.Invoke(value);
			updateInputField();
		}

		public void OnInputFieldChanged(string value)
		{
			if (!float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var result))
				result = Slider.minValue;
			
			result = Mathf.Clamp(result, Slider.minValue, Slider.maxValue);
			Slider.SetValueWithoutNotify(result);
			
			OnValueChangedEvent?.Invoke(result);
			updateInputField();
		}

		private void updateInputField()
		{
			var text = Slider.value.ToString(Slider.wholeNumbers ? IntFormat : FloatFormat, CultureInfo.CurrentCulture);
			InputField.SetTextWithoutNotify(text);
		}
	}
}