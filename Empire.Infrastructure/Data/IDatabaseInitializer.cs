namespace Empire.Infrastructure.Data
{
    /// <summary>
    /// Interface for database initialization and migration services
    /// </summary>
    public interface IDatabaseInitializer
    {
        /// <summary>
        /// Initialize the database by applying migrations and seeding initial data
        /// </summary>
        Task InitializeAsync();
    }
}
