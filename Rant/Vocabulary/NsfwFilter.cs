namespace Rant.Vocabulary
{
    /// <summary>
    /// Used to instruct the dictionary loader if entries marked as NSFW should be loaded.
    /// </summary>
    public enum NsfwFilter
    {
        /// <summary>
        /// Allow NSFW entries.
        /// </summary>
        Allow,
        /// <summary>
        /// Disallow NSFW entries.
        /// </summary>
        Disallow
    }
}