namespace Analiser.Data
{
    public class CodeClass : IEquatable<CodeClass>
    {
        public static CodeClass Empty => new CodeClass(string.Empty, string.Empty, null, []);

        private int _hash = 0;

        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string FullName => $"{Namespace}.{Name}";

        public CodeType? BaseType { get; set; } = null;

        public CodeType[] InterfaceTypes { get; set; } = [];

        public bool IsDataTorque => Namespace.StartsWith("DataTorque.");
        public bool IsIho => Namespace.StartsWith("DataTorque.Iho");
        public bool IsClient => IsDataTorque && !IsIho;

        public List<CodeProperty> Properties { get; set; } = new List<CodeProperty>();
        public List<CodeMethod> Methods { get; set; } = new List<CodeMethod>();

        public CodeClass(string @namespace, string name, CodeType? baseType, CodeType[] interfaceTypes)
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
            BaseType = baseType;
            InterfaceTypes = interfaceTypes;
        }

        public bool Equals(CodeClass? other)
        {
            if (other == null)
            {
                return false;
            }

            return other.GetHashCode() == GetHashCode();
        }

        public void BuildHash()
        {
            _hash = HashCode.Combine(FullName, BaseType);

            foreach (CodeType interfaceType in InterfaceTypes)
            {
                interfaceType.BuildHash();
                _hash = HashCode.Combine(_hash, interfaceType);
            }

            foreach (CodeProperty property in Properties)
            {
                property.BuildHash();
                _hash = HashCode.Combine(_hash, property);
            }

            foreach (CodeMethod method in Methods)
            {
                method.BuildHash();
                _hash = HashCode.Combine(_hash, method);
            }
        }

        public override int GetHashCode()
        {
            if (_hash == 0)
            {
                BuildHash();
            }

            return _hash;
        }
    }
}
