using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions.Helpers;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.SqlCommand;
using NHibernate.Stat;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Type;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Recruitment.Impl.Entities;

namespace Szlem.Recruitment.Impl
{
    internal class DbSessionProvider : IDisposable
    {
        private readonly IPersistenceConfigurer _persistenceConfigurer;
        private readonly ILogger<DbSessionProvider> _logger;

        public DbSessionProvider(IPersistenceConfigurer persistenceConfigurer, ILogger<DbSessionProvider> logger)
        {
            _persistenceConfigurer = persistenceConfigurer ?? throw new ArgumentNullException(nameof(persistenceConfigurer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        private Configuration _configuration;
        public Configuration Configuration => _configuration ?? (_configuration = BuildConfiguration());

        private ISessionFactory _sessionFactory;
        protected ISessionFactory SessionFactory => _sessionFactory ?? (_sessionFactory = Configuration.BuildSessionFactory());

        public ISession CreateSession()
        {
            return SessionFactory.WithOptions().Interceptor(new MyInterceptor(_logger)).OpenSession();
        }


        private class MyInterceptor : EmptyInterceptor
        {
            private readonly ILogger<DbSessionProvider> _logger;

            public MyInterceptor(ILogger<DbSessionProvider> logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public override SqlString OnPrepareStatement(SqlString sql)
            {
                _logger.LogDebug(sql.ToString());
                return base.OnPrepareStatement(sql);
            }
        }


        private Configuration BuildConfiguration()
        {
            var config = Fluently.Configure()
                .Mappings(x => {
                    x.FluentMappings
                        .AddFromAssemblyOf<DbSessionProvider>();
                    x.FluentMappings.Conventions.AddFromAssemblyOf<Persistence.NHibernate.Marker>();
                    x.FluentMappings.Conventions.Add(ForeignKey.EndsWith("ID"));
                });
            if (_persistenceConfigurer != null)
                config = config.Database(_persistenceConfigurer);
            return config.BuildConfiguration();
        }

        public void UnsafeCleanAndRebuildSchema()
        {
            var sb = new System.Text.StringBuilder();
            using (var sw = new System.IO.StringWriter(sb))
                using (var session = CreateSession())
                    new SchemaExport(Configuration).Execute(sql => { }, true, false, session.Connection, sw);
            var schema = sb.ToString();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool onlyManagedResources)
        {
            if (_sessionFactory != null)
                _sessionFactory.Dispose();
            _sessionFactory = null;
        }
    }
}
