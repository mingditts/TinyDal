using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Threading.Tasks;
using TinyDal.Common;

namespace TinyDal.EF
{
	public abstract class EFBaseDataContext : DbContext, IDataContext
	{
		public long? TenantId { get; private set; }

		private IDbContextTransaction _transaction;

		public EFBaseDataContext(DbContextOptions options, long? tenantId, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) : base(options)
		{
			this.TenantId = tenantId;

			this.Database.EnsureCreated();

			this._transaction = this.Database.BeginTransaction(isolationLevel);
		}

		public void Save()
		{
			this.SaveChanges();
		}

		public async Task SaveAsync()
		{
			await this.SaveChangesAsync();
		}

		public void Commit()
		{
			this._transaction.Commit();
		}

		public async Task CommitAsync()
		{
			await this._transaction.CommitAsync();
		}

		public void Rollback()
		{
			this._transaction.Rollback();
		}

		public async Task RollbackAsync()
		{
			await this._transaction.RollbackAsync();
		}

		public void Dispose()
		{
			base.Dispose();

			this._transaction.Dispose();
		}
	}
}
