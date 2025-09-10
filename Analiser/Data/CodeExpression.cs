namespace Analiser.Data
{
    public class CodeExpression : IEquatable<CodeExpression>
    {
        public static CodeExpression Empty => new CodeExpression(CodeType.Empty, string.Empty);

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

        internal static CodeExpression Parse(string value)
        {
            value = value.Trim();
            if (value == string.Empty)
            {
                return Empty;
            }

            string[] parts = value.Split(' ');
            if (parts.Length == 1)
            {
                return new CodeExpression(CodeType.Empty, parts[0]);
            }

            if (parts.Length == 2)
            {
                CodeType codeType = CodeType.Parse(parts[0]);

                string name = parts[1];
                return new CodeExpression(codeType, name);
            }

            throw new Exception($"Invalid CodeExpression value '{value}'");
        }
    }
}
