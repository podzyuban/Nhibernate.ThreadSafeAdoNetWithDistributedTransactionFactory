using System;
using System.Data;
using NHibernate.Transaction;
using NHibernate;
using NHibernate.Engine;


namespace NHibernate
{
    internal class ThreadSafeAdoTransaction : ITransaction
    {
        private readonly ITransaction _source;
        private readonly object _lock;

        public ThreadSafeAdoTransaction(ITransaction source, object @lock)
        {
            this._source = source;
            this._lock = @lock;
        }

        public void Dispose()
        {
            this.WithLock(this._source.Dispose);
        }

        public void Begin()
        {
            this._source.Begin(); // not nessary lock sync
        }

        public void Commit()
        {
            this.WithLock(this._source.Commit);
        }

        public void Rollback()
        {
            this.WithLock(this._source.Rollback);
        }

        public bool IsActive => this._source.IsActive;

        public bool WasRolledBack
        {
            get { return this.WithLock(() => this._source.WasRolledBack); }
        }

        public bool WasCommitted
        {
            get { return this.WithLock(() => this._source.WasCommitted); }
        }
        public void RegisterSynchronization(ISynchronization synchronization)
        {
            this.WithLock(() => this._source.RegisterSynchronization(synchronization));
        }

        public void Enlist(IDbCommand command)
        {
            this._source.Enlist(command); // not nessary lock sync
        }

        public void Begin(System.Data.IsolationLevel isolationLevel)
        {
            this._source.Begin(isolationLevel);
        }

        private void WithLock(System.Action action)
        {
            WithLock<object>(() =>
            {
                action();
                return null;
            });
        }

        private T WithLock<T>(Func<T> func)
        {
            lock (this._lock)
            {
                var result = func();

                return result;
            }
        }
    }
}