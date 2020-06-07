namespace TinyDal.Common
{
	public abstract class DeletableEntity : Entity
	{
		public abstract bool IsDeleted { get; set; }
	}
}
