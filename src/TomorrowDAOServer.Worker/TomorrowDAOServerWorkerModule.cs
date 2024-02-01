using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace TomorrowDAOServer.Worker
{
    [DependsOn(
        typeof(TomorrowDAOServerApplicationContractsModule),
        typeof(AbpBackgroundWorkersModule)
    )]
    public class TomorrowDAOServerWorkerModule : AbpModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var backgroundWorkerManger = context.ServiceProvider.GetRequiredService<IBackgroundWorkerManager>();
        }
    }
}