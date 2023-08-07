using jsonrepairsharp.JSONRepair.Test;

namespace jsonrepairsharp
{
    internal class Program
    {
        static void Main(string[] args)
        {

            

            JsonRepairTests.PerformTest();
            Console.WriteLine("Done!");


            //Console.WriteLine("Hello, World!");
            //try
            //{
            //    string json = @"{greeting: hello world\nnext: 'line\'}"; //@" {name: 'John'}";
            //    string repaired = JsonRepair.RepairJson(json);
            //    Console.WriteLine(repaired);
            //    // Output: {"name": "John"}
            //}
            //catch (JSONRepairError err)
            //{
            //    Console.WriteLine(err.Message);
            //    Console.WriteLine("Position: " + err.Data["Position"]);
            //}
        }
    }
}