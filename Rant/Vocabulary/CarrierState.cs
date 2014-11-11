namespace Rant.Dictionaries
{
    /// <summary>
    /// Maintains carrier synchronization states for a particular vocabulary during the execution of a pattern.
    /// </summary>
    public sealed class CarrierState
    {
        private readonly Vocabulary _vocabulary;

        public Vocabulary Vocabulary
        {
            get { return _vocabulary; }
        }

        public CarrierState(Vocabulary vocabulary)
        {
            _vocabulary = vocabulary;
        }
    }
}