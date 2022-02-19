using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using essentialMix.Data.Patterns.Repository;
using essentialMix.Helpers;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SystemDbContext = System.Data.Entity.DbContext;

namespace essentialMix.Data.Entity.Patterns.Repository;

public class UnitOfWork<TContext> : RepositoryBase, IUnitOfWork<TContext>
	where TContext : SystemDbContext
{
	private readonly IDictionary<Type, Func<TContext, IConfiguration, ILogger, IRepositoryBase>> _templates = new Dictionary<Type, Func<TContext, IConfiguration, ILogger, IRepositoryBase>>();
	private readonly IDictionary<Type, IRepositoryBase> _instances = new Dictionary<Type, IRepositoryBase>();

	private TContext _context;

	/// <inheritdoc />
	public UnitOfWork([NotNull] TContext context, [NotNull] IConfiguration configuration)
		: this(context, configuration, null, false)
	{
	}

	/// <inheritdoc />
	protected UnitOfWork([NotNull] TContext context, [NotNull] IConfiguration configuration, ILogger logger)
		: this(context, configuration, logger, false)
	{
	}

	/// <inheritdoc />
	protected UnitOfWork([NotNull] TContext context, [NotNull] IConfiguration configuration, ILogger logger, bool ownsContext)
		: base(configuration, logger)
	{
		_context = context;
		OwnsContext = ownsContext;
	}

	/// <inheritdoc />
	public TContext Context => _context;

	protected bool OwnsContext { get; }

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			lock (_templates)
			{
				_templates.Clear();
				Clear();
			}

			if (OwnsContext) ObjectHelper.Dispose(ref _context);
		}

		base.Dispose(disposing);
	}

	/// <inheritdoc />
	public void Register<T>(Expression<Func<TContext, IConfiguration, ILogger, T>> template)
		where T : IRepositoryBase
	{
		Expression castExpr = Expression.Convert(template.Body, typeof(IRepositoryBase));
		Register(typeof(T), Expression.Lambda<Func<TContext, IConfiguration, ILogger, IRepositoryBase>>(castExpr));
	}

	/// <inheritdoc />
	public void Register(Type type, Expression<Func<TContext, IConfiguration, ILogger, IRepositoryBase>> template)
	{
		if (!typeof(IRepositoryBase).IsAssignableFrom(type)) throw new ArgumentException($"{type} does not implement {nameof(IRepositoryBase)} interface.", nameof(type));

		lock (_templates)
		{
			if (_templates.ContainsKey(type)) return;
			_templates.Add(type, template.Compile());
		}
	}

	/// <inheritdoc />
	public void Deregister<T>()
		where T : IRepositoryBase
	{
		Deregister(typeof(T));
	}

	/// <inheritdoc />
	public void Deregister(Type type)
	{
		lock (_templates)
		{
			if (!_templates.ContainsKey(type)) return;
			_templates.Remove(type);
		}
	}

	/// <inheritdoc />
	public void ClearRegistration()
	{
		lock (_templates)
			_templates.Clear();
	}

	/// <inheritdoc />
	[NotNull]
	public TRepository GetOrCreate<TRepository>()
		where TRepository : IRepositoryBase
	{
		return (TRepository)GetOrCreate(typeof(TRepository));
	}

	/// <inheritdoc />
	[NotNull]
	public IRepositoryBase GetOrCreate(Type type)
	{
		lock (_instances)
		{
			if (_instances.TryGetValue(type, out IRepositoryBase repository)) return repository;

			lock (_templates)
			{
				if (!_templates.TryGetValue(type, out Func<TContext, IConfiguration, ILogger, IRepositoryBase> template)) throw new InvalidOperationException("Type is not registered or created.");
				repository = template(Context, Configuration, Logger);
				_instances.Add(type, repository);
				return repository;
			}
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		lock (_instances)
		{
			foreach (IRepositoryBase repository in _instances.Values)
				repository?.Dispose();

			_instances.Clear();
		}
	}
}