using System.Collections.Generic;

namespace Insta
{
    public class OperationResult
    {
        public bool Success { get; set; }

        public IReadOnlyCollection<string> Errors { get; set; }

        public IReadOnlyCollection<string> Messages { get; set; }
    }
}
