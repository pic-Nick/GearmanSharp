using System;
using System.Net.Sockets;
using System.Threading;

namespace Twingly.Gearman
{
    /// <summary>
    /// Adapter for System.Net.Sockets.Socket that implements ISocket.
    /// Passes all calls to the underlying Socket.
    /// AddressFamily.InterNetwork. SocketType.Stream. ProtocolType.Tcp.
    /// </summary>
    public class SocketAdapter : ISocket
    {
        private readonly Socket _socket;
        private class StateObject {
          public Socket Handler;
          public Object Result;
          public Exception Exception;
          public EventWaitHandle WaitObject;
        }

        public SocketAdapter(Socket socket)
        {
            _socket = socket;
        }

        public virtual bool Connected
        {
            get { return _socket.Connected; }
        }

        public virtual bool BlockingMode {
          get { return _socket.Blocking; }
        }

        public virtual void Connect(string host, int port)
        {
          var mre = new ManualResetEvent(false);
          var stateObject = new StateObject { Handler = _socket, WaitObject = mre };
          _socket.BeginConnect(host, port, ar => {
            var so = (StateObject)ar.AsyncState;
            try {
              so.Handler.EndConnect(ar);
            } catch (Exception ex) {
              so.Exception = ex;
            }
            so.WaitObject.Set();
          }, stateObject);
          mre.WaitOne(_socket.ReceiveTimeout);
          if (stateObject.Exception != null)
            throw stateObject.Exception;
          //try {
          //  _socket.Connect(host, port);
          //} catch (SocketException ex) {
          //  if (!_socket.Blocking && ex.SocketErrorCode == SocketError.WouldBlock) {
          //    var sw = new Stopwatch();
          //    sw.Start();
          //    while (!_socket.Poll(1000000, SelectMode.SelectWrite)) {
          //      Thread.Sleep(500);
          //      if (sw.ElapsedMilliseconds > _socket.ReceiveTimeout)
          //        throw new SocketException((int)SocketError.TimedOut);
          //    }
          //  } else
          //    throw;
          //}
        }

        public virtual int Send(byte[] buffer) {
          var mre = new ManualResetEvent(false);
          var stateObject = new StateObject { Handler = _socket, WaitObject = mre };
          _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, ar => {
            var so = (StateObject)ar.AsyncState;
            try {
              so.Result = so.Handler.EndSend(ar);
            } catch (Exception ex) {
              so.Exception = ex;
            }
            so.WaitObject.Set();
          }, stateObject);
          mre.WaitOne(_socket.SendTimeout);
          if (stateObject.Exception != null)
            throw stateObject.Exception;
          return (int)stateObject.Result;
          //return _socket.Send(buffer);
        }

        public virtual int Receive(byte[] buffer, int size, SocketFlags socketFlags) {
          return this.Receive(buffer, 0, size, socketFlags);
          //int res;
          //var sw = new Stopwatch();
          //sw.Start();
          //int count = 0;
          //do {
          //  try {
          //    res = _socket.Receive(buffer, size, socketFlags);
          //    break;
          //  } catch (SocketException ex) {
          //if (_socket.Blocking || ex.SocketErrorCode != SocketError.WouldBlock)
          //      throw;
          //    Thread.Sleep(500);
          //    if (sw.ElapsedMilliseconds > _socket.ReceiveTimeout)
          //      throw new SocketException((int)SocketError.TimedOut);
          //  }
          //} while (true);
          //return res;
        }

        public virtual int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
          var mre = new ManualResetEvent(false);
          var stateObject = new StateObject { Handler = _socket, WaitObject = mre };
          _socket.BeginReceive(buffer, offset, size, socketFlags, ar => {
            var so = (StateObject)ar.AsyncState;
            try {
              so.Result = so.Handler.EndReceive(ar);
            } catch (Exception ex) {
              so.Exception = ex;
            }
            so.WaitObject.Set();
          }, stateObject);
          mre.WaitOne(_socket.ReceiveTimeout);
          if (stateObject.Exception != null)
            throw stateObject.Exception;
          return (int)stateObject.Result;

          //int res;
          //var sw = new Stopwatch();
          //sw.Start();
          //int count = 0;
          //do {
          //  try {
          //    res = _socket.Receive(buffer, offset, size, socketFlags);
          //    break;
          //  } catch (SocketException ex) {
          //if (_socket.Blocking || ex.SocketErrorCode != SocketError.WouldBlock)
          //      throw;
          //    Thread.Sleep(500);
          //    if (sw.ElapsedMilliseconds > _socket.ReceiveTimeout)
          //      throw new SocketException((int)SocketError.TimedOut);
          //  }
          //} while (true);
          //return res;
        }

        public virtual void Shutdown()
        {
          _socket.Shutdown(SocketShutdown.Both);
          var mre = new ManualResetEvent(false);
          var stateObject = new StateObject { Handler = _socket, WaitObject = mre };
          _socket.BeginDisconnect(true, ar => {
            var so = (StateObject)ar.AsyncState;
            try {
              so.Handler.EndDisconnect(ar);
            } catch (Exception ex) {
              so.Exception = ex;
            }
            so.WaitObject.Set();
          }, stateObject);
          mre.WaitOne(_socket.ReceiveTimeout);
          if (stateObject.Exception != null)
            throw stateObject.Exception;
    }

    public virtual void Close()
        {
          _socket.Close();
        }
    }
}