namespace TinyDal.Common
{
	public abstract class MultiTenantEntity : DeletableEntity
	{
		public abstract long TenantId { get; set; }
	}
}
