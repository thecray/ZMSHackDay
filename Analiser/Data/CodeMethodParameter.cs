namespace Analiser.Data
{
    public class CodeMethodParameter : IEquatable<CodeMethodParameter>
    {
        public static CodeMethodParameter Empty => new CodeMethodParameter(CodeType.Empty, string.Empty);

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

        internal static CodeMethodParameter Parse(string value)
        {
            value = value.Trim();
            if (value == string.Empty)
            {
                return Empty;
            }

            string[] parts = value.Split(' ');
            if (parts.Length == 1)
            {
                return new CodeMethodParameter(CodeType.Empty, parts[0]);
            }

            if (parts.Length == 2)
            {
                CodeType codeType = CodeType.Parse(parts[0]);

                string name = parts[1];
                return new CodeMethodParameter(codeType, name);
            }

            throw new Exception($"Invalid CodeProperty value '{value}'");
        }
    }
}
