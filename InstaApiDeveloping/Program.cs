using System;
using System.Threading.Tasks;

namespace InstaApiDeveloping
{
    class Program
    {
        public static async Task Main()
        {
            //new StartUp().Install();

            await ExecuteAsync();

            Console.ReadKey();
        }

        static async Task ExecuteAsync()
        {
            var justForTest = new JustForTestApi();

            var result = await justForTest.TestingFunctionalityAsync();

            Console.WriteLine(result);
        }
    }
}
