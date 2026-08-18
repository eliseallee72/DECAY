
using System;

namespace Decay
{
    internal static class ContentIdValidator
    {
        public static string RequireCategory(string value, string category, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A content ID cannot be empty.", parameterName);
            }

            string trimmed = value.Trim();
            if (!IsValidCategoryId(trimmed, category))
            {
                throw new ArgumentException(
                    $"Content ID '{trimmed}' must use the lowercase '{category}.name' format with only lowercase letters, numbers, and underscores in the name.",
                    parameterName);
            }

            return trimmed;
        }

        public static bool IsValidCategoryId(string value, string category)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(category))
            {
                return false;
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            string prefix = category + ".";
            if (!value.StartsWith(prefix, StringComparison.Ordinal) || value.Length == prefix.Length)
            {
                return false;
            }

            int nameStart = prefix.Length;
            int nameLength = value.Length - nameStart;
            return IsValidNameSegment(value, nameStart, nameLength);
        }

        private static bool IsValidNameSegment(string value, int start, int length)
        {
            char first = value[start];
            if (!IsLowercaseLetter(first))
            {
                return false;
            }

            int end = start + length;
            for (int i = start + 1; i < end; i++)
            {
                char c = value[i];
                if (!IsLowercaseLetter(c) && !char.IsDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLowercaseLetter(char c) => c >= 'a' && c <= 'z';
    }
}
