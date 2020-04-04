using System.Threading.Tasks;

namespace Informers
{
    public interface IInputOutputService
    {
        Task<string> GetMessage();

        Task<bool> OutputMessageAsync(string message);

        Task<EAgreementAnswer> GetAgreementAnswer(string answerText);
    }
}
