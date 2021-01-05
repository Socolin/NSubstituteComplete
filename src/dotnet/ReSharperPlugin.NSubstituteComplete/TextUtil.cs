using System.Collections.Generic;
using System.Text;
using JetBrains.ReSharper.Psi.JavaScript.Util.Literals;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace ReSharperPlugin.NSubstituteComplete
{
    public class TextUtil
    {
        public static string ToKebabCase(string input)
        {
            var tokens = Tokenize(input);
            return string.Join("-", tokens);
        }

        public static string ToCamelCase(string input)
        {
            var tokens = Tokenize(input);
            var sb = new StringBuilder();
            for (var index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];
                if (index > 0)
                    sb.Append(token[0].ToUpperFast()).Append(token.Slice(1, token.Length));
                else
                    sb.Append(token);
            }

            return sb.ToString();
        }

        private static List<string> Tokenize(string input)
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