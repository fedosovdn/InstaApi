using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Informers
{
    public class StubInputOutputService : IInputOutputService
    {
        public string UserName { get; set; }

        public Task<EAgreementAnswer> GetAgreementAnswer(string answerText)
        {
            return Task.FromResult(EAgreementAnswer.Yes);
        }

        public Task<string> GetMessage()
        {
            return Task.FromResult(UserName);
        }

        public Task<bool> OutputMessageAsync(string message)
        {
            return Task.FromResult(true);
        }
    }
}
