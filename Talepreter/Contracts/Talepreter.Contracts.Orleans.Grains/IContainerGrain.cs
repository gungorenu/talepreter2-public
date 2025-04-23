namespace Talepreter.Contracts.Orleans.Grains
{
    /// <summary>
    /// an interface for container grains only, not supposed to be used elsewhere
    /// </summary>
    public interface IContainerGrain: IGrainWithStringKey
    {
        /// <summary>
        /// initializes a publish, creates collection entries, first objects and indexes if necessary
        /// </summary>
        Task InitializePublish(Guid taleId, Guid taleVersionId);

        /// <summary>
        /// deletes all command data
        /// </summary>
        Task Purge(Guid taleId, Guid taleVersionId);

        /// <summary>
        /// Backs up existing tale version data to new version
        /// </summary>
        Task BackupTo(Guid taleId, Guid taleVersionId, Guid newVersionId);
    }
}
