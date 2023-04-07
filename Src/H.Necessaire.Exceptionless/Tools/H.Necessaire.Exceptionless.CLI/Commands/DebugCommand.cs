using H.Necessaire.Runtime.CLI.Commands;
using System.Threading.Tasks;

namespace H.Necessaire.Exceptionless.CLI.Commands
{
    internal class DebugCommand : CommandBase
    {
        public override Task<OperationResult> Run()
        {
            return OperationResult.Win().AsTask();
        }
    }
}
