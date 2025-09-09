namespace Analiser.Data
{
    public class CodeProject : IEquatable<CodeProject>
    {
        private int _hash = 0;

        public string Name { get; set; } = string.Empty;
        public List<CodeClass> Classes { get; set; } = new List<CodeClass>();

        public CodeProject(string name)
        {
            Name = name;
        }

        public void BuildHash()
        {
            _hash = Name.GetHashCode();

            foreach (CodeClass c in Classes)
            {
                c.BuildHash(); // force it to recreate
                _hash = HashCode.Combine(_hash, c);
            }
        }

        public bool Equals(CodeProject? other)
        {
            if (other == null)
            {
                return false;
            }

            return other.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return Name;
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
