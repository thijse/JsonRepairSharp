using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class JsonRepair
{
    /// <summary>
    /// Dictionary of control characters and their corresponding escape sequences.
    /// </summary>
    private static readonly Dictionary<char, string> ControlCharacters = new Dictionary<char, string>
    {
        { '\b', "\b" },
        { '\f', "\f" },
        { '\n', "\n" },
        { '\r', "\r" },
        { '\t', "\t" }
    };

    /// <summary>
    /// Dictionary of escape characters and their corresponding escape sequences.
    /// </summary>
    private static readonly Dictionary<char, string> EscapeCharacters = new Dictionary<char, string>
    {
        { '\"', "\"" },
        { '\\', "\\" },
        { '/' , "/"  },
        { 'b' , "\b" },
        { 'f' , "\f" },
        { 'n' , "\n" },
        { 'r' , "\r" },
        { 't' , "\t" }
    };

    /*
       * Repair a string containing an invalid JSON document.
       * For example output from a LLM model
       *
       * Example:
        try
        {
            string json = "{name: 'John'}";
            string repaired = JSONRepair.JsonRepair(json);
            Console.WriteLine(repaired);
            // Output: {"name": "John"}
        }
        catch (JSONRepairError err)
        {
            Console.WriteLine(err.Message);
            Console.WriteLine("Position: " + err.Data["Position"]);
        }
     */

    /// <summary>
    /// Repairs a string containing an invalid JSON document.
    /// </summary>
    /// <param name="text">The JSON document to repair</param>
    /// <returns>The repaired JSON document</returns>
    /// <exception cref="JSONRepairError">Thrown when an error occurs during JSON repair</exception>
    public static string RepairJson(string text)
    {
        int i         = 0 ; // current index in text
        string output = ""; // generated output

        var processed = ParseValue();
        if (!processed) { ThrowUnexpectedEnd(); }

        var processedComma = ParseCharacter(StringUtils.CodeComma);
        if (processedComma)
        {
            ParseWhitespaceAndSkipComments();
        }

        if (StringUtils.IsStartOfValue(text.CharCodeAt(i).ToString()) && StringUtils.EndsWithCommaOrNewline(output))
        {
            // start of a new value after end of the root level object: looks like
            // newline delimited JSON -> turn into a root level array
            if (!processedComma)
            {
                // repair missing comma
                output = StringUtils.InsertBeforeLastWhitespace(output, ",");
            }

            ParseNewlineDelimitedJson();
        }
        else if (processedComma)
        {
            // repair: remove trailing comma
            output = StringUtils.StripLastOccurrence(output, ",");
        }

        if (i >= text.Length)
        {
            // reached the end of the document properly
            return output;
        }

        ThrowUnexpectedCharacter();

        /// <summary>
        /// Parses a JSON value.
        /// </summary>
        /// <returns>True if a value was parsed, false otherwise</returns>
        bool ParseValue()
        {
            ParseWhitespaceAndSkipComments();
            bool processed =
                ParseObject()         ||
                ParseArray()          ||
                ParseString()         ||
                ParseNumber()         ||
                ParseKeywords()       ||
                ParseUnquotedString();
            ParseWhitespaceAndSkipComments();

            return processed;
        }

        /// <summary>
        /// Parses and repairs whitespace in the JSON document.
        /// </summary>
        /// <returns>True if any whitespace was parsed and repaired, false otherwise</returns>
        bool ParseWhitespaceAndSkipComments()
        {
            var start = i;
            if (i >= text.Length) return false;

            var changed = ParseWhitespace();
            do
            {
                changed = ParseComment();
                if (changed)
                {
                    changed = ParseWhitespace();
                }
            } while (changed);

            return i > start;
        }

        /// <summary>
        /// Parses and repairs whitespace in the JSON document.
        /// </summary>
        /// <returns>True if any whitespace was parsed and repaired, false otherwise</returns>
        bool ParseWhitespace()
        {
            string whitespace = "";
            bool normal;
            while ((normal = StringUtils.IsWhitespace(text.CharCodeAt(i))) || StringUtils.IsSpecialWhitespace(text.CharCodeAt(i)))
            {
                if (normal)
                {
                    whitespace += text.CharCodeAt(i);
                }
                else
                {
                    // repair special whitespace
                    whitespace += " ";
                }

                i++;
            }

            if (whitespace.Length > 0)
            {
                output += whitespace;
                return true;
            }

            return false;
        }

        bool ParseComment()
        {
            // find a block comment '/* ... */'
            if (text.CharCodeAt(i) == StringUtils.CodeSlash && text.CharCodeAt(i+1) == StringUtils.CodeAsterisk)
            {
                // repair block comment by skipping it
                while (i < text.Length && !AtEndOfBlockComment(text, i))
                {
                    i++;
                }
                i += 2;

                return true;
            }

            // find a line comment '// ...'
            if (text.CharCodeAt(i) == StringUtils.CodeSlash && text.CharCodeAt(i + 1) == StringUtils.CodeSlash)
            {
                // repair line comment by skipping it
                while (i < text.Length && text.CharCodeAt(i) != StringUtils.CodeNewline)
                {
                    i++;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses a JSON character.
        /// </summary>
        /// <param name="code">The character code to parse</param>
        /// <returns>True if the character was parsed, false otherwise</returns>
        bool ParseCharacter(int code)
        {
            if (text.CharCodeAt(i) == code)
            {
                output += text.CharCodeAt(i);
                i++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Skips a JSON character.
        /// </summary>
        /// <param name="code">The character code to skip</param>
        /// <returns>True if the character was skipped, false otherwise</returns>
        bool SkipCharacter(int code)
        {
            if (text.CharCodeAt(i) == code)
            {
                i++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Skips a JSON escape character.
        /// </summary>
        /// <returns>True if the escape character was skipped, false otherwise</returns>
        bool SkipEscapeCharacter()
        {
            return SkipCharacter(StringUtils.CodeBackslash);
        }

        /// <summary>
        /// Parses a JSON object.
        /// </summary>
        /// <returns>True if an object was parsed, false otherwise</returns>
        bool ParseObject()
        {
            if (text.CharCodeAt(i) == StringUtils.CodeOpeningBrace)
            {
                output += "{";
                i++;
                ParseWhitespaceAndSkipComments();

                bool initial = true;
                while (i < text.Length && text.CharCodeAt(i) != StringUtils.CodeClosingBrace)
                {
                    bool processedComma;
                    if (!initial)
                    {
                        processedComma = ParseCharacter(StringUtils.CodeComma);
                        if (!processedComma)
                        {
                            // repair missing comma
                            output = StringUtils.InsertBeforeLastWhitespace(output, ",");
                        }
                        ParseWhitespaceAndSkipComments();
                    }
                    else
                    {
                        processedComma = true;
                        initial        = false;
                    }

                    bool processedKey = ParseString() || ParseUnquotedString();
                    if (!processedKey)
                    {
                        if (
                            text.CharCodeAt(i) == StringUtils.CodeClosingBrace   ||
                            text.CharCodeAt(i) == StringUtils.CodeOpeningBrace   ||
                            text.CharCodeAt(i) == StringUtils.CodeClosingBracket ||
                            text.CharCodeAt(i) == StringUtils.CodeOpeningBracket ||
                            text.CharCodeAt(i) == '\0'
                        )
                        {
                            // repair trailing comma
                            output = StringUtils.StripLastOccurrence(output, ",");
                        }
                        else
                        {
                            ThrowObjectKeyExpected();
                        }
                        break;
                    }

                    ParseWhitespaceAndSkipComments();
                    bool processedColon = ParseCharacter(StringUtils.CodeColon);
                    if (!processedColon)
                    {
                        if (StringUtils.IsStartOfValue(text.CharCodeAt(i).ToString()))
                        {
                            // repair missing colon
                            output = StringUtils.InsertBeforeLastWhitespace(output, ":");
                        }
                        else
                        {
                            ThrowColonExpected();
                        }
                    }
                    bool processedValue = ParseValue();
                    if (!processedValue)
                    {
                        if (processedColon)
                        {
                            // repair missing object value
                            output += "null";
                        }
                        else
                        {
                            ThrowColonExpected();
                        }
                    }
                }

                if (text.CharCodeAt(i) == StringUtils.CodeClosingBrace)
                {
                    output += "}";
                    i++;
                }
                else
                {
                    // repair missing end bracket
                    output = StringUtils.InsertBeforeLastWhitespace(output, "}");
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses a JSON array.
        /// </summary>
        /// <returns>True if an array was parsed, false otherwise</returns>
        bool ParseArray()
        {
            if (text.CharCodeAt(i) == StringUtils.CodeOpeningBracket)
            {
                output += "[";
                i++;
                ParseWhitespaceAndSkipComments();

                bool initial = true;
                while (i < text.Length && text.CharCodeAt(i) != StringUtils.CodeClosingBracket)
                {
                    if (!initial)
                    {
                        bool processedComma = ParseCharacter(StringUtils.CodeComma);
                        if (!processedComma)
                        {
                            // repair missing comma
                            output = StringUtils.InsertBeforeLastWhitespace(output, ",");
                        }
                    }
                    else
                    {
                        initial = false;
                    }

                    bool processedValue = ParseValue();
                    if (!processedValue)
                    {
                        // repair trailing comma
                        output = StringUtils.StripLastOccurrence(output, ",");
                        break;
                    }
                }

                if (text.CharCodeAt(i) == StringUtils.CodeClosingBracket)
                {
                    output += "]";
                    i++;
                }
                else
                {
                    // repair missing closing array bracket
                    output = StringUtils.InsertBeforeLastWhitespace(output, "]");
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses and repairs Newline Delimited JSON (NDJSON): multiple JSON objects separated by a newline character.
        /// </summary>
        void ParseNewlineDelimitedJson()
        {
            output = "[" + output.TrimEnd() + "]";
            output = output.Replace("\n", ",");

            while (i < text.Length && StringUtils.IsWhitespace(text.CharCodeAt(i)))
            {
                i++;
            }

            while (i < text.Length)
            {
                ParseValue();
                if (i < text.Length && text.CharCodeAt(i) == StringUtils.CodeComma)
                {
                    i++;
                }
                while (i < text.Length && StringUtils.IsWhitespace(text.CharCodeAt(i)))
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Parses a JSON string.
        /// </summary>
        /// <returns>True if a string was parsed, false otherwise</returns>
        bool ParseString()
        {
            bool skipEscapeChars = text.CharCodeAt(i) == StringUtils.CodeBackslash;
            if (skipEscapeChars)
            {
                // repair: remove the first escape character
                i++;
                skipEscapeChars = true;
            }

            if (StringUtils.IsQuote(text.CharCodeAt(i)))
            {
                var startQuote = text.CharCodeAt(i);
                output        += "\"";
                i             ++;

                while (i < text.Length && !StringUtils.IsMatchingEndQuote(text.CharCodeAt(i), startQuote))
                {
                    if (text.CharCodeAt(i) == StringUtils.CodeBackslash)
                    {
                        var character = text.CharCodeAt(i+1);
                        var escapeChar = EscapeCharacters.GetValueOrDefault(character);
                        if (escapeChar != null)
                        {
                            output += text.Substring(i, 2);
                            i += 2;
                        }
                        else if (character == 'u')
                        {
                            if (
                                StringUtils.IsHex(text.CharCodeAt(i + 2)) &&
                                StringUtils.IsHex(text.CharCodeAt(i + 3)) &&
                                StringUtils.IsHex(text.CharCodeAt(i + 4)) &&
                                StringUtils.IsHex(text.CharCodeAt(i + 5))
                            )
                            {
                                output += text.Substring(i, 6);
                                i += 6;
                            }
                            else
                            {
                                ThrowInvalidUnicodeCharacter(i);
                            }
                        }
                        else
                        {
                            // repair invalid escape character: remove it
                            output += character;
                            i      += 2;
                        }
                    }
                    else
                    {
                        char character = text.CharCodeAt(i);
                        int code       = text.CharCodeAt(i);

                        if (code == StringUtils.CodeDoubleQuote && text.CharCodeAt(i - 1) != StringUtils.CodeBackslash)
                        {
                            // repair unescaped double quote
                            output += "\\" + character;
                            i++;
                        }
                        else if (StringUtils.IsControlCharacter(character))
                        {
                            // unescaped control character
                            output += ControlCharacters[character];
                            i++;
                        }
                        else
                        {
                            if (!StringUtils.IsValidStringCharacter(code))
                            {
                                ThrowInvalidCharacter(character);
                            }
                            output += character;
                            i++;
                        }
                    }

                    if (skipEscapeChars)
                    {
                        bool processed = SkipEscapeCharacter();
                        if (processed)
                        {
                            // repair: skipped escape character (nothing to do)
                        }
                    }
                }

                if (StringUtils.IsQuote(text.CharCodeAt(i)))
                {
                    if (text.CharCodeAt(i) != StringUtils.CodeDoubleQuote)
                    {
                        // repair non-normalized quote. todo?
                    }
                    output += "\"";
                    i++;
                }
                else
                {
                    // repair missing end quote
                    output += "\"";
                }

                ParseConcatenatedString();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses and repairs concatenated JSON strings in the JSON document.
        /// </summary>
        /// <returns>True if any concatenated strings were parsed and repaired, false otherwise</returns>
        bool ParseConcatenatedString()
        {
            bool processed = false;

            ParseWhitespaceAndSkipComments();
            while (text.CharCodeAt(i) == StringUtils.CodePlus)
            {
                processed = true;
                i++;
                ParseWhitespaceAndSkipComments();

                // repair: remove the end quote of the first string
                output = StringUtils.StripLastOccurrence(output, "\"", true);
                var start = output.Length;
                ParseString();

                // repair: remove the start quote of the second string
                output = StringUtils.RemoveAtIndex(output, start, 1);
            }

            return processed;
        }

        /// <summary>
        /// Parses a JSON number.
        /// </summary>
        /// <returns>True if a number was parsed, false otherwise</returns>
        bool ParseNumber()
        {
            int start = i;
            if (text.CharCodeAt(i) == StringUtils.CodeMinus)
            {
                i++;
                if (ExpectDigitOrRepair(start))
                {
                    return true;
                }
            }

            if (text.CharCodeAt(i) == StringUtils.CodeZero)
            {
                i++;
            }
            else if (StringUtils.IsNonZeroDigit(text.CharCodeAt(i)))
            {
                i++;
                while (StringUtils.IsDigit(text.CharCodeAt(i)))
                {
                    i++;
                }
            }

            if (text.CharCodeAt(i) == StringUtils.CodeDot)
            {
                i++;
                if (ExpectDigitOrRepair(start))
                {
                    return true;
                }
                while (StringUtils.IsDigit(text.CharCodeAt(i)))
                {
                    i++;
                }
            }

            if (text.CharCodeAt(i) == StringUtils.CodeLowercaseE || text.CharCodeAt(i) == StringUtils.CodeUppercaseE)
            {
                i++;
                if (text.CharCodeAt(i) == StringUtils.CodeMinus || text.CharCodeAt(i) == StringUtils.CodePlus)
                {
                    i++;
                }
                if (ExpectDigitOrRepair(start))
                {
                    return true;
                }
                while (StringUtils.IsDigit(text.CharCodeAt(i)))
                {
                    i++;
                }
            }

            if (i > start)
            {
                output += text.Substring(start, i - start);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses and repairs JSON keywords (true, false, null) in the JSON document.
        /// </summary>
        /// <returns>True if a keyword was parsed and repaired, false otherwise</returns>
        bool ParseKeywords()
        {
            return
                ParseKeyword("true" , "true")  ||
                ParseKeyword("false", "false") ||
                ParseKeyword("null" , "null")  ||
                // repair Python keywords True, False, None
                ParseKeyword("True" , "true")  ||
                ParseKeyword("False", "false") ||
                ParseKeyword("None" , "null");
        }

        /// <summary>
        /// Parses a specific JSON keyword.
        /// </summary>
        /// <param name="name">The name of the keyword</param>
        /// <param name="value">The repaired value of the keyword</param>
        /// <returns>True if the keyword was parsed and repaired, false otherwise</returns>
        bool ParseKeyword(string name, string value)
        {
            if (text.SubstringSafe(i, name.Length) == name)
            {
                output += value;
                i      += name.Length;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses an unquoted JSON string or a function call.
        /// </summary>
        /// <returns>True if an unquoted string or a function call was parsed, false otherwise</returns>
        bool ParseUnquotedString()
        {
            // note that the symbol can end with whitespaces: we stop at the next delimiter
            int start = i;
            while (i < text.Length && !StringUtils.IsDelimiter(text.CharCodeAt(i).ToString()))
            {
                i++;
            }

            if (i > start)
            {
                if (text.CharCodeAt(i) == StringUtils.CodeOpenParenthesis)
                {
                    // repair a MongoDB function call like NumberLong("2")
                    // repair a JSONP function call like callback({...});
                    i++;

                    ParseValue();

                    if (text.CharCodeAt(i) == StringUtils.CodeCloseParenthesis)
                    {
                        // repair: skip close bracket of function call
                        i++;
                        if (text.CharCodeAt(i) == StringUtils.CodeSemicolon)
                        {
                            // repair: skip semicolon after JSONP call
                            i++;
                        }
                    }

                    return true;
                }
                else
                {
                    // repair unquoted string

                    // first, go back to prevent getting trailing whitespaces in the string
                    while (StringUtils.IsWhitespace(text.CharCodeAt(i-1)) && i > 0)
                    {
                        i--;
                    }

                    string symbol = text.Substring(start, i - start);
                    output += symbol == "undefined" ? "null" : $"\"{symbol}\"";

                    return true;
                }
            }
            return false;
        }

        void ExpectDigit(int start)
        {
            if (!StringUtils.IsDigit(text.CharCodeAt(i)))
            {
                string numSoFar = text.Substring(start, i - start);
                throw new JSONRepairError($"Invalid number '{numSoFar}', expecting a digit {Got()}", 2);
            }
        }

        bool ExpectDigitOrRepair(int start)
        {
            if (i >= text.Length)
            {
                // repair numbers cut off at the end
                // this will only be called when we end after a '.', '-', or 'e' and does not
                // change the number more than it needs to make it valid JSON
                output += text.Substring(start, i - start) + "0";
                return true;
            }
            else
            {
                ExpectDigit(start);
                return false;
            }
        }


        void ThrowInvalidCharacter(char character)
        {
            //throw new JSONRepairError($"Invalid character {character}", i);
        }

        void ThrowUnexpectedCharacter()
        {
            //throw new JSONRepairError($"Unexpected character {text.CharCodeAt(i)}", i);
        }

        void ThrowUnexpectedEnd()
        {
            //throw new JSONRepairError("Unexpected end of json string", text.Length);
        }

        void ThrowObjectKeyExpected()
        {
            //throw new JSONRepairError("Object key expected", i);
        }

        void ThrowColonExpected()
        {
            //throw new JSONRepairError("Colon expected", i);
        }

        void ThrowInvalidUnicodeCharacter(int start)
        {
            int end = start + 2;
            while (Regex.IsMatch(text[end].ToString(), @"\w"))
            {
                end++;
            }
            string chars = text.Substring(start, end - start);
            //throw new JSONRepairError($"Invalid unicode character \"{chars}\"", i);
        }

        string Got()
        {
            return text.CharCodeAt(i) != '\0' ? $"but got '{text.CharCodeAt(i)}'" : "but reached end of input";
        }

        bool AtEndOfBlockComment(string text, int i)
        {
            return text.CharCodeAt(i) == '*' && text[i + 1] == '/';
        }

        return output;
    }

}

/// <summary>
/// Represents an error that occurs during JSON repair.
/// </summary>
public class JSONRepairError : Exception
{
    /// <summary>
    /// The index at which the error occurred.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JSONRepairError"/> class with a specified error message and index.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="index">The index at which the error occurred.</param>
    public JSONRepairError(string message, int index) : base(message)
    {
        Index = index;
    }
}