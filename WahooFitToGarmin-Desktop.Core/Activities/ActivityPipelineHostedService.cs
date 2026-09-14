using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Runs the pipeline for the application's lifetime.
    /// </summary>
    /// <remarks>
    /// The pipeline is a background worker, so it is expressed as a hosted
    /// service and started by the generic host that already exists. This is what
    /// leaves the user interface with three jobs: render state, forward user
    /// intent, and supply platform implementations.
    /// </remarks>
    public sealed class ActivityPipelineHostedService : BackgroundService
    {
        private readonly ActivityPipeline _pipeline;
        private readonly ActivityDiscovery _discovery;
        private readonly ILogger<ActivityPipelineHostedService> _logger;

        public ActivityPipelineHostedService(
            ActivityPipeline pipeline,
            ActivityDiscovery discovery,
            ILogger<ActivityPipelineHostedService> logger)
        {
            _pipeline = pipeline;
            _discovery = discovery;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting uploader ......");

            _discovery.Start();

            try
            {
                await _pipeline.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _discovery.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}
