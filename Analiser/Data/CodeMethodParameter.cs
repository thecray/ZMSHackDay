namespace Analiser.Data
{
    public class CodeMethodParameter : IEquatable<CodeMethodParameter>
    {
        private int _hash = 0;

        public CodeType Type { get; set; }
        public string Name { get; set; }

        public CodeMethodParameter(CodeType type, string name)
        {
            Type = type;
            Name = name;
        }

        public bool Equals(CodeMethodParameter? other)
        {
            if (other == null)
            {
                return false;
            }

            return other.GetHashCode() == GetHashCode();
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
