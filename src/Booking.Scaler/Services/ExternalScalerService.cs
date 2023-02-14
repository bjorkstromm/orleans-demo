using Externalscaler;
using Grpc.Core;
using Orleans.Runtime;

namespace Booking.Scaler.Services;

public class ExternalScalerService : ExternalScaler.ExternalScalerBase
{
    private readonly ILogger<ExternalScalerService> _logger;
    private readonly IClusterClient _clusterClient;

    private const string MetricNameKey = "grainThreshold";
    private const string GrainTypeKey = "graintype";
    private const string UpperBoundKey = "upperbound";

    public ExternalScalerService(
        IClusterClient clusterClient,
        ILogger<ExternalScalerService> logger)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public override async Task<GetMetricsResponse> GetMetrics(
        GetMetricsRequest request,
        ServerCallContext context)
    {
        CheckRequestMetadata(request.ScaledObjectRef);

        var response = new GetMetricsResponse();
        var grainType = request.ScaledObjectRef.ScalerMetadata[GrainTypeKey];
        var upperbound = Convert.ToInt32(request.ScaledObjectRef.ScalerMetadata[UpperBoundKey]);

        var (grainCount, siloCount) = await GetGrainCountInCluster(grainType);
        var grainsPerSilo = grainCount > 0 && siloCount > 0 ? grainCount / siloCount : 0;

        var metricValue = siloCount;

        // scale in
        if (grainsPerSilo < upperbound)
        {
            metricValue = grainCount == 0 ? 1 : Convert.ToInt16(grainCount / upperbound);
        }

        // scale out
        if (grainsPerSilo >= upperbound)
        {
            metricValue = siloCount + 1;
        }

        _logger.LogInformation(
            "Grains Per Silo: {GrainsPerSilo}, " +
            "Upper Bound: {Upperbound}," +
            "Grain Count: {SummaryGrainCount}, " +
            "Silo Count: {SummarySiloCount}. " +
            "Scale to {MetricValue}",
            grainsPerSilo, upperbound, grainCount, siloCount, metricValue);

        response.MetricValues.Add(new MetricValue
        {
            MetricName = MetricNameKey,
            MetricValue_ = metricValue
        });

        return response;
    }

    public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request,
        ServerCallContext context)
    {
        CheckRequestMetadata(request);

        var resp = new GetMetricSpecResponse();

        resp.MetricSpecs.Add(new MetricSpec
        {
            MetricName = MetricNameKey,
            TargetSize = 1
        });

        return Task.FromResult(resp);
    }

    public override async Task StreamIsActive(
        ScaledObjectRef request,
        IServerStreamWriter<IsActiveResponse> responseStream,
        ServerCallContext context)
    {
        CheckRequestMetadata(request);

        while (!context.CancellationToken.IsCancellationRequested)
        {
            if (await TooManyGrainsInTheCluster(request))
            {
                _logger.LogInformation("Writing IsActiveResponse to stream with Result = true");
                await responseStream.WriteAsync(new IsActiveResponse
                {
                    Result = true
                });
            }

            await Task.Delay(TimeSpan.FromSeconds(30));
        }
    }

    public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
    {
        CheckRequestMetadata(request);

        var result = await TooManyGrainsInTheCluster(request);
        _logger.LogInformation("Returning {Result} from IsActive", result);

        return new IsActiveResponse
        {
            Result = result
        };
    }

    private static void CheckRequestMetadata(ScaledObjectRef request)
    {
        if (!request.ScalerMetadata.ContainsKey(GrainTypeKey) || !request.ScalerMetadata.ContainsKey(UpperBoundKey))
        {
            throw new ArgumentException($"{GrainTypeKey}, and {UpperBoundKey} must be specified");
        }
    }

    private async Task<bool> TooManyGrainsInTheCluster(ScaledObjectRef request)
    {
        var grainType = request.ScalerMetadata[GrainTypeKey];
        var upperbound = request.ScalerMetadata[UpperBoundKey];

        var (grainCount, siloCount) = await GetGrainCountInCluster(grainType);

        if (grainCount == 0 || siloCount == 0)
        {
            return false;
        }

        var tooMany = Convert.ToInt32(upperbound) <= (grainCount / siloCount);

        return tooMany;
    }

    private async Task<(int GrainCount, int SiloCount)> GetGrainCountInCluster(string grainType)
    {
        var managementGrain = _clusterClient.GetGrain<IManagementGrain>(0);
        var statistics = await managementGrain.GetDetailedGrainStatistics();

        var grainCount = statistics
            .Count(grain => string.Equals(grain.GrainId.Type.ToString(), grainType, StringComparison.OrdinalIgnoreCase));

        var activeSiloCount = (await managementGrain.GetDetailedHosts(onlyActive: true)).Length;

        _logger.LogInformation(
            "Found {Count} instances of {GrainType} in cluster, within {ActiveSiloCount} silos in the cluster",
            grainCount, grainType, activeSiloCount);

        return (grainCount, activeSiloCount);
    }
}