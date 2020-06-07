using System;
using System.Threading.Tasks;

namespace TinyDal.Common
{
	public interface IDataContext : IDisposable /*, IAsyncDisposable*/
	{
		long? TenantId { get; }

		void Save();

		Task SaveAsync();

		void Commit();

		Task CommitAsync();

		void Rollback();

		Task RollbackAsync();
	}
}