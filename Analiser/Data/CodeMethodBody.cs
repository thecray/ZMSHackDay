namespace Analiser.Data
{
    public class CodeMethodBody : IEquatable<CodeMethodBody>
    {
        public static CodeMethodBody Empty = new CodeMethodBody();

        private int _hash = 0;
        public List<CodeType> ReferencedTypes { get; set; } = new List<CodeType>();
        public List<string> ReferencedStoredProcedures { get; set; } = new List<string>();
        public List<CodeExpression> ReferencedExpressions { get; set; } = new List<CodeExpression>();

        public string BodyText { get; set; } = string.Empty;
        public string OriginalBodyText { get; set; } = string.Empty;

        public bool Equals(CodeMethodBody? other)
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
            _hash = BodyText.GetHashCode();

            foreach (CodeType referencedType in ReferencedTypes)
            {
                referencedType.BuildHash();
                _hash = HashCode.Combine(_hash, referencedType);
            }

            foreach (string storedProcedure in ReferencedStoredProcedures)
            {
                _hash = HashCode.Combine(_hash, storedProcedure);
            }

            foreach (CodeExpression referencedExpression in ReferencedExpressions)
            {
                referencedExpression.BuildHash();
                _hash = HashCode.Combine(_hash, referencedExpression);
            }
        }
    }
}
