using System;
using Rant.Vocabulary;

namespace Rant
{
    public partial class RantEngine
    {
        private IRantVocabulary _vocabulary;

        /// <summary>
        /// The vocabulary associated with this instance.
        /// </summary>
        public IRantVocabulary Vocabulary
        {
            get { return _vocabulary; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _vocabulary = value;
            }
        }

        private void LoadVocab(string path, NsfwFilter filter)
        {
            if (_vocabulary != null) return;

            if (String.IsNullOrEmpty(path))
            {
                _vocabulary = new RantVocabulary(null);
                return;
            }

            _vocabulary = RantVocabulary.FromDirectory(path, filter);
        }
    }
}