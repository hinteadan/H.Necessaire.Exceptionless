namespace H.Necessaire.Exceptionless.Logging
{
    internal class DependencyGroup : ImADependencyGroup
    {
        public void RegisterDependencies(ImADependencyRegistry dependencyRegistry)
        {
            dependencyRegistry
                .Register<ExceptionlessLogProcessor>(() => new ExceptionlessLogProcessor())
                ;
        }
    }
}
