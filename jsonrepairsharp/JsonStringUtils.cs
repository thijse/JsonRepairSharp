using System.Text.RegularExpressions;

public static class StringUtils
{
    public const int CodeBackslash                = 0x5c; // "\"
    public const int CodeSlash                    = 0x2f; // "/"
    public const int CodeAsterisk                 = 0x2a; // "*"
    public const int CodeOpeningBrace             = 0x7b; // "{"
    public const int CodeClosingBrace             = 0x7d; // "}"
    public const int CodeOpeningBracket           = 0x5b; // "["
    public const int CodeClosingBracket           = 0x5d; // "]"
    public const int CodeOpenParenthesis          = 0x28; // "("
    public const int CodeCloseParenthesis         = 0x29; // ")"
    public const int CodeSpace                    = 0x20; // " "
    public const int CodeNewline                  = 0xa; // "\n"
    public const int CodeTab                      = 0x9; // "\t"
    public const int CodeReturn                   = 0xd; // "\r"
    public const int CodeBackspace                = 0x08; // "\b"
    public const int CodeFormFeed                 = 0x0c; // "\f"
    public const int CodeDoubleQuote              = 0x0022; // "
    public const int CodePlus                     = 0x2b; // "+"
    public const int CodeMinus                    = 0x2d; // "-"
    public const int CodeQuote                    = 0x27; // "'"
    public const int CodeZero                     = 0x30;
    public const int CodeOne                      = 0x31;
    public const int CodeNine                     = 0x39;
    public const int CodeComma                    = 0x2c; // ","
    public const int CodeDot                      = 0x2e; // "." (dot, period)
    public const int CodeColon                    = 0x3a; // ":"
    public const int CodeSemicolon                = 0x3b; // ";"
    public const int CodeUppercaseA               = 0x41; // "A"
    public const int CodeLowercaseA               = 0x61; // "a"
    public const int CodeUppercaseE               = 0x45; // "E"
    public const int CodeLowercaseE               = 0x65; // "e"
    public const int CodeUppercaseF               = 0x46; // "F"
    public const int CodeLowercaseF               = 0x66; // "f"
    private const int CodeNonBreakingSpace        = 0xa0;
    private const int CodeEnQuad                  = 0x2000;
    private const int CodeHairSpace               = 0x200a;
    private const int CodeNarrowNoBreakSpace      = 0x202f;
    private const int CodeMediumMathematicalSpace = 0x205f;
    private const int CodeIdeographicSpace        = 0x3000;
    private const int CodeDoubleQuoteLeft         = 0x201c; // “
    private const int CodeDoubleQuoteRight        = 0x201d; // ”
    private const int CodeQuoteLeft               = 0x2018; // ‘
    private const int CodeQuoteRight              = 0x2019; // ’
    private const int CodeGraveAccent             = 0x0060; // `
    private const int CodeAcuteAccent             = 0x00b4; // ´


    private static readonly string EscapedDelimeters = Regex.Escape(@",:[]{}()\n");
    private static readonly string StartOfValue      = Regex.Escape(@"[{\w-");
    private static readonly Regex RegexDelimiter     = new Regex(@"^[,:\[\]\{\}\(\)\n]$");   //$"^[{EscapedDelimeters}\n]$"); //new Regex("^[,:\\[\\]{}()\n]$");
    private static readonly Regex RegexStartOfValue  = new Regex(@"^[\[\{\w-]$");                // $"^[{StartOfValue}]$");//new Regex("^[[{\\w-]$");


    public static bool IsHex(int code)
    {

        
        return (code >= CodeZero       && code <= CodeNine)       ||
               (code >= CodeUppercaseA && code <= CodeUppercaseF) ||
               (code >= CodeLowercaseA && code <= CodeLowercaseF);
    }

    public static bool IsDigit(int code)
    {
        return code >= CodeZero && code <= CodeNine;
    }

    public static bool IsNonZeroDigit(int code)
    {
        return code >= CodeOne && code <= CodeNine;
    }

    public static bool IsValidStringCharacter(int code)
    {
        return code >= 0x20 && code <= 0x10ffff;
    }

    public static bool IsDelimiter(string character)
    {
        return RegexDelimiter.IsMatch(character) || (!string.IsNullOrEmpty(character) && IsQuote(character[0]));
    }

    public static bool IsStartOfValue(string character)
    {
        return RegexStartOfValue.IsMatch(character) || (!string.IsNullOrEmpty(character) && IsQuote(character[0]));
    }

    public static bool IsControlCharacter(int code)
    {
        return code == CodeNewline   ||
               code == CodeReturn    ||
               code == CodeTab       ||
               code == CodeBackspace ||
               code == CodeFormFeed;
    }

    public static bool IsWhitespace(int code)
    {
        return code == CodeSpace || code == CodeNewline || code == CodeTab || code == CodeReturn;
    }

    public static bool IsSpecialWhitespace(int code)
    {
        return code == CodeNonBreakingSpace                  ||
               (code >= CodeEnQuad && code <= CodeHairSpace) ||
               code == CodeNarrowNoBreakSpace                ||
               code == CodeMediumMathematicalSpace           ||
               code == CodeIdeographicSpace;
    }

    public static bool IsQuote(int code)
    {
        return IsDoubleQuoteLike(code) || IsSingleQuoteLike(code);
    }

    public static bool IsDoubleQuoteLike(int code)
    {
        return code == CodeDoubleQuote || code == CodeDoubleQuoteLeft || code == CodeDoubleQuoteRight;
    }

    public static bool IsDoubleQuote(int code)
    {
        return code == CodeDoubleQuote;
    }

    public static bool IsSingleQuoteLike(int code)
    {
        return code == CodeQuote       ||
               code == CodeQuoteLeft   ||
               code == CodeQuoteRight  ||
               code == CodeGraveAccent ||
               code == CodeAcuteAccent;
    }

    //public static bool IsEndQuote(int code)
    //{
    //    return
    //        IsSingleQuoteLike(code) ?
    //        IsSingleQuoteLike(code) :
    //        IsDoubleQuote(code)     ?
    //        IsDoubleQuote(code)     :
    //        IsDoubleQuoteLike(code);
    //}

    public static bool IsMatchingEndQuote(int code, int startCode)
    {
        return
            IsSingleQuoteLike(startCode) ?
            IsSingleQuoteLike(code) :
            IsDoubleQuote(startCode) ?
            IsDoubleQuote(code) :
            IsDoubleQuoteLike(startCode) ? 
            IsDoubleQuoteLike(code) :
            false;
    }

    public static string StripLastOccurrence(string text, string textToStrip, bool stripRemainingText = false)
    {
        var index = text.LastIndexOf(textToStrip, StringComparison.Ordinal);
        return index != -1
            ? text.Substring(0, index) + (stripRemainingText ? string.Empty : text.Substring(index + 1))
            : text;
    }

    public static string InsertBeforeLastWhitespace(string text, string textToInsert)
    {
        var index = text.Length;

        if (!IsWhitespace(text[index - 1]))
        {
            // no trailing whitespaces
            return text + textToInsert;
        }

        while (IsWhitespace(text[index - 1]))
        {
            index--;
        }

        return text.Substring(0, index) + textToInsert + text.Substring(index);
    }

    public static string RemoveAtIndex(string text, int start, int count)
    {
        return text.Substring(0, start) + text.Substring(start + count);
    }

    public static bool EndsWithCommaOrNewline(string text)
    {
        return Regex.IsMatch(text, @"[,\n][ \t\r]*$");
    }
}

public static class StringExtensions
{
    public static char CharCodeAt(this string str, int i)
    {
        if (i < 0 || i >= str.Length) return '\0';
        return str[i];
    }
    public static string StringAt(this string str, int i)
    {
        if (i < 0 || i >= str.Length) return "";
        return str[i].ToString();
    }

    public static string SubstringSafe(this string str, int startIndex, int length)
    {
        return str.Substring(startIndex, Math.Min(length, str.Length - startIndex));
    }
}