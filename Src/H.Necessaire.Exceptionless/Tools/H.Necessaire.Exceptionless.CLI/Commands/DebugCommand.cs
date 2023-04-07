using H.Necessaire.Runtime.CLI.Commands;
using System;
using System.Threading.Tasks;

namespace H.Necessaire.Exceptionless.CLI.Commands
{
    internal class DebugCommand : CommandBase
    {
        public override async Task<OperationResult> Run()
        {
            await Logger.LogTrace("Just a Simple Trace");
            await Logger.LogInfo("Just a Simple Info");
            await Logger.LogDebug("Just a Simple Debug");
            await Logger.LogWarn("Just a Simple Warn");


            Console.ReadLine();

            return OperationResult.Win();
        }
    }
}
