using System;

namespace Twingly.Gearman
{
    public interface IGearmanClient : IDisposable, IGearmanClientEventHandler
    {
        GearmanJobStatus GetStatus(GearmanJobRequest jobRequest, bool createNewConnection = false);
        GearmanJobStatus GetStatus(string host, string jobHandle);
        GearmanJobStatus GetStatus(string host, int port, string jobHandle);

        byte[] SubmitJob(string functionName, byte[] functionArgument);
        byte[] SubmitJob(string functionName, byte[] functionArgument, string uniqueId, GearmanJobPriority priority);
        byte[] SubmitJob(string functionName, byte[] functionArgument, string uniqueId, GearmanJobPriority priority, long timeout);

        TResult SubmitJob<TArg, TResult>(string functionName, TArg functionArgument,
            DataSerializer<TArg> argumentSerializer, DataDeserializer<TResult> resultDeserializer, long timeout)
            where TArg : class
            where TResult : class;

        TResult SubmitJob<TArg, TResult>(string functionName, TArg functionArgument, string uniqueId, GearmanJobPriority priority,
            DataSerializer<TArg> argumentSerializer, DataDeserializer<TResult> resultDeserializer, long timeout)
            where TArg : class
            where TResult : class;


        GearmanJobRequest SubmitBackgroundJob(string functionName, byte[] functionArgument);
        GearmanJobRequest SubmitBackgroundJob(string functionName, byte[] functionArgument, string uniqueId, GearmanJobPriority priority);

        GearmanJobRequest SubmitBackgroundJob<TArg>(string functionName, TArg functionArgument,
            DataSerializer<TArg> argumentSerializer)
            where TArg : class;

        GearmanJobRequest SubmitBackgroundJob<TArg>(string functionName, TArg functionArgument, string uniqueId, GearmanJobPriority priority,
            DataSerializer<TArg> argumentSerializer)
            where TArg : class;
    }
}