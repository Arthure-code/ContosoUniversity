using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace ContosoUniversity.Tests.Doubles
{
    /// <summary>
    /// Entity Framework appelle ses requetes de facon asynchrone. Un
    /// ensemble simule, lui, tient dans une liste en memoire : ce
    /// fournisseur recoit les appels asynchrones et repond depuis la liste.
    /// </summary>
    internal sealed class FournisseurAsynchrone<TEntite> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _interne;

        public FournisseurAsynchrone(IQueryProvider interne)
        {
            _interne = interne;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new RequeteAsynchrone<TEntite>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new RequeteAsynchrone<TElement>(expression);
        }

        public object? Execute(Expression expression)
        {
            return _interne.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _interne.Execute<TResult>(expression);
        }

        /// <summary>
        /// TResult vaut Task de quelque chose : le resultat est calcule tout
        /// de suite, puis emballe dans une tache deja terminee.
        /// </summary>
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            Type type = typeof(TResult).GetGenericArguments()[0];

            object? resultat = typeof(IQueryProvider)
                .GetMethods()
                .Single(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(type)
                .Invoke(_interne, new object[] { expression });

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(type)
                .Invoke(null, new[] { resultat })!;
        }
    }

    /// <summary>
    /// Une requete que l'on peut parcourir des deux facons, celle que
    /// Linq attend et celle qu'Entity Framework attend.
    /// </summary>
    internal sealed class RequeteAsynchrone<TEntite> : EnumerableQuery<TEntite>, IAsyncEnumerable<TEntite>, IQueryable<TEntite>
    {
        public RequeteAsynchrone(Expression expression)
            : base(expression)
        {
        }

        public IAsyncEnumerator<TEntite> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new EnumerateurAsynchrone<TEntite>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new FournisseurAsynchrone<TEntite>(this);
    }

    internal sealed class EnumerateurAsynchrone<TEntite> : IAsyncEnumerator<TEntite>
    {
        private readonly IEnumerator<TEntite> _interne;

        public EnumerateurAsynchrone(IEnumerator<TEntite> interne)
        {
            _interne = interne;
        }

        public TEntite Current => _interne.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_interne.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _interne.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
