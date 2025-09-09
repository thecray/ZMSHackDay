namespace Analiser.Data
{
    public class CodeExpression : IEquatable<CodeExpression>
    {
        private int _hash = 0;

        public CodeType Type { get; set; }
        public string Name { get; set; }

        public CodeExpression(CodeType type, string name)
        {
            Type = type;
            Name = name;
        }

        public override string ToString()
        {
            return Type.ToString() + "::" + Name;
        }

        public override int GetHashCode()
        {
            if (_hash == 0)
            {
                BuildHash();
            }

            return _hash;
        }

        public bool Equals(CodeExpression? other)
        {
            if (other == null)
            {
                return false;
            }

            return other.GetHashCode() == GetHashCode();
        }

        public void BuildHash()
        {
            _hash = HashCode.Combine(Type, Name);
        }
    }
}
