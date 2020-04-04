using System;
using System.Threading.Tasks;

namespace Informers
{
    public class ConsoleInputOutputService : IInputOutputService
    {
        public async Task<EAgreementAnswer> GetAgreementAnswer(string answerText)
        {
            await OutputMessageAsync($"{answerText}? (y/n)");
            var answer = await GetMessage();

            return answer == "y" ? EAgreementAnswer.Yes : EAgreementAnswer.No;
        }

        public Task<string> GetMessage()
        {
            return Task.FromResult(Console.ReadLine());
        }

        public Task<bool> OutputMessageAsync(string message)
        {
            Console.WriteLine(message);

            return Task.FromResult(true);
        }
    }
}
