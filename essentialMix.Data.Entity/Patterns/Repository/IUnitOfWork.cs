using System;
using System.Linq.Expressions;
using essentialMix.Data.Patterns.Repository;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SystemDbContext = System.Data.Entity.DbContext;

namespace essentialMix.Data.Entity.Patterns.Repository;

public interface IUnitOfWork<TContext> : IRepositoryBase
	where TContext : SystemDbContext
{
	[NotNull]
	TContext Context { get; }
	void Register<T>([NotNull] Expression<Func<TContext, IConfiguration, ILogger, T>> template)
		where T : IRepositoryBase;
	void Register([NotNull] Type type, [NotNull] Expression<Func<TContext, IConfiguration, ILogger, IRepositoryBase>> template);
	void Deregister<T>()
		where T : IRepositoryBase;
	void Deregister([NotNull] Type type);
	void ClearRegistration();
	TRepository GetOrCreate<TRepository>()
		where TRepository : IRepositoryBase;
	IRepositoryBase GetOrCreate([NotNull] Type type);
	void Clear();
}