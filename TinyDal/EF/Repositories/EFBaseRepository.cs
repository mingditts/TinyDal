using TinyDal.Common;
using TinyDal.Common.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TinyDal.EF.Repositories
{
	public abstract class EFBaseRepository<T> : IRepository<T> where T : Entity, new()
	{
		private long? _tenantId;
		private DbContext _context;
		private DbSet<T> _dbSet;

		public EFBaseRepository(long? tenantId, DbContext context)
		{
			this._tenantId = tenantId;
			this._context = context;
			this._dbSet = this._context.Set<T>();
		}

		#region Privates

		private Func<T, bool> BuildPredicate(Func<T, bool> predicate, bool deletionManagement)
		{
			Func<T, bool> dePredicate = (T x) => { return true; };

			if (deletionManagement == true)
			{
				dePredicate = (T x) =>
				{
					bool isDeletableEntity = x is DeletableEntity;
					return isDeletableEntity ? (x as DeletableEntity).IsDeleted != true : true;
				};
			}

			Func<T, bool> mtPredicate = (T x) => { return true; };

			if (this._tenantId != null)
			{
				mtPredicate = (T x) =>
				{
					bool isMultiTenantEntity = x is MultiTenantEntity;
					return isMultiTenantEntity ? (x as MultiTenantEntity).TenantId == this._tenantId : true;
				};
			}

			return (T x) => { return dePredicate(x) && mtPredicate(x) && predicate(x); };
		}

		private void AutoUpdate(T entity)
		{
			if (this._tenantId != null)
			{
				bool isMultiTenantEntity = entity is MultiTenantEntity;

				if (isMultiTenantEntity)
				{
					MultiTenantEntity multiTenantEntity = entity as MultiTenantEntity;

					if (multiTenantEntity.TenantId == 0)
					{
						multiTenantEntity.TenantId = this._tenantId.Value;
					}
				}
			}
		}

		private void AutoUpdate(IList<T> entities)
		{
			if (this._tenantId != null)
			{
				foreach (var entity in entities)
				{
					this.AutoUpdate(entity);
				}
			}
		}

		#endregion

		/// <summary>
		/// Execute raw sql
		/// </summary>
		/// <param name="statement"></param>
		/// <param name="parameters"></param>
		protected void ExecuteRaw(string statement, params object[] parameters)
		{
			this._context.Database.ExecuteSqlRaw(statement, parameters);
		}

		/// <summary>
		/// Execute raw sql async
		/// </summary>
		/// <param name="statement"></param>
		/// <param name="parameters"></param>
		protected void ExecuteRawAsync(string statement, params object[] parameters)
		{
			this._context.Database.ExecuteSqlRawAsync(statement, parameters);
		}

		public T FindOneBy(Func<T, bool> predicate, bool readOnly = false, bool preventDeletionManagement = false)
		{
			Func<T, bool> tPredicate = this.BuildPredicate(predicate, !preventDeletionManagement);

			if (readOnly)
			{
				return this._dbSet.AsNoTracking().Where(tPredicate).FirstOrDefault();
			}
			else
			{
				return this._dbSet.Where(tPredicate).FirstOrDefault();
			}
		}

		public async Task<T> FindOneByAsync(Func<T, bool> predicate, bool readOnly = false, bool preventDeletionManagement = false)
		{
			Func<T, bool> tPredicate = this.BuildPredicate(predicate, !preventDeletionManagement);

			var task = new Task<T>(() =>
			{
				if (readOnly)
				{
					return this._dbSet.AsNoTracking().Where(tPredicate).AsQueryable().FirstOrDefault();
				}
				else
				{
					return this._dbSet.Where(tPredicate).AsQueryable().FirstOrDefault();
				}
			});

			task.Start();

			return await task;
		}

		public IList<T> FindManyBy(Func<T, bool> predicate, bool readOnly = false, bool preventDeletionManagement = false)
		{
			Func<T, bool> tPredicate = this.BuildPredicate(predicate, !preventDeletionManagement);

			if (readOnly)
			{
				return this._dbSet.AsNoTracking().Where(tPredicate).ToList();
			}
			else
			{
				return this._dbSet.Where(tPredicate).ToList();
			}
		}

		public async Task<List<T>> FindManyByAsync(Func<T, bool> predicate, bool readOnly = false, bool preventDeletionManagement = false)
		{
			Func<T, bool> tPredicate = this.BuildPredicate(predicate, !preventDeletionManagement);

			var task = new Task<List<T>>(() =>
			{
				if (readOnly)
				{
					return this._dbSet.AsNoTracking().Where(tPredicate).AsQueryable().ToList();
				}
				else
				{
					return this._dbSet.Where(tPredicate).AsQueryable().ToList();
				}
			});

			task.Start();

			return await task;
		}

		public void Insert(T entity)
		{
			this.AutoUpdate(entity);

			this._dbSet.Add(entity);
		}

		public void Insert(IList<T> entities)
		{
			this.AutoUpdate(entities);

			this._dbSet.AddRange(entities);
		}

		public void Update(T entity)
		{
			this._dbSet.Update(entity);
		}

		public void Update(IList<T> entities)
		{
			this._dbSet.UpdateRange(entities);
		}

		public void DeleteById(long id)
		{
			T localEntity = this._dbSet.Local.FirstOrDefault<T>(e => e.Id == id);

			if (localEntity != null)
			{
				this._dbSet.Remove(localEntity);
			}
			else
			{
				this._dbSet.Remove(new T() { Id = id });
			}
		}

		public void Delete(T entity)
		{
			this._dbSet.Remove(entity);
		}

		public void Delete(IList<T> entities)
		{
			this._dbSet.RemoveRange(entities);
		}

		public void DeleteBy(Func<T, bool> predicate, bool preventDeletionManagement = false)
		{
			Func<T, bool> tPredicate = this.BuildPredicate(predicate, !preventDeletionManagement);

			this._dbSet.Remove(this.FindOneBy(tPredicate));
		}

		public void DeleteManyBy(Func<T, bool> predicate, bool preventDeletionManagement = false)
		{
			Func<T, bool> tPredicate = this.BuildPredicate(predicate, !preventDeletionManagement);

			this._dbSet.RemoveRange(this.FindManyBy(tPredicate));
		}
	}
}