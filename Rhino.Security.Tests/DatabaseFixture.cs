using System;
using System.Data.SQLite;
using Microsoft.Practices.ServiceLocation;
using NHibernate;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;
using Rhino.Security.Interfaces;
using Xunit;
using Environment = NHibernate.Cfg.Environment;

namespace Rhino.Security.Tests
{
	public abstract class DatabaseFixture : IDisposable
	{
		protected readonly ISessionFactory factory;
		protected Account account;
		protected IAuthorizationRepository authorizationRepository;
		protected IAuthorizationService authorizationService;
		protected IPermissionsService permissionService;
		protected IPermissionsBuilderService permissionsBuilderService;

		protected ISession session;
		protected User user;

		protected DatabaseFixture()
		{
			BeforeSetup();

			SillyContainer.SessionProvider = (() => session);
			var sillyContainer = new SillyContainer();
			ServiceLocator.SetLocatorProvider(() => sillyContainer);

			Assert.NotNull(typeof (SQLiteConnection));

			Configuration cfg = new Configuration()
				.SetProperty(Environment.ConnectionDriver, typeof(SQLite20Driver).AssemblyQualifiedName)
				.SetProperty(Environment.Dialect, typeof(SQLiteDialect).AssemblyQualifiedName)
				//.SetProperty(Environment.ConnectionDriver, typeof(Sql2008ClientDriver).AssemblyQualifiedName)
				//.SetProperty(Environment.Dialect, typeof(MsSql2008Dialect).AssemblyQualifiedName)
				.SetProperty(Environment.ConnectionString, ConnectionString)
				//.SetProperty(Environment.ProxyFactoryFactoryClass, typeof(ProxyFactoryFactory).AssemblyQualifiedName)
				.SetProperty(Environment.ReleaseConnections, "on_close")
				.SetProperty(Environment.UseSecondLevelCache, "true")
				.SetProperty(Environment.UseQueryCache, "true")
				.SetProperty(Environment.CacheProvider, typeof (HashtableCacheProvider).AssemblyQualifiedName)
				.AddAssembly(typeof (User).Assembly);

			Security.Configure<User>(cfg, SecurityTableStructure.Prefix);

			factory = cfg.BuildSessionFactory();

			session = factory.OpenSession();

			new SchemaExport(cfg).Execute(true, true, false, session.Connection, null);

			session.BeginTransaction();

			SetupEntities();

			session.Flush();
		}

		public virtual string ConnectionString
		{
			get { return "Data Source=:memory:"; }
		}

		#region IDisposable Members

		public virtual void Dispose()
		{
			if (session.Transaction.IsActive)
				session.Transaction.Rollback();
			session.Dispose();
		}

		#endregion

		protected virtual void BeforeSetup()
		{
		}

		private void SetupEntities()
		{
			user = new User {Name = "Ayende"};
			account = new Account {Name = "south sand"};

			session.Save(user);
			session.Save(account);

			authorizationService = ServiceLocator.Current.GetInstance<IAuthorizationService>();
			permissionService = ServiceLocator.Current.GetInstance<IPermissionsService>();
			permissionsBuilderService = ServiceLocator.Current.GetInstance<IPermissionsBuilderService>();
			authorizationRepository = ServiceLocator.Current.GetInstance<IAuthorizationRepository>();

			authorizationRepository.CreateUsersGroup("Administrators");
			authorizationRepository.CreateEntitiesGroup("Important Accounts");
			authorizationRepository.CreateOperation("/Account/Edit");


			authorizationRepository.AssociateUserWith(user, "Administrators");
			authorizationRepository.AssociateEntityWith(account, "Important Accounts");
		}
	}
}