using System.Windows.Documents;
using System.Windows;

namespace Talepreter.GUI.Common
{
    public class FormattedTextHelper
    {
        private static readonly IDictionary<int, string> _Tags = new Dictionary<int, string>();
        static FormattedTextHelper()
        {
            _Tags[1] = "`";
            _Tags[2] = "*";
            _Tags[3] = "**";
            _Tags[4] = "==";
            _Tags[5] = "~~";
            _Tags[6] = "_";
            _Tags[7] = "^";
            _Tags[8] = "~";
            _Tags[9] = "|";
        }

        public static Paragraph FormParagraph(string line)
        {
            var prg = new Paragraph
            {
                Tag = line
            };
            var start = 0;
            if (line.StartsWith("- "))
            {
                prg.SetResourceReference(FrameworkContentElement.StyleProperty, "IndentedParagraphStyle"); // for lists
                start = 2;
            }
            else prg.SetResourceReference(FrameworkContentElement.StyleProperty, "ParagraphStyle");

            // little hack. because model reader can understand it and will put it in new format but sometimes the text (description, notes...) are taken as is, without format check.
            // it is meaningless to be done by model for each and every string. instead here we can simply swap it.
            var trimmedC6 = line.Replace("<u>", "_").Replace("</u>", "_");

            foreach (var child in FindInlines(trimmedC6[start..])) prg.Inlines.Add(child);
            return prg;
        }

        public static Span FormSpan(string line)
        {
            var span = new Span
            {
                Tag = line
            };

            // little hack. because model reader can understand it and will put it in new format but sometimes the text (description, notes...) are taken as is, without format check.
            // it is meaningless to be done by model for each and every string. instead here we can simply swap it.
            var trimmedC6 = line.Replace("<u>", "_").Replace("</u>", "_");

            foreach (var child in FindInlines(trimmedC6)) span.Inlines.Add(child);
            return span;
        }

        private static IEnumerable<Inline> FindInlines(string line)
        {
            if (string.IsNullOrEmpty(line)) yield break;
            string text = "";
            for (int i = 0; i < line.Length;)
            {
                var c = line[i];

                int format;
                // all of these increment index by its own, after the block "i" should be first tagged text block
                switch (c)
                {
                    case '^': format = 7; break;
                    case '*':
                        format = 2;
                        if (i < line.Length - 1 && line[i + 1] == '*')
                        {
                            i++;
                            format = 3;
                        }
                        break;
                    case '~':
                        format = 8;
                        if (i < line.Length - 1 && line[i + 1] == '~')
                        {
                            format = 5;
                            i++;
                        }
                        break;
                    case '=':
                        if (i < line.Length - 1 && line[i + 1] == '=')
                        {
                            i++;
                            format = 4;
                        }
                        else
                        {
                            i++;
                            text += c;
                            continue;
                        }
                        break;
                    case '_': format = 6; break;
                    case '|': format = 9; break;
                    case '`': format = 1; break;
                    default:
                        i++;
                        text += c;
                        continue;
                }

                // accumulated text so far
                if (!string.IsNullOrEmpty(text)) yield return new Run(text);
                text = ""; // reset

                i++; // at this point index 0 is at subsection

                // we have found a formatted text here. find ending of it, ignore invalid nesting here
                var tagLength = _Tags[format].Length;
                var end = line.IndexOf(_Tags[format], i);
                if (end < 0) throw new InvalidOperationException("Tag did not end, seems invalid");

                // this is for nesting
                var subsection = line[i..end];
                if (string.IsNullOrEmpty(subsection)) throw new InvalidOperationException("Tagged text seems to be empty");
                i += subsection.Length + tagLength;

                // this recursion has an owner span
                var span = new Span();
                span.SetResourceReference(TextElement.ForegroundProperty, $"FTBC{format}");
                foreach (var child in FindInlines(subsection)) span.Inlines.Add(child);
                yield return span;
            }

            // the text we have actually maybe has no tag or after some tag there is tag, so we return the value in Run
            if (!string.IsNullOrEmpty(text)) yield return new Run(text);
        }
    }
}
