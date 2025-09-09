namespace Analiser.Data
{
    public class CodeProperty : IEquatable<CodeProperty>
    {
        private int _hash = 0;

        public CodeType Type { get; set; }

        public string Name { get; set; }

        public CodeProperty(CodeType type, string name)
        {
            Type = type;
            Name = name;
        }

        public bool Equals(CodeProperty? other)
        {
            if (other == null)
            {
                return false;
            }

            return other.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return $"{Type.FullName}.{Name}";
        }

        public override int GetHashCode()
        {
            if (_hash == 0)
            {
                BuildHash();
            }

            return _hash;
        }

        public void BuildHash()
        {
            _hash = HashCode.Combine(Type, Name);
        }
    }
}
