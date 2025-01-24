using System.Collections.Generic;
using System.Text;

namespace Tools
{
	public static class TextTools
	{
		// https://stackoverflow.com/a/14655185
		public static IEnumerable<string> ParseText(string line, char delimiter, char textQualifier)
		{
			if (string.IsNullOrWhiteSpace(line))
				yield break;

			var inString = false;
			var token = new StringBuilder();

			for (var i = 0; i < line.Length; i++)
			{
				var currentChar = line[i];

				var prevChar = i > 0 ? line[i - 1] : '\0';
				var nextChar = i + 1 < line.Length ? line[i + 1] : '\0';

				if (currentChar == textQualifier && (prevChar == '\0' || prevChar == delimiter) && !inString)
				{
					inString = true;
					continue;
				}

				if (currentChar == textQualifier && (nextChar == '\0' || nextChar == delimiter) && inString)
				{
					inString = false;
					continue;
				}

				if (currentChar == delimiter && !inString)
				{
					yield return token.ToString();
					token = token.Remove(0, token.Length);
					continue;
				}

				token = token.Append(currentChar);

			}

			yield return token.ToString();
		}
	}
}