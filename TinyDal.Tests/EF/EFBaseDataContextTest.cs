using TinyDal.Common;
using TinyDal.EF;
using TinyDal.EF.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TinyDal.Tests.EF
{
	/// <summary>
	/// Before run this tests you must create a database named TinyDalTestDb (see method BuildDataContext)
	/// </summary>
	[TestFixture]
	[Description("It tests the Dal layer.")]
	[Category("Unit")]
	public class EFBaseDataContextTest
	{
		public const int TENANT_ID = 454;     //must be > 0

		#region Mocked Context and Entities

		private class MockDataContext : EFBaseDataContext
		{
			private DbSet<MockEntity> MockEntities { get; set; }
			private DbSet<MockDeletableEntity> MockDeletableEntities { get; set; }
			private DbSet<MockMultiTenantEntity> MockMultiTenantEntities { get; set; }

			public MockEntityRepository MockEntityRepository { get; private set; }
			public MockDeletableEntityRepository MockDeletableEntityRepository { get; private set; }
			public MockMultiTenantEntityRepository MockMultiTenantEntityRepository { get; private set; }

			public MockDataContext(DbContextOptions options, long? tenantId, IsolationLevel isolationLevel) : base(options, tenantId, isolationLevel)
			{
				this.MockEntityRepository = new MockEntityRepository(tenantId, this);
				this.MockDeletableEntityRepository = new MockDeletableEntityRepository(tenantId, this);
				this.MockMultiTenantEntityRepository = new MockMultiTenantEntityRepository(tenantId, this);
			}

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<MockEntity>().ToTable("MockEntities");
				modelBuilder.Entity<MockDeletableEntity>().ToTable("MockDeletableEntities");
				modelBuilder.Entity<MockMultiTenantEntity>().ToTable("MockMultiTenantEntities");
			}
		}

		private class MockEntity : Entity
		{
			[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
			public override long Id { get; set; }

			public string Name { get; set; }
		}

		private class MockDeletableEntity : DeletableEntity
		{
			public override bool IsDeleted { get; set; }

			[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
			public override long Id { get; set; }

			public string Name { get; set; }
		}

		private class MockMultiTenantEntity : MultiTenantEntity
		{
			public override long TenantId { get; set; }

			public override bool IsDeleted { get; set; }

			[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
			public override long Id { get; set; }

			public string Name { get; set; }
		}

		private class MockEntityRepository : EFBaseRepository<MockEntity>
		{
			public MockEntityRepository(long? tenantId, DbContext context) : base(tenantId, context)
			{

			}

			public void InsertRaw(string sql)
			{
				base.ExecuteRaw(sql);
			}
		}
		private class MockDeletableEntityRepository : EFBaseRepository<MockDeletableEntity>
		{
			public MockDeletableEntityRepository(long? tenantId, DbContext context) : base(tenantId, context)
			{

			}
		}

		private class MockMultiTenantEntityRepository : EFBaseRepository<MockMultiTenantEntity>
		{
			public MockMultiTenantEntityRepository(long? tenantId, DbContext context) : base(tenantId, context)
			{

			}
		}

		#endregion

		private Guid _guid;

		public EFBaseDataContextTest()
		{
			this._guid = Guid.NewGuid();
		}

		#region Privates

		private string GetAppDataFolderPath()
		{
			return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "App_Data");
		}

		private MockDataContext BuildDataContext(string databaseEngine, long? tenantId = TENANT_ID, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
		{
			if ("sqlserver".Equals(databaseEngine))
			{
				var optionsBuilder = new DbContextOptionsBuilder<MockDataContext>();
				optionsBuilder.UseSqlServer(@"Server=(localdb)\dev;Database=TinyDalTestDb;Integrated security=True;");
				return new MockDataContext(optionsBuilder.Options, tenantId, isolationLevel);
			}
			else
			{
				var optionsBuilder = new DbContextOptionsBuilder<MockDataContext>();
				optionsBuilder.UseSqlite($@"Filename={this.GetAppDataFolderPath()}/database_{this._guid.ToString("N")}.db;cache=shared");
				return new MockDataContext(optionsBuilder.Options, tenantId, isolationLevel);
			}
		}

		#endregion

		[SetUp]
		public void SetUp()
		{
			if (!Directory.Exists(this.GetAppDataFolderPath()))
			{
				Directory.CreateDirectory(this.GetAppDataFolderPath());
			}

			this._guid = Guid.NewGuid();
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(this.GetAppDataFolderPath()))
			{
				Directory.Delete(this.GetAppDataFolderPath(), true);
			}
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			if (Directory.Exists(this.GetAppDataFolderPath()))
			{
				Directory.Delete(this.GetAppDataFolderPath(), true);
			}
		}

		//
		// Tests
		//

		[Test]
		[Description("It tests a simple transaction rollback.")]
		public void Rollback_ItMustRollbackTheTransaction([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				Assert.AreEqual(TENANT_ID, dataContext.TenantId, "Wrong data context tenant id.");

				//insert

				dataContext.MockEntityRepository.Insert(new MockEntity
				{
					//Id = 1,
					Name = "Name"
				});

				dataContext.Save();

				MockEntity mockEntity = dataContext.MockEntityRepository.FindOneBy(x => x.Name == "Name");

				Assert.IsNotNull(mockEntity, "Entity is null.");
				Assert.AreEqual("Name", mockEntity.Name, "Wrong name.");

				dataContext.Rollback();

				mockEntity = dataContext.MockEntityRepository.FindOneBy(x => x.Name == "Name");

				Assert.IsNull(mockEntity, "Entity is not null.");
			}
		}

		[Test]
		[Description("It tests a basic entity crud operations.")]
		public void Entity_BasicCrud([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				Assert.AreEqual(TENANT_ID, dataContext.TenantId, "Wrong data context tenant id.");

				//insert

				dataContext.MockEntityRepository.Insert(new MockEntity
				{
					//Id = 1,
					Name = "Name"
				});

				dataContext.Save();

				MockEntity mockEntity = dataContext.MockEntityRepository.FindOneBy(x => x.Name == "Name");

				Assert.IsNotNull(mockEntity, "Entity is null.");
				Assert.AreEqual("Name", mockEntity.Name, "Wrong name.");

				//update

				mockEntity.Name = "NAME";

				dataContext.MockEntityRepository.Update(mockEntity);

				dataContext.Save();

				mockEntity = dataContext.MockEntityRepository.FindManyBy(x => x.Name == "NAME").FirstOrDefault();

				Assert.IsNotNull(mockEntity, "Entity is null.");
				Assert.AreEqual("NAME", mockEntity.Name, "Wrong name.");

				//delete

				dataContext.MockEntityRepository.Delete(mockEntity);

				dataContext.Save();

				mockEntity = dataContext.MockEntityRepository.FindOneBy(x => x.Name == "NAME");

				Assert.IsNull(mockEntity, "Entity is not null.");

				dataContext.Rollback();
			}
		}

		[Test]
		[Description("It tests a basic multi tenant entity crud operations.")]
		public void MultiTenantEntity_BasicCrud([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				Assert.AreEqual(TENANT_ID, dataContext.TenantId, "Wrong data context tenant id.");

				//insert

				dataContext.MockMultiTenantEntityRepository.Insert(new MockMultiTenantEntity
				{
					//IsDeleted = false,
					//TenantId = 0,
					//Id = 1,
					Name = "Name"
				});

				dataContext.Save();

				MockMultiTenantEntity mockMultiTenantEntity = dataContext.MockMultiTenantEntityRepository.FindOneBy(x => x.Name == "Name");

				Assert.IsNotNull(mockMultiTenantEntity, "Entity is null.");
				Assert.AreEqual(TENANT_ID, mockMultiTenantEntity.TenantId, "Wrong tenant id.");
				Assert.AreEqual("Name", mockMultiTenantEntity.Name, "Wrong name.");

				//update (range)

				mockMultiTenantEntity.Name = "NAME";

				dataContext.MockMultiTenantEntityRepository.Update(new List<MockMultiTenantEntity>() { mockMultiTenantEntity });

				dataContext.Save();

				mockMultiTenantEntity = dataContext.MockMultiTenantEntityRepository.FindManyBy(x => x.Name == "NAME").FirstOrDefault();

				Assert.IsNotNull(mockMultiTenantEntity, "Entity is null.");
				Assert.AreEqual("NAME", mockMultiTenantEntity.Name, "Wrong name.");

				//delete

				dataContext.MockMultiTenantEntityRepository.Delete(new List<MockMultiTenantEntity>() { mockMultiTenantEntity });

				dataContext.Save();

				mockMultiTenantEntity = dataContext.MockMultiTenantEntityRepository.FindOneBy(x => x.Name == "NAME");

				Assert.IsNull(mockMultiTenantEntity, "Entity is not null.");

				dataContext.Rollback();
			}
		}

		[Test]
		[Description("It tests a basic entity async crud operations.")]
		public async Task Entity_BasicAsyncCrud([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				Assert.AreEqual(TENANT_ID, dataContext.TenantId, "Wrong data context tenant id.");

				//insert

				dataContext.MockEntityRepository.Insert(new MockEntity
				{
					//Id = 1,
					Name = "Name"
				});

				await dataContext.SaveAsync();

				MockEntity mockEntity = await dataContext.MockEntityRepository.FindOneByAsync(x => x.Name == "Name");

				Assert.IsNotNull(mockEntity, "Entity is null.");
				Assert.AreEqual("Name", mockEntity.Name, "Wrong name.");

				//update

				mockEntity.Name = "NAME";

				dataContext.MockEntityRepository.Update(mockEntity);

				await dataContext.SaveAsync();

				mockEntity = (await dataContext.MockEntityRepository.FindManyByAsync(x => x.Name == "NAME")).FirstOrDefault();

				Assert.IsNotNull(mockEntity, "Entity is null.");
				Assert.AreEqual("NAME", mockEntity.Name, "Wrong name.");

				//delete

				dataContext.MockEntityRepository.DeleteBy(x => "NAME".Equals(x.Name));

				await dataContext.SaveAsync();

				mockEntity = await dataContext.MockEntityRepository.FindOneByAsync(x => x.Name == "NAME");

				Assert.IsNull(mockEntity, "Entity is not null.");

				await dataContext.RollbackAsync();
			}
		}

		[Test]
		[Description("It tests a basic multi tenant entity async crud operations.")]
		public async Task MultiTenantEntity_BasicAsyncCrud([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				Assert.AreEqual(TENANT_ID, dataContext.TenantId, "Wrong data context tenant id.");

				//insert

				dataContext.MockMultiTenantEntityRepository.Insert(new MockMultiTenantEntity
				{
					//IsDeleted = false,
					//TenantId = 0,
					//Id = 1,
					Name = "Name"
				});

				await dataContext.SaveAsync();

				MockMultiTenantEntity mockMultiTenantEntity = await dataContext.MockMultiTenantEntityRepository.FindOneByAsync(x => x.Name == "Name");

				Assert.IsNotNull(mockMultiTenantEntity, "Entity is null.");
				Assert.AreEqual(TENANT_ID, mockMultiTenantEntity.TenantId, "Wrong tenant id.");
				Assert.AreEqual("Name", mockMultiTenantEntity.Name, "Wrong name.");

				//update

				mockMultiTenantEntity.Name = "NAME";

				dataContext.MockMultiTenantEntityRepository.Update(new List<MockMultiTenantEntity>() { mockMultiTenantEntity });

				await dataContext.SaveAsync();

				mockMultiTenantEntity = (await dataContext.MockMultiTenantEntityRepository.FindManyByAsync(x => x.Name == "NAME")).FirstOrDefault();

				Assert.IsNotNull(mockMultiTenantEntity, "Entity is null.");
				Assert.AreEqual("NAME", mockMultiTenantEntity.Name, "Wrong name.");

				//delete

				dataContext.MockMultiTenantEntityRepository.DeleteManyBy(x => "NAME".Equals(x.Name));

				await dataContext.SaveAsync();

				mockMultiTenantEntity = await dataContext.MockMultiTenantEntityRepository.FindOneByAsync(x => x.Name == "NAME");

				Assert.IsNull(mockMultiTenantEntity, "Entity is not null.");

				await dataContext.RollbackAsync();
			}
		}

		[Test]
		[Description("It tests a basic concurrent transaction operations.")]
		public void Entity_ConcurrentTransactions([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			var guid = Guid.NewGuid();

			string entityName = "Name_" + guid.ToString();

			using (var dataContext1 = this.BuildDataContext(databaseEngine))
			using (var dataContext2 = this.BuildDataContext(databaseEngine, TENANT_ID, IsolationLevel.ReadUncommitted))
			{
				Assert.AreEqual(TENANT_ID, dataContext1.TenantId, "Wrong data context 1 tenant id.");
				Assert.AreEqual(TENANT_ID, dataContext2.TenantId, "Wrong data context 2 tenant id.");

				//insert

				dataContext1.MockEntityRepository.Insert(new List<MockEntity>() { new MockEntity
				{
					//Id = 1,
					Name = entityName
				}});

				dataContext1.Save();

				MockEntity mockEntity1 = dataContext1.MockEntityRepository.FindOneBy(x => x.Name == entityName);

				Assert.IsNotNull(mockEntity1, "Entity is null.");
				Assert.AreEqual(entityName, mockEntity1.Name, "Wrong name.");

				Task context2Task = new Task(async () =>
				{
					MockEntity mockEntity2 = await dataContext2.MockEntityRepository.FindOneByAsync(x => x.Name == entityName, true);

					Assert.IsNotNull(mockEntity2, "Entity is not visible to the transaction 2 in read uncommitted.");
				});

				context2Task.Start();

				Task.Delay(2000).Wait();

				context2Task.Wait();

				dataContext1.Rollback();

				MockEntity mockEntity2 = dataContext2.MockEntityRepository.FindOneBy(x => x.Name == entityName);

				Assert.IsNull(mockEntity2, "Entity is visible to the transaction 2 after rollback.");
			}

			//cleanup

			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				dataContext.MockMultiTenantEntityRepository.DeleteManyBy(x => entityName.Equals(x.Name));

				dataContext.Save();

				dataContext.Commit();

				MockEntity mockEntity2 = dataContext.MockEntityRepository.FindOneBy(x => x.Name == entityName);

				Assert.IsNull(mockEntity2, "Cleanup error.");
			}
		}

		[Test]
		[Description("It tests a basic concurrent transaction async operations.")]
		public async Task Entity_ConcurrentAsyncTransactions([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			var guid = Guid.NewGuid();

			string entityName = "Name_" + guid.ToString();

			using (var dataContext1 = this.BuildDataContext(databaseEngine))
			using (var dataContext2 = this.BuildDataContext(databaseEngine, TENANT_ID, IsolationLevel.ReadUncommitted))
			{
				Assert.AreEqual(TENANT_ID, dataContext1.TenantId, "Wrong data context 1 tenant id.");
				Assert.AreEqual(TENANT_ID, dataContext2.TenantId, "Wrong data context 2 tenant id.");

				//insert

				dataContext1.MockEntityRepository.Insert(new List<MockEntity>() { new MockEntity
				{
					//Id = 1,
					Name = entityName
				}});

				await dataContext1.SaveAsync();

				MockEntity mockEntity1 = (await dataContext1.MockEntityRepository.FindManyByAsync(x => x.Name == entityName, true)).FirstOrDefault();

				Assert.IsNotNull(mockEntity1, "Entity is null.");
				Assert.AreEqual(entityName, mockEntity1.Name, "Wrong name.");

				Task context2Task = new Task(async () =>
				{
					MockEntity mockEntity2 = await dataContext2.MockEntityRepository.FindOneByAsync(x => x.Name == entityName);

					Assert.IsNotNull(mockEntity2, "Entity is not visible to the transaction 2 in read uncommitted.");
				});

				context2Task.Start();

				await Task.Delay(2000);

				await context2Task;

				await dataContext1.RollbackAsync();

				MockEntity mockEntity2 = await dataContext2.MockEntityRepository.FindOneByAsync(x => x.Name == entityName);

				Assert.IsNull(mockEntity2, "Entity is visible to the transaction 2 after rollback.");
			}

			//cleanup

			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				dataContext.MockMultiTenantEntityRepository.DeleteManyBy(x => entityName.Equals(x.Name));

				await dataContext.SaveAsync();

				await dataContext.CommitAsync();

				MockEntity mockEntity2 = await dataContext.MockEntityRepository.FindOneByAsync(x => x.Name == entityName);

				Assert.IsNull(mockEntity2, "Cleanup error.");
			}
		}

		[Test]
		[Description("It tests tenant less basic crud operations.")]
		public void TenantLessDataContext_BasicCrud([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			using (var dataContext = this.BuildDataContext(databaseEngine, null, IsolationLevel.ReadUncommitted))
			{
				Assert.IsNull(dataContext.TenantId, "Wrong data context tenant id.");

				//insert

				dataContext.MockMultiTenantEntityRepository.Insert(new MockMultiTenantEntity
				{
					//IsDeleted = false,
					//TenantId = 0,
					//Id = 1,
					Name = "Name"
				});

				dataContext.Save();

				MockMultiTenantEntity mockMultiTenantEntity = dataContext.MockMultiTenantEntityRepository.FindOneBy(x => x.Name == "Name");

				Assert.IsNotNull(mockMultiTenantEntity, "Entity is null.");
				Assert.AreEqual(0, mockMultiTenantEntity.TenantId, "Wrong tenant id after update.");
				Assert.AreEqual("Name", mockMultiTenantEntity.Name, "Wrong name.");

				//update

				mockMultiTenantEntity.Name = "NAME";
				mockMultiTenantEntity.TenantId = TENANT_ID;

				dataContext.MockMultiTenantEntityRepository.Update(mockMultiTenantEntity);

				dataContext.Save();

				mockMultiTenantEntity = dataContext.MockMultiTenantEntityRepository.FindOneBy(x => x.Name == "NAME");

				Assert.IsNotNull(mockMultiTenantEntity, "Entity is null.");
				Assert.AreEqual("NAME", mockMultiTenantEntity.Name, "Wrong name.");
				Assert.AreEqual(TENANT_ID, mockMultiTenantEntity.TenantId, "Wrong tenant id after update.");

				dataContext.Rollback();
			}
		}

		[Test]
		[Description("It tests the IsDeleted logic.")]
		public void IsDeleted_MustHandleItInAutomaticAndHiddenWay([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				Assert.AreEqual(TENANT_ID, dataContext.TenantId, "Wrong data context tenant id.");

				//insert

				dataContext.MockDeletableEntityRepository.Insert(new MockDeletableEntity
				{
					//Id = 1,
					IsDeleted = true,
					Name = "Name"
				});

				dataContext.Save();

				MockDeletableEntity mockDeletableEntity = dataContext.MockDeletableEntityRepository.FindOneBy(x => x.Name == "Name");

				Assert.IsNull(mockDeletableEntity, "Entity is not null.");

				//update

				mockDeletableEntity = dataContext.MockDeletableEntityRepository.FindOneBy(x => x.Name == "Name" && x.IsDeleted == true, false, true);

				mockDeletableEntity.Name = "NAME";
				mockDeletableEntity.IsDeleted = false;

				dataContext.MockDeletableEntityRepository.Update(mockDeletableEntity);

				dataContext.Save();

				mockDeletableEntity = dataContext.MockDeletableEntityRepository.FindManyBy(x => x.Name == "NAME").FirstOrDefault();

				Assert.IsNotNull(mockDeletableEntity, "Entity is null.");
				Assert.AreEqual("NAME", mockDeletableEntity.Name, "Wrong name.");

				dataContext.Rollback();
			}
		}

		[Test]
		[Description("It tests the delete by id method.")]
		public void DeleteById_ItDeletesTheRecordWithoutReadIt([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				Assert.AreEqual(TENANT_ID, dataContext.TenantId, "Wrong data context tenant id.");

				//insert

				dataContext.MockDeletableEntityRepository.Insert(new MockDeletableEntity
				{
					//Id = 1,
					Name = "Name"
				});

				dataContext.Save();

				MockDeletableEntity mockDeletableEntity = dataContext.MockDeletableEntityRepository.FindOneBy(x => x.Name == "Name");

				Assert.IsNotNull(mockDeletableEntity, "Entity is null.");

				//delete

				dataContext.MockDeletableEntityRepository.DeleteById(mockDeletableEntity.Id);

				dataContext.Save();

				mockDeletableEntity = dataContext.MockDeletableEntityRepository.FindOneBy(x => x.Name == "Name");

				Assert.IsNull(mockDeletableEntity, "Entity is not null.");

				dataContext.Rollback();
			}
		}

		[Test]
		[Description("It tests the delete by id method that must attach the entity before delete it.")]
		public void DeleteById_AttachBeforeDelete([Values("sqlserver", "sqlite")]string databaseEngine)
		{
			var guid = Guid.NewGuid();

			string entityName = "Name_" + guid.ToString();

			using (var dataContext = this.BuildDataContext(databaseEngine))
			{
				dataContext.MockEntityRepository.InsertRaw($"insert into MockEntities (Name) values ('{entityName}')");

				MockEntity mockEntity = dataContext.MockEntityRepository.FindOneBy(x => x.Name == entityName, true);

				Assert.IsNotNull(mockEntity, "Entity is null.");

				//delete

				dataContext.MockEntityRepository.DeleteById(mockEntity.Id);

				dataContext.Save();

				mockEntity = dataContext.MockEntityRepository.FindOneBy(x => x.Name == entityName);

				Assert.IsNull(mockEntity, "Entity is not null.");

				dataContext.Rollback();
			}
		}
	}
}
