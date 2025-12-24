using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;

namespace Services.Tests.Helpers;

public class AsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public AsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new AsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new AsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
    {
        return new AsyncEnumerable<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        // Handle Task<T> return types (e.g., Task<TEntity?> from FirstOrDefaultAsync)
        if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethods()
                .FirstOrDefault(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                ?.MakeGenericMethod(resultType)
                ?.Invoke(_inner, new[] { expression });

            if (executionResult != null)
            {
                var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))?.MakeGenericMethod(resultType);
                var task = fromResultMethod?.Invoke(null, new[] { executionResult });
                if (task != null)
                {
                    return (TResult)task;
                }
            }
            
            // If executionResult is null, return Task.FromResult with default value
            var defaultTaskMethod = typeof(Task).GetMethod(nameof(Task.FromResult))?.MakeGenericMethod(resultType);
            var defaultValue = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            var defaultTask = defaultTaskMethod?.Invoke(null, new[] { defaultValue });
            return (TResult)(defaultTask ?? throw new InvalidOperationException("Failed to create Task"));
        }

        // Handle non-Task return types
        var syncResult = _inner.Execute(expression);
        return (TResult)syncResult;
    }
}

internal class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public AsyncEnumerable(Expression expression)
        : base(expression)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new AsyncQueryProvider<T>(this);
}

internal class AsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public AsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return default;
    }
}

