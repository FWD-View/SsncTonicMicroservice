using System;

namespace Tonic.Common.Models;

/// <summary>
/// Human readable text-point and contextual snippet
/// </summary>
public sealed record TextPosition(int Line, int Offset, string Snippet)
{
    public static TextPosition? ParseOffset(string sourceText, int desiredOffset)
    {
        if (!string.IsNullOrEmpty(sourceText) && desiredOffset > -1)
        {
            string[] lines = sourceText.Split('\n');

            int? lineIndex = null;
            int charOffset = 0;

            var currentOffset = 0;

            if (desiredOffset > 0)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    charOffset = 0;
                    foreach (char _ in lines[i])
                    {
                        charOffset++;
                        currentOffset++;

                        if (currentOffset == desiredOffset)
                        {
                            lineIndex = i;
                            break;
                        }
                    }

                    if (lineIndex.HasValue)
                    {
                        break;
                    }

                    currentOffset++; //for the \n removed by Split
                }
            }
            else
            {
                lineIndex = 0;
                charOffset = desiredOffset;
            }

            if (lineIndex.HasValue)
            {
                var textPosition = new TextPosition(lineIndex.Value, charOffset, lines[lineIndex.Value].Substring(charOffset));

                return textPosition;
            }
        }

        return null;
    }

    public static TextPosition? ParseLine(string sourceText, int desiredLine)
    {
        //line is 1-based, convert to 0-based
        desiredLine--;

        if (!string.IsNullOrEmpty(sourceText) && desiredLine > -1)
        {
            string[] lines = sourceText.Split('\n');

            if (desiredLine < lines.Length)
            {
                string line = lines[desiredLine];
                string lineNoLeadingWhitespace = line.TrimStart();

                TextPosition? textPosition;

                if (line.Length == lineNoLeadingWhitespace.Length)
                {
                    int charOffset = line.IndexOf(lineNoLeadingWhitespace, StringComparison.Ordinal);

                    textPosition = new TextPosition(desiredLine, charOffset, lineNoLeadingWhitespace);
                }
                else
                {
                    textPosition = new TextPosition(desiredLine, 0, lineNoLeadingWhitespace);
                }
                return textPosition;
            }
        }

        return null;
    }


    /// <summary>
    /// Human readable text point
    /// </summary>
    /// <remarks>Adds 1 to both <see cref="Line"/> and <see cref="Offset"/> to go from 0-based to 1-based for humans</remarks>
    public override string ToString() => $"({(Line + 1)}:{(Offset + 1)}): {Snippet}";
}