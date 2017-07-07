using Rant.Core.Utilities;
using System;

namespace Rant.Vocabulary
{
    public sealed class EntryClass
    {
        private readonly string _name;

        public EntryClass(string name)
        {
            if (!Util.ValidateName(name))
                throw new ArgumentException($"Invalid class name: {name}");
            _name = name.ToLowerInvariant();
        }

        public string Name => _name;
        
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return ReferenceEquals(_name, obj);
        }
        
        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }
    }
}
