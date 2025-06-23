namespace Services.PDF.Addional
{
    using System.Text;

    public static class TextHelper
    {
        public static List<string> SplitText(string text, int maxLineLength)
        {
            List<string> lines = [];
            string[] paragraphs = text.Split('\n');
            foreach (string paragraph in paragraphs)
            {
                string[] words = paragraph.Split(' ');
                StringBuilder currentLine = new StringBuilder();
                foreach (string word in words.Select(x => x.Replace("\t", "    ")))
                {
                    if (word.Length >= maxLineLength)
                    {
                        if (currentLine.Length > 0)
                        {
                            string line = currentLine.ToString().TrimEnd();
                            lines.Add(line);
                            currentLine.Clear();
                        }

                        lines.Add(word);
                        continue;
                    }

                    if (currentLine.Length + word.Length + (currentLine.Length > 0 ? 1 : 0) > maxLineLength)
                    {
                        string line = currentLine.ToString().TrimEnd();
                        lines.Add(line);
                        currentLine.Clear();
                    }

                    if (currentLine.Length > 0)
                        currentLine.Append(" ");
                    currentLine.Append(word);
                }

                if (currentLine.Length > 0)
                {
                    string line = currentLine.ToString().TrimEnd();
                    lines.Add(line);
                    currentLine.Clear();
                }

                if (paragraph != paragraphs[paragraphs.Length - 1])
                {
                    lines.Add("");
                }
            }

            return lines;
        }
    }
}