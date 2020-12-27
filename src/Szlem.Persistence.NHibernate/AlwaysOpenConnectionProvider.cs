using NHibernate.Connection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Szlem.Persistence.NHibernate
{
    public class AlwaysOpenConnectionProvider : DriverConnectionProvider
    {
        private DbConnection _cachedConnection;
        private readonly object _syncRoot = new object();

        public override DbConnection GetConnection()
        {
            lock (_syncRoot)
            {
                if (_cachedConnection != null)
                    return _cachedConnection;
            }

            var connection = base.GetConnection();

            lock (_syncRoot)
            {
                if (_cachedConnection == null)
                {
                    _cachedConnection = connection;
                    return _cachedConnection;
                }
            }

            // there already is a cached connection, close this one and return the cached one
            connection.Close();
            return _cachedConnection;
        }

        public override async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                if (_cachedConnection != null)
                    return _cachedConnection;
            }

            var connection = await base.GetConnectionAsync(cancellationToken);

            lock (_syncRoot)
            {
                if (_cachedConnection == null)
                {
                    _cachedConnection = connection;
                    return _cachedConnection;
                }
            }

            // there already is a cached connection, close this one and return the cached one
            await connection.CloseAsync();
            return _cachedConnection;
        }

        public override void CloseConnection(DbConnection conn) { }
    }
}
