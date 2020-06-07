using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TinyDal.Common.Repositories
{
	public interface IRepository<T> where T : Entity
	{
		T FindOneBy(Func<T, bool> predicate, bool readOnly = false, bool preventDeletionManagement = false);

		IList<T> FindManyBy(Func<T, bool> predicate, bool readOnly = false, bool preventDeletionManagement = false);

		Task<T> FindOneByAsync(Func<T, bool> predicate, bool readOnly = false, bool preventDeletionManagement = false);

		Task<List<T>> FindManyByAsync(Func<T, bool> predicate, bool readOnly = false, bool preventDeletionManagement = false);

		void Insert(T entity);

		void Insert(IList<T> entities);

		void Update(T entity);

		void Update(IList<T> entities);

		void DeleteById(long id);

		void Delete(T entity);

		void Delete(IList<T> entities);

		void DeleteBy(Func<T, bool> predicate, bool preventDeletionManagement = false);

		void DeleteManyBy(Func<T, bool> predicate, bool preventDeletionManagement = false);
	}
}