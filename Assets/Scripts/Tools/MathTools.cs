namespace Tools
{
	public static class MathTools
	{
		public static float Remap(float value, float startingMin, float startingMax, float targetMin, float targetMax)
		{
			return (value - startingMin) * (targetMax - targetMin) / (startingMax - startingMin) + targetMin;
		}
	}
}