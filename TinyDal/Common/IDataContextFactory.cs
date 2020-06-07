namespace TinyDal.Common
{
	public interface IDataContextFactory
	{
		IDataContext CreateContext();
	}
}