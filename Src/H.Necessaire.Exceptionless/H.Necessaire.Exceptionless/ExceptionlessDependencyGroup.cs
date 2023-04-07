namespace H.Necessaire.Exceptionless
{
    public class ExceptionlessDependencyGroup : ImADependencyGroup
    {
        public void RegisterDependencies(ImADependencyRegistry dependencyRegistry)
        {
            dependencyRegistry
                .Register<Logging.DependencyGroup>(() => new Logging.DependencyGroup())
                ;
        }
    }
}
