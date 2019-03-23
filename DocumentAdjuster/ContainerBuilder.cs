using DocumentAdjuster.Services;
using Ninject;

namespace DocumentAdjuster
{
    internal static class ContainerBuilder
    {
        public static StandardKernel Build()
        {
            var container = new StandardKernel();

            container.Bind<IDocumentAdjuster>().To<DocumentAdjuster>().InSingletonScope();
            container.Bind<IBinarizationService>().To<SimpleBinarizationService>().InSingletonScope();
            container.Bind<IBorderSearchService>().To<BorderSearchService>().InSingletonScope();
            container.Bind<IEquationOfLineService>().To<EquationOfLineService>().InSingletonScope();
            container.Bind<IMedianFilterService>().To<MedianFilterService>().InSingletonScope();
            container.Bind<ICornerFinder>().To<CornerFinder>().InSingletonScope();
            container.Bind<IProjectiveTransformationService>().To<ProjectiveTransformationService>().InSingletonScope();

            return container;
        }
    }
}
