using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using Twingly.Gearman.Configuration;
using Twingly.Gearman.Exceptions;

namespace Twingly.Gearman
{
    public abstract class GearmanConnectionManager : IDisposable
    {
        internal const int _DEFAULT_PORT = 4730;

        private readonly IList<IGearmanConnection> _connections;
        private IGearmanConnectionFactory _connectionFactory;

        public IGearmanConnectionFactory ConnectionFactory
        {
            get
            {
                return _connectionFactory;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _connectionFactory = value;
            }
        }

        protected GearmanConnectionManager()
        {
            _connections = new List<IGearmanConnection>();
            _connectionFactory = new GearmanConnectionFactory();
        }

        protected GearmanConnectionManager(string clusterName)
            : this()
        {
            if (clusterName == null)
                throw new ArgumentNullException("clusterName");

            var section = ConfigurationManager.GetSection("gearman") as GearmanConfigurationSection;
            if (section == null)
                throw new ConfigurationErrorsException("Section gearman is not found.");

            ParseConfiguration(section.Clusters[clusterName]);
        }

        protected GearmanConnectionManager(ClusterConfigurationElement clusterConfiguration)
            : this()
        {
            if (clusterConfiguration == null)
                throw new ArgumentNullException("clusterConfiguration");

            ParseConfiguration(clusterConfiguration);
        }

        public void Dispose()
        {
            // http://codecrafter.blogspot.se/2010/01/better-idisposable-pattern.html
            CleanUpManagedResources();
            GC.SuppressFinalize(this);
        }

        protected virtual void CleanUpManagedResources()
        {
            DisconnectAll();
        }

        private void ParseConfiguration(ClusterConfigurationElement cluster)
        {
            foreach (ServerConfigurationElement server in cluster.Servers)
            {
                AddServer(server.Host, server.Port);
            }
        }

        public void AddServer(string host)
        {
            AddServer(host, _DEFAULT_PORT);
        }

        public void AddServer(string host, int port)
        {
            AddConnection(ConnectionFactory.CreateConnection(host, port));
        }

      public bool AddServers(string hosts) {
      switch (hosts) {
        case null:
          throw new ArgumentNullException("hosts");
        case "":
          throw new ArgumentException("Parameter cann't be empty.", "hosts");
      }
      var hostsArr = hosts.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
      if (hostsArr.Length > 0) {
        foreach (var host in hostsArr) {
          var parts = host.Split(':');
          int port;
          if (parts.Length == 2 && Int32.TryParse(parts[1], out port))
            this.AddServer(host, port);
          else
            this.AddServer(host);
        }
        return true;
      }
        return false;
      }

    public void DisconnectAll()
        {
            foreach (var connection in _connections)
            {
                if (connection != null)
                {
                    connection.Disconnect();
                }
            }
        }

        private void AddConnection(IGearmanConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            _connections.Add(connection);
        }

        protected IEnumerable<IGearmanConnection> GetAliveConnections(bool blockingMode)
        {
            var connections = _connections.Shuffle(new Random()).ToList();
            var isAllDead = _connections.All(conn => conn.IsDead());
            var aliveConnections = new List<IGearmanConnection>();

            foreach (var connection in connections) {
                if (connection.BlockingMode != blockingMode)
                    connection.Disconnect();

                // Try to reconnect if they're not connected and not dead, or if all servers are dead, we will try to reconnect them anyway.
                if (!connection.IsConnected() && (!connection.IsDead() || isAllDead))
                {
                    try {
                        connection.BlockingMode = blockingMode;
                        connection.Connect();
                        OnConnectionConnected(connection);

                        // quick idea: Make GearmanConnection a base class and sub class it differently for the
                        // client and the worker, where the worker always registers all functions when connecting?
                        // Could that work?
                    }
                    catch (GearmanConnectionException ex)
                    {
                        // Is it enough to catch GearmanConnectionException?
            connection.MarkAsDead();
                        continue;
                    }
                }

                if (connection.IsConnected())
                {
                    aliveConnections.Add(connection);
                }
            }

            return aliveConnections;
        }

        protected IEnumerable<IGearmanConnection> GetAliveConnections() {
          return GetAliveConnections(true);
        }

        protected virtual void OnConnectionConnected(IGearmanConnection connection)
        {
        }
    }
}