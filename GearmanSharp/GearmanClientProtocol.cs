using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Twingly.Gearman.Exceptions;
using Twingly.Gearman.Packets;

namespace Twingly.Gearman
{
    public class GearmanJobData : EventArgs
    {
        public string JobHandle { get; protected set; }
        public byte[] Data { get; protected set; }

        public GearmanJobData(string jobHandle, byte[] data)
        {
            JobHandle = jobHandle;
            Data = data;
        }
    }

    public class GearmanClientProtocol : GearmanProtocol, IGearmanClientEventHandler
    {
        public event EventHandler JobCreated;
        public event EventHandler<GearmanJobData> JobCompleted;
        public event EventHandler JobFailed;
        public event EventHandler<GearmanJobData> JobData;
        public event EventHandler<GearmanJobData> JobWarning;
        public event EventHandler<GearmanJobStatus> JobStatus;
        public event EventHandler<GearmanJobData> JobException;

        public GearmanClientProtocol(IGearmanConnection connection)
            : base(connection)
        {
        }

        public string SubmitBackgroundJob(string functionName, byte[] functionArgument, string uniqueId, GearmanJobPriority priority)
        {
            return SubmitJob(functionName, functionArgument, true, uniqueId, priority);
        }

        private string SubmitJob(string functionName, byte[] functionArgument, bool background, string uniqueId, GearmanJobPriority priority)
        {
            if (functionName == null)
                throw new ArgumentNullException("functionName");

            IResponsePacket response;
            lock (Connection.SyncObject) {
              Connection.SendPacket(PackRequest(
                  GetSubmitJobType(priority, background),
                  functionName,
                  uniqueId ?? "",
                  functionArgument ?? new byte[0]));
              response = Connection.GetNextPacket();
            }

            switch (response.Type)
            {
                case PacketType.JOB_CREATED:
                    var packet = UnpackJobCreatedResponse(response);
                    onJobCreated(new EventArgs());
                    return packet;
                case PacketType.ERROR:
                    throw UnpackErrorResponse(response);
                default:
                    throw new GearmanApiException("Got unknown packet from server");
            }
        }

        private static PacketType GetSubmitJobType(GearmanJobPriority priority, bool background)
        {
            switch (priority)
            {
                case GearmanJobPriority.High:
                    return background ? PacketType.SUBMIT_JOB_HIGH_BG : PacketType.SUBMIT_JOB_HIGH;
                case GearmanJobPriority.Normal:
                    return background ? PacketType.SUBMIT_JOB_BG : PacketType.SUBMIT_JOB;
                case GearmanJobPriority.Low:
                    return background ? PacketType.SUBMIT_JOB_LOW_BG : PacketType.SUBMIT_JOB_LOW;
                default:
                    throw new GearmanApiException("Unknown priority and background combination for SubmitJobRequest");
            }
        }

      public byte[] SubmitJob(string functionName, byte[] functionArgument, string uniqueId, GearmanJobPriority priority) {
        return SubmitJob(functionName, functionArgument, uniqueId, priority, Timeout.Infinite);
      }

      public byte[] SubmitJob(string functionName, byte[] functionArgument, string uniqueId, GearmanJobPriority priority, long timeout) {
        var result = new List<byte>();
        var workDone = false;
        lock (Connection.SyncObject) {
          var jobHandle = SubmitJob(functionName, functionArgument, false, uniqueId, priority);
          Stopwatch t = new Stopwatch();
          t.Start();
        //var jobStatus = GetStatus(jobHandle);
          try {
            while (!workDone /*&& jobStatus.IsKnown*/) {
              try {
                var response = Connection.GetNextPacket();

                workDone = ProcessResponse(response, jobHandle, ref result);
              } catch (GearmanConnectionException ex) {
                var inner = ex.InnerException as SocketException;
                if (inner != null && inner.SocketErrorCode == SocketError.TimedOut) {
                  var response = SendGetStatus(jobHandle);
                  switch (response.Type) {
                    case PacketType.STATUS_RES:
                      var jobStatus = UnpackStatusResponse(response);
                      if (!jobStatus.IsKnown)
                        throw;
                      break;
                    // Raise the event?
                    case PacketType.ERROR:
                      throw UnpackErrorResponse(response);
                    default:
                      workDone = ProcessResponse(response, jobHandle, ref result);
                      break;
                  }
                }
                if (inner == null)
                  throw;
                if (timeout != Timeout.Infinite && t.Elapsed.TotalSeconds > timeout)
                  break;
              }
            }
          } finally {
            t.Stop();
          }
          return result != null ? result.ToArray() : null;
        }
      }

      private bool ProcessResponse(IResponsePacket response, string jobHandle, ref List<byte> result) {
        bool workDone = false;
        switch (response.Type) {
          case PacketType.WORK_FAIL:
            var workFail = UnpackWorkFailResponse(response);
            if (jobHandle == workFail) {
              onJobFailed(new EventArgs());
              result = null;
              workDone = true;
            }
            break;
          case PacketType.WORK_COMPLETE:
            var workComplete = UnpackWorkCompleteResponse(response);
            if (jobHandle == workComplete.JobHandle) {
              onJobCompleted(workComplete);
              result.AddRange(workComplete.Data);
              workDone = true;
            }
            break;
          case PacketType.WORK_DATA:
            var workData = UnpackWorkDataResponse(response);
            if (jobHandle == workData.JobHandle) {
              onJobData(workData);
              result.AddRange(workData.Data);
            }
            break;
          case PacketType.WORK_WARNING:
            // Protocol specs say treat this as a DATA packet, so we do
            var workWarning = UnpackWorkDataResponse(response);
            if (jobHandle == workWarning.JobHandle) {
              onJobWarning(workWarning);
              result.AddRange(workWarning.Data);
            }
            break;
          case PacketType.WORK_STATUS:
            var workStatus = UnpackStatusResponse(response);
            if (jobHandle == workStatus.JobHandle)
              onJobStatus(workStatus);
            break;
          case PacketType.WORK_EXCEPTION:
            var workException = UnpackWorkExceptionResponse(response);
            if (jobHandle == workException.JobHandle)
              onJobException(workException);
            break;
          case PacketType.ERROR:
            throw UnpackErrorResponse(response);
          default:
            throw new GearmanApiException("Got unknown packet from server");
        }
        return workDone;
      }

      private IResponsePacket SendGetStatus(string jobHandle) {
        IResponsePacket response;
        lock (Connection.SyncObject) {
          Connection.SendPacket(new RequestPacket(PacketType.GET_STATUS, Encoding.UTF8.GetBytes(jobHandle)));
          response = Connection.GetNextPacket();
        }
        return response;
      }

      public GearmanJobStatus GetStatus(string jobHandle)
        {
            var response = SendGetStatus(jobHandle);

            switch (response.Type)
            {
                case PacketType.STATUS_RES:
                    return UnpackStatusResponse(response);
                    // Raise the event?
                case PacketType.ERROR:
                    throw UnpackErrorResponse(response);
                default:
                    throw new GearmanApiException("Got unknown packet from server");
            }
        }

        protected void onJobCreated(EventArgs e)
        {
            EventHandler handler = JobCreated;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void onJobCompleted(GearmanJobData e)
        {
            EventHandler<GearmanJobData> handler = JobCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void onJobFailed(EventArgs e)
        {
            EventHandler handler = JobFailed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void onJobData(GearmanJobData e)
        {
            EventHandler<GearmanJobData> handler = JobData;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void onJobWarning(GearmanJobData e)
        {
            EventHandler<GearmanJobData> handler = JobWarning;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void onJobStatus(GearmanJobStatus e)
        {
            EventHandler<GearmanJobStatus> handler = JobStatus;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void onJobException(GearmanJobData e)
        {
            EventHandler<GearmanJobData> handler = JobException;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}