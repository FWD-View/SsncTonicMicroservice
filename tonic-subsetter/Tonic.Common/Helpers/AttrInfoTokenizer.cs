using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Serilog;

namespace Tonic.Common.Helpers;

public class AttrInfoTokenizer
{
    private bool _exhausted;
    private int _index;
    private readonly string[] _tokens;

    private AttrInfoTokenizer(string attrInfo)
    {
        // We always skip over the first empty string
        _index = 1;
        _tokens = ParseTokens(attrInfo);
        UpdateExhausted();
    }

    private static bool EndsWithOddNumberOfEscapes(string strToCheck)
    {
        if (string.IsNullOrEmpty(strToCheck)) return false;

        var numEscapes = 0;
        var lastChar = strToCheck[^1];
        while (lastChar == '\\' && strToCheck.Length - 1 - numEscapes > 0)
        {
            numEscapes += 1;
            lastChar = strToCheck[strToCheck.Length - 1 - numEscapes];
        }

        return numEscapes % 2 != 0;
    }

    private static string[] ParseTokens(string attrInfo)
    {
        var preEscapedTokens = attrInfo.Split("|");
        var tokens = new List<string>();
        var i = 0;
        while (i < preEscapedTokens.Length)
        {
            var currentToken = preEscapedTokens[i];
            var endsWithOddNumberOfEscapes = EndsWithOddNumberOfEscapes(currentToken);
            if (endsWithOddNumberOfEscapes)
            {
                var sb = new StringBuilder(currentToken);
                while (endsWithOddNumberOfEscapes && i < preEscapedTokens.Length)
                {
                    sb.Append('|');
                    i += 1;
                    currentToken = preEscapedTokens[i];
                    sb.Append(currentToken);
                    endsWithOddNumberOfEscapes = EndsWithOddNumberOfEscapes(currentToken);
                }

                tokens.Add(sb.ToString());
            }
            else
            {
                tokens.Add(currentToken);
            }

            i += 1;
        }

        return tokens.ToArray();
    }


    private void Skip(int numSkips = 1)
    {
        if (_exhausted) throw new Exception("AttrInfo Tokenizer is exhausted");

        var actualNumChomps = Math.Min(_tokens.Length, _index + numSkips) - _index;
        _index += actualNumChomps;
        UpdateExhausted();
    }

    private string ChompValue()
    {
        if (_exhausted) throw new Exception("AttrInfo Tokenizer is exhausted");

        var actualNumChomps = Math.Min(_tokens.Length, _index + 1) - _index;
        var curVal = _tokens[_index];
        _index += actualNumChomps;
        UpdateExhausted();
        return curVal;
    }

    private bool HasMoreTokens => !_exhausted;

    private void UpdateExhausted()
    {
        _exhausted = _index >= _tokens.Length;
    }

    public override string ToString() => $"|{string.Join("|", _tokens)}";

    public static IList<string> ExtractToken(string attrId, string attrInfo)
    {
        var tokenizer = new AttrInfoTokenizer(attrInfo);
        if (!tokenizer.HasMoreTokens) return ImmutableArray<string>.Empty;

        var retval = ImmutableArray.CreateBuilder<string>();

        var versionNumber = Convert.ToInt32(tokenizer.ChompValue());
        if (versionNumber != 2) throw new Exception("Can only generate data for AttrInfo V2");

        try
        {
            while (tokenizer.HasMoreTokens)
            {
                var rowAttrId = tokenizer.ChompValue();

                // Skip ahead 4
                tokenizer.Skip(4);

                var numAttrsStr = tokenizer.ChompValue();
                var numAttrs = string.IsNullOrEmpty(numAttrsStr) ? 0 : Convert.ToInt32(numAttrsStr);

                if (attrId == rowAttrId)
                {
                    // Mask if you hit the actual attribute ID
                    for (var i = 0; i < numAttrs; ++i)
                    {
                        tokenizer.Skip();
                        var attrVal = tokenizer.ChompValue();
                        retval.Add(attrVal);
                        tokenizer.Skip(2);
                    }
                }
                else
                {
                    // Just skip over this attribute or header
                    tokenizer.Skip(numAttrs * 4);
                }
            }
        }
        catch
        {
            Log.Error("Failed to parse ATTR_INFO at {Index}, ATTR_INFO:\n{AttrInfo}", tokenizer._index, attrInfo);
        }

        return retval.ToImmutable();
    }
}