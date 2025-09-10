namespace Analiser.Data
{
    public class CodeMethod : IEquatable<CodeMethod>
    {
        public static CodeMethod Empty => new CodeMethod([], CodeType.Void, string.Empty, string.Empty);

        private int _hash = 0;

        public string[] Modifiers { get; set; }
        public CodeType ReturnType { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }

        public List<CodeMethodParameter> Parameters { get; set; } = new List<CodeMethodParameter>();

        public CodeMethodBody Body { get; set; } = new CodeMethodBody();

        public CodeMethod(string[] modifiers, CodeType returnType, string name, string hash)
        {
            Modifiers = modifiers;
            ReturnType = returnType;
            Name = name;
            Hash = hash;
        }

        public bool Equals(CodeMethod? other)
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
            ReturnType.BuildHash();
            Body.BuildHash();

            _hash = HashCode.Combine(ReturnType, Name, Body, Hash);

            foreach (string modifier in Modifiers)
            {
                _hash = HashCode.Combine(_hash, modifier);
            }

            foreach (CodeMethodParameter parameter in Parameters)
            {
                parameter.BuildHash();
                _hash = HashCode.Combine(_hash, parameter);
            }
        }
    }
}
