namespace Analiser.Data
{
    public class CodeType : IEquatable<CodeType>
    {
        private int _hash = 0;

        public static CodeType Empty => new CodeType(string.Empty, string.Empty);
        public static CodeType Void => new CodeType(string.Empty, "void");

        public string Namespace { get; set; }
        public string Name { get; set; }

        public string FullName => Namespace == string.Empty ? Name : $"{Namespace}.{Name}";

        public bool IsDataTorque => Namespace.StartsWith("DataTorque.");
        public bool IsIho => Namespace.StartsWith("DataTorque.Iho");
        public bool IsClient => IsDataTorque && !IsIho;

        public CodeType(string @namespace, string name)
        {
            if (@namespace == "<global namespace>")
            {
                Namespace = "Unlinked";
            }
            else
            {
                Namespace = @namespace;
            }

            Name = name;
        }

        public bool Equals(CodeType? other)
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

        public override string ToString() => FullName;

        public void BuildHash()
        {
            _hash = HashCode.Combine(FullName);
        }

        internal static CodeType Parse(string value)
        {
            value = value.Trim();
            if (value == string.Empty)
            {
                return Empty;
            }

            if (value == "void")
            {
                return Void;
            }

            string[] parts = value.Split('.');
            if (parts.Length == 1)
            {
                return new CodeType(string.Empty, parts[0]);
            }

            string ns = string.Join(".", parts[..^1]);
            string name = parts[^1];
            return new CodeType(ns, name);
        }
    }
}
