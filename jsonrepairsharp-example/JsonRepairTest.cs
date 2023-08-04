using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jsonrepairsharp
{
    using System;

    namespace JSONRepair.Test
    {
        public class JsonRepairTests
        {
            static int fails = 0;
            static int passes = 0;
            public static void PerformTest()
            {

                // ParseValidJson_ParseFullJsonObject
                AssertRepair(@"{""a"":2.3e100,""b"":""str"",""c"":null,""d"":false,""e"":[1,2,3]}");

                // ParseValidJson_ParseWhitespace
                AssertRepair("  { \n } \t ");

                // ParseValidJson_ParseObject
                AssertRepair("{}");
                AssertRepair(@"{""a"": {}}");
                AssertRepair(@"{""a"": ""b""}");
                AssertRepair(@"{""a"": 2}");

                // ParseValidJson_ParseArray
                AssertRepair("[]");
                AssertRepair("[{}]");
                AssertRepair(@"{""a"":[]}");
                AssertRepair("[1, \"hi\", true, false, null, {}, []]");

                // ParseValidJson_ParseNumber
                AssertRepair("23");
                AssertRepair("0");
                AssertRepair("0e+2");
                AssertRepair("0.0");
                AssertRepair("-0");
                AssertRepair("2.3");
                AssertRepair("2300e3");
                AssertRepair("2300e+3");
                AssertRepair("2300e-3");
                AssertRepair("-2");
                AssertRepair("2e-3");
                AssertRepair("2.3e-3");

                // ParseValidJson_ParseString
                AssertRepair("\"str\"");
                AssertRepair("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\"");
                AssertRepair("\"\\u260E\"");

                //// ParseValidJson_ParseKeywords
                //AssertRepair("true");
                AssertRepair("false");
                AssertRepair("null");

                // ParseValidJson_CorrectlyHandleStringsEqualToJsonDelimiter
                AssertRepair("\"\"");
                AssertRepair("\"[\"");
                AssertRepair("\"]\"");
                AssertRepair("\"{\"");
                AssertRepair("\"}\"");
                AssertRepair("\":\"");
                AssertRepair("\",\"");

                // ParseValidJson_SupportsUnicodeCharactersInString
                AssertEqual(JsonRepair.RepairJson("\"★\""), "\"★\"");
                AssertEqual(JsonRepair.RepairJson("\"\\u2605\""), "\"\\u2605\"");
                AssertEqual(JsonRepair.RepairJson("\"😀\""), "\"😀\"");
                AssertEqual(JsonRepair.RepairJson("\"\\ud83d\\ude00\""), "\"\\ud83d\\ude00\"");
                AssertEqual(JsonRepair.RepairJson("\"йнформация\""), "\"йнформация\"");

                // ParseValidJson_SupportsEscapedUnicodeCharactersInString
                AssertEqual(JsonRepair.RepairJson("\"\\u2605\""), "\"\\u2605\"");
                AssertEqual(JsonRepair.RepairJson("\"\\ud83d\\ude00\""), "\"\\ud83d\\ude00\"");
                AssertEqual(JsonRepair.RepairJson("\"\\u0439\\u043d\\u0444\\u043e\\u0440\\u043c\\u0430\\u0446\\u0438\\u044f\""), "\"\\u0439\\u043d\\u0444\\u043e\\u0440\\u043c\\u0430\\u0446\\u0438\\u044f\"");

                // ParseValidJson_SupportsUnicodeCharactersInKey
                AssertEqual(JsonRepair.RepairJson("{\"★\":true}"), "{\"★\":true}");
                AssertEqual(JsonRepair.RepairJson("{\"\\u2605\":true}"), "{\"\\u2605\":true}");
                AssertEqual(JsonRepair.RepairJson("{\"😀\":true}"), "{\"😀\":true}");
                AssertEqual(JsonRepair.RepairJson("{\"\\ud83d\\ude00\":true}"), "{\"\\ud83d\\ude00\":true}");

                // RepairInvalidJson_ShouldAddMissingQuotes
                AssertRepair("abc", "\"abc\"");
                AssertRepair("hello   world", "\"hello   world\"");
                AssertRepair("{a:2}", "{\"a\":2}");
                AssertRepair("{a: 2}", "{\"a\": 2}");
                AssertRepair("{2: 2}", "{\"2\": 2}");
                AssertRepair("{true: 2}", "{\"true\": 2}");
                AssertRepair("{\n  a: 2\n}", "{\n  \"a\": 2\n}");
                AssertRepair("[a,b]", "[\"a\",\"b\"]");
                AssertRepair("[\na,\nb\n]", "[\n\"a\",\n\"b\"\n]");

                // RepairInvalidJson_ShouldAddMissingEndQuote
                AssertRepair("\"abc", "\"abc\"");
                AssertRepair("'abc", "\"abc\"");
                AssertRepair("\u2018abc", "\"abc\"");

                // RepairInvalidJson_ShouldReplaceSingleQuotesWithDoubleQuotes
                AssertRepair("{'a':2}", "{\"a\":2}");
                AssertRepair("{'a':'foo'}", "{\"a\":\"foo\"}");
                AssertRepair("{\"a\":'foo'}", "{\"a\":\"foo\"}");
                AssertRepair("{a:'foo',b:'bar'}", "{\"a\":\"foo\",\"b\":\"bar\"}");

                // RepairInvalidJson_ShouldReplaceSpecialQuotesWithDoubleQuotes
                AssertRepair("{“a”:“b”}", "{\"a\":\"b\"}");
                AssertRepair("{‘a’:‘b’}", "{\"a\":\"b\"}");
                AssertRepair("{`a´:`b´}", "{\"a\":\"b\"}");

                // RepairInvalidJson_ShouldNotReplaceSpecialQuotesInsideNormalString
                AssertRepair("\"Rounded “ quote\"", "\"Rounded “ quote\"");

                // RepairInvalidJson_ShouldLeaveStringContentUntouched
                AssertRepair("\"{a:b}\"", "\"{a:b}\"");

                // RepairInvalidJson_ShouldAddOrRemoveEscapeCharacters
                AssertRepair("\"foo'bar\"", "\"foo'bar\"");
                AssertRepair("\"foo\\\"bar\"", "\"foo\\\"bar\"");
                AssertRepair("'foo\"bar'", "\"foo\\\"bar\"");
                AssertRepair("'foo\\'bar'", "\"foo'bar\"");
                AssertRepair("\"foo\\'bar\"", "\"foo'bar\"");
                AssertRepair("\"\\a\"", "\"a\"");

                // RepairInvalidJson_ShouldRepairMissingObjectValue
                AssertRepair("{\"a\":}", "{\"a\":null}");
                AssertRepair("{\"a\":,\"b\":2}", "{\"a\":null,\"b\":2}");
                AssertRepair("{\"a\":", "{\"a\":null}");

                // RepairInvalidJson_ShouldRepairUndefinedValues
                AssertRepair("{\"a\":undefined}", "{\"a\":null}");
                AssertRepair("[undefined]", "[null]");
                AssertRepair("undefined", "null");

                // RepairInvalidJson_ShouldEscapeUnescapedControlCharacters
                AssertRepair("\"hello\\bworld\"", "\"hello\\bworld\"");
                AssertRepair("\"hello\\fworld\"", "\"hello\\fworld\"");
                AssertRepair("\"hello\\nworld\"", "\"hello\\nworld\"");
                AssertRepair("\"hello\\rworld\"", "\"hello\\rworld\"");
                AssertRepair("\"hello\\tworld\"", "\"hello\\tworld\"");
                AssertRepair("{\"value\\n\": \"dc=hcm,dc=com\"}", "{\"value\\n\": \"dc=hcm,dc=com\"}");

                // RepairInvalidJson_ShouldReplaceSpecialWhitespaceCharacters
                AssertRepair("{\"a\":\u00a0\"foo\u00a0bar\"}", "{\"a\": \"foo\u00a0bar\"}");
                AssertRepair("{\"a\":\u202F\"foo\"}", "{\"a\": \"foo\"}");
                AssertRepair("{\"a\":\u205F\"foo\"}", "{\"a\": \"foo\"}");
                AssertRepair("{\"a\":\u3000\"foo\"}", "{\"a\": \"foo\"}");

                // RepairInvalidJson_ShouldReplaceNonNormalizedQuotes
                AssertRepair("\u2018foo\u2019", "\"foo\"");
                AssertRepair("\u201Cfoo\u201D", "\"foo\"");
                AssertRepair("\u0060foo\u00B4", "\"foo\"");

                AssertRepair("\u0060foo'", "\"foo\"");

                AssertRepair("\u0060foo'", "\"foo\"");

                // RepairInvalidJson_ShouldRemoveBlockComments
                AssertRepair("/* foo */ {}", " {}");
                AssertRepair("{} /* foo */ ", "{}  ");
                AssertRepair("{} /* foo ", "{} ");
                AssertRepair("\n/* foo */\n{}", "\n\n{}");
                AssertRepair("{\"a\":\"foo\",/*hello*/\"b\":\"bar\"}", "{\"a\":\"foo\",\"b\":\"bar\"}");

                // RepairInvalidJson_ShouldRemoveLineComments
                AssertRepair("{} // comment", "{} ");
                AssertRepair("{\n\"a\":\"foo\",//hello\n\"b\":\"bar\"\n}", "{\n\"a\":\"foo\",\n\"b\":\"bar\"\n}");

                // RepairInvalidJson_ShouldNotRemoveCommentsInsideString
                AssertRepair("\"/* foo */\"", "\"/* foo */\"");

                // RepairInvalidJson_ShouldStripJsonpNotation
                AssertRepair("callback_123({});", "{}");
                AssertRepair("callback_123([]);", "[]");
                AssertRepair("callback_123(2);", "2");
                AssertRepair("callback_123(\"foo\");", "\"foo\"");
                AssertRepair("callback_123(null);", "null");
                AssertRepair("callback_123(true);", "true");
                AssertRepair("callback_123(false);", "false");
                AssertRepair("callback({}", "{}");
                AssertRepair("/* foo bar */ callback_123 ({})", " {}");
                AssertRepair("/* foo bar */ callback_123 ({})", " {}");                
                AssertRepair("/* foo bar */\ncallback_123({})", "\n\n{}");               //FAILS: Returns "\n{}"
                AssertRepair("/* foo bar */ callback_123 (  {}  )", "   {}  ");
                AssertRepair("  /* foo bar */   callback_123({});  ", "     {}  ");
                AssertRepair("\n/* foo\nbar */\ncallback_123 ({});\n\n", "\n\n{}\n\n");



                //Assert.Throws<JSONRepairError>(() => jsonrepair("callback {}"));

                //RepairInvalidJson_ShouldRepairEscapedStringContents
                AssertRepair("\\\"hello world\\\"", "\"hello world\"");
                AssertRepair("\\\"hello world\\", "\"hello world\"");
                AssertRepair("\\\"hello \\\\\"world\\\\\"\\\"", "\"hello \\\"world\\\"\"");
                AssertRepair("[\\\"hello \\\\\"world\\\\\"\\\"]", "[\"hello \\\"world\\\"\"]");
                AssertRepair("{\\\"stringified\\\": \\\"hello \\\\\"world\\\\\"\\\"}", "{\"stringified\": \"hello \\\"world\\\"\"}");


                
                // the following is weird but understandable
                AssertRepair("[\\\"hello\\, \\\"world\\\"]", "[\"hello, \",\"world\\\\\",\"]\"]"); // FAILS: Returns "[\"hello, \",\"world\\\",\"]\"]" 

                // the following is sort of invalid: the end quote should be escaped too,
                // but the fixed result is most likely what you want in the end
                AssertRepair("\\\"hello\"", "\"hello\"");

                // RepairInvalidJson_ShouldStripTrailingCommasFromArray
                AssertRepair("[1,2,3,]", "[1,2,3]");
                AssertRepair("[1,2,3,\n]", "[1,2,3\n]");
                AssertRepair("[1,2,3,  \n  ]", "[1,2,3  \n  ]");
                AssertRepair("{\"array\":[1,2,3,]}", "{\"array\":[1,2,3]}");

                AssertRepair("\"[1,2,3,]\"", "\"[1,2,3,]\"");

                // RepairInvalidJson_ShouldStripTrailingCommasFromObject
                AssertRepair("{\"a\":2,}", "{\"a\":2}");
                AssertRepair("{\"a\":2  ,  }", "{\"a\":2    }");
                AssertRepair("{\"a\":2  , \n }", "{\"a\":2   \n }");
                AssertRepair("{\"a\":2/*foo*/,/*foo*/}", "{\"a\":2}");

                AssertRepair("\"{a:2,}\"", "\"{a:2,}\"");

                // RepairInvalidJson_ShouldStripTrailingCommaAtTheEnd
                AssertRepair("4,", "4");
                AssertRepair("4 ,", "4 ");
                AssertRepair("4 , ", "4  ");
                AssertRepair("{\"a\":2},", "{\"a\":2}");
                AssertRepair("[1,2,3],", "[1,2,3]");

                // RepairInvalidJson_ShouldAddMissingClosingBracketForObject
                AssertRepair("{", "{}");
                AssertRepair("{\"a\":2", "{\"a\":2}");
                AssertRepair("{\"a\":2,", "{\"a\":2}");
                AssertRepair("{\"a\":{\"b\":2}", "{\"a\":{\"b\":2}}");
                AssertRepair("{\n  \"a\":{\"b\":2\n}", "{\n  \"a\":{\"b\":2\n}}");
                AssertRepair("[{\"b\":2]", "[{\"b\":2}]");
                AssertRepair("[{\"b\":2\n]", "[{\"b\":2}\n]");
                AssertRepair("[{\"i\":1{\"i\":2}]", "[{\"i\":1},{\"i\":2}]");
                AssertRepair("[{\"i\":1,{\"i\":2}]", "[{\"i\":1},{\"i\":2}]");

                // RepairInvalidJson_ShouldAddMissingClosingBracketForArray
                AssertRepair("[", "[]");
                AssertRepair("[1,2,3", "[1,2,3]");
                AssertRepair("[1,2,3,", "[1,2,3]");
                AssertRepair("[[1,2,3,", "[[1,2,3]]");
                AssertRepair("{\n\"values\":[1,2,3\n}", "{\n\"values\":[1,2,3]\n}");
                AssertRepair("{\n\"values\":[1,2,3\n", "{\n\"values\":[1,2,3]}\n");

                // RepairInvalidJson_ShouldStripMongoDbDataTypes
                string mongoDocument = "{\n" +
                    "   \"_id\" : ObjectId(\"123\"),\n" +
                    "   \"isoDate\" : ISODate(\"2012-12-19T06:01:17.171Z\"),\n" +
                    "   \"regularNumber\" : 67,\n" +
                    "   \"long\" : NumberLong(\"2\"),\n" +
                    "   \"long2\" : NumberLong(2),\n" +
                    "   \"int\" : NumberInt(\"3\"),\n" +
                    "   \"int2\" : NumberInt(3),\n" +
                    "   \"decimal\" : NumberDecimal(\"4\"),\n" +
                    "   \"decimal2\" : NumberDecimal(4)\n" +
                    "}";

                string expectedJson = "{\n" +
                    "   \"_id\" : \"123\",\n" +
                    "   \"isoDate\" : \"2012-12-19T06:01:17.171Z\",\n" +
                    "   \"regularNumber\" : 67,\n" +
                    "   \"long\" : \"2\",\n" +
                    "   \"long2\" : 2,\n" +
                    "   \"int\" : \"3\",\n" +
                    "   \"int2\" : 3,\n" +
                    "   \"decimal\" : \"4\",\n" +
                    "   \"decimal2\" : 4\n" +
                    "}";

                AssertEqual(JsonRepair.RepairJson(mongoDocument), expectedJson);

                // RepairInvalidJson_ShouldReplacePythonConstants
                AssertRepair("True", "true");
                AssertRepair("False", "false");
                AssertRepair("None", "null");

                // RepairInvalidJson_ShouldTurnUnknownSymbolsIntoAString
                AssertRepair("foo", "\"foo\"");
                AssertRepair("[1,foo,4]", "[1,\"foo\",4]");
                AssertRepair("{foo: bar}", "{\"foo\": \"bar\"}");

                AssertRepair("foo 2 bar", "\"foo 2 bar\"");
                AssertRepair("{greeting: hello world}", "{\"greeting\": \"hello world\"}");
                AssertRepair("{greeting: hello world\nnext: \"line\"}", "{\"greeting\": \"hello world\",\n\"next\": \"line\"}");
                AssertRepair("{greeting: hello world!}", "{\"greeting\": \"hello world!\"}");

                // RepairInvalidJson_ShouldConcatenateStrings
                AssertRepair("\"hello\" + \" world\"", "\"hello world\"");
                AssertRepair("\"hello\" +\n \" world\"", "\"hello world\"");
                AssertRepair("\"a\"+\"b\"+\"c\"", "\"abc\"");
                AssertRepair("\"hello\" + /*comment*/ \" world\"", "\"hello world\"");
                AssertRepair("{\n  \"greeting\": 'hello' +\n 'world'\n}", "{\n  \"greeting\": \"helloworld\"\n}");

                // RepairInvalidJson_ShouldRepairMissingCommaBetweenArrayItems
                AssertRepair("{\"array\": [{}{}]}", "{\"array\": [{},{}]}");
                AssertRepair("{\"array\": [{} {}]}", "{\"array\": [{}, {}]}");
                AssertRepair("{\"array\": [{}\n{}]}", "{\"array\": [{},\n{}]}");
                AssertRepair("{\"array\": [\n{}\n{}\n]}", "{\"array\": [\n{},\n{}\n]}");
                AssertRepair("{\"array\": [\n1\n2\n]}", "{\"array\": [\n1,\n2\n]}");
                AssertRepair("{\"array\": [\n\"a\"\n\"b\"\n]}", "{\"array\": [\n\"a\",\n\"b\"\n]}");

                AssertRepair("[\n{},\n{}\n]", "[\n{},\n{}\n]");

                Console.WriteLine($"passed {passes}, failed: {fails}");
            }

            private static void AssertRepair(string text)
            {
                AssertRepair(text, text);
            }

            private static void AssertRepair(string text, string expected)
            {
                string result = JsonRepair.RepairJson(text);
                if (result == expected)
                {
                    passes++;
                    Console.WriteLine("PASS: " + text);
                }
                else
                {
                    fails++;
                    Console.WriteLine("FAIL: " + text);
                    Console.WriteLine("Expected: " + expected);
                    Console.WriteLine("Actual: " + result);
                }
            }

            private static void AssertEqual(string text, string expected)
            {
                if (text == expected)
                {
                    passes++;
                    Console.WriteLine("PASS: " + text);
                }
                else
                {
                    fails++;
                    Console.WriteLine("FAIL: " + text);
                    Console.WriteLine("Expected: " + expected);
                }

            }
            
        }
    }
}