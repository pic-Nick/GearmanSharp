using System;
using System.Linq;
using System.Linq.Expressions;
using Twingly.Gearman.Packets;

namespace Twingly.Gearman
{
    public interface IGearmanConnection: ICloneable
    {
        object SyncObject { get; }
        void Connect();
        void Disconnect();
        void SendPacket(RequestPacket p);
        IResponsePacket GetNextPacket();

        bool IsConnected();

        string Host { get; }
        int Port { get; }
        bool BlockingMode { get; set; }

        bool IsDead(); // A dead connection should not be retried. When it's time to retry, it won't be dead.
        void MarkAsDead();
    }
}