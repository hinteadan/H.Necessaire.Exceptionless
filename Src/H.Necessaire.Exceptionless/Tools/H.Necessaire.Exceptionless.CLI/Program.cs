using H.Necessaire.CLI;
using H.Necessaire.Runtime.CLI;

namespace H.Necessaire.Exceptionless.CLI
{
    internal class Program
    {
        public static void Main()
        {
            new CliApp()
                .WithEverything()
                .WithDefaultRuntimeConfig()
                .With(x => x.Register<ExceptionlessDependencyGroup>(() => new ExceptionlessDependencyGroup()))
                .Run(askForCommandIfEmpty: true)
                .GetAwaiter()
                .GetResult()
                ;
        }
    }
}