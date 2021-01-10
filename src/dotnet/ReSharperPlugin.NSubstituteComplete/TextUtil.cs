using System.Collections.Generic;
using System.Text;
using JetBrains.ReSharper.Psi.JavaScript.Util.Literals;
using JetBrains.Util;

namespace ReSharperPlugin.NSubstituteComplete
{
    public class TextUtil
    {
        public static string ToKebabCase(string input)
        {
            var tokens = TokenizeLower(input);
            return string.Join("-", tokens);
        }

        public static List<string> TokenizeLower(string input)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                if (c.IsLetterFast() && c.IsUpperFast())
                    AddToken(tokens, sb);

                if (c == '-' || c == '_')
                    AddToken(tokens, sb);

                if (c.IsLetterFast())
                    sb.Append(c.ToLowerFast());
                else if (c.IsDigit())
                    sb.Append(c);
            }

            AddToken(tokens, sb);

            return tokens;
        }

        private static void AddToken(List<string> tokens, StringBuilder sb)
        {
            if (sb.Length > 0)
                tokens.Add(sb.ToString());
            sb.Clear();
        }
    }
}