using JsonRepairSharp;

namespace JsonRepairSharpMinimal
{
    internal class Program
    {
        static void Main(string[] args)
        {
            JsonRepair.ThrowExceptions = true;

            try
            {
                AssertRepair("{name: 'John'}", "{\"name\": \"John\"}");
            }
            catch (JSONRepairError err)
            {
                Console.WriteLine($"Error {err.Message} at position {err.Data["Position"]}");
            }

            Console.WriteLine("Done!");
        }


        private static void AssertRepair(string text, string expected)
        {
            string result = JsonRepair.RepairJson(text);
            if (result == expected)
            {
                Console.WriteLine("PASS: " + text);
                Console.WriteLine("As expected: " + result);
            }
            else
            {
                Console.WriteLine("FAIL: " + text);
                Console.WriteLine("Expected: " + expected);
                Console.WriteLine("Actual: " + result);
            }
        }

    }
}