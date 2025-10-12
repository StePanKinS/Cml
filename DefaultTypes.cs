namespace Cml;

public class DefaultType : StructDefinition
{
    public static DefaultType Void { get; } = new("void", 0);
    public static DefaultType Char { get; } = new("char", 1);
    public static DefaultType Bool { get; } = new("bool", 8);

    protected DefaultType(string name, int size)
        : base(name, size, [], null!, [], Location.Nowhere)
    {
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj);

    public override int GetHashCode()
        => base.GetHashCode();

    public override string ToString()
        => $"DefaultType({Name})";


    public class Integer : DefaultType
    {
        public static Integer SByte { get; } = new("sbyte", 1, true);
        public static Integer Short { get; } = new("short", 2, true);
        public static Integer Int { get; } = new("int", 4, true);
        public static Integer Long { get; } = new("long", 8, true);

        public static Integer Byte { get; } = new("byte", 1, false);
        public static Integer UShort { get; } = new("ushort", 2, false);
        public static Integer UInt { get; } = new("uint", 4, false);
        public static Integer ULong { get; } = new("ulong", 8, false);

        public static Integer Lit { get; } = new("!UnknownSizeInt", 0, false);

        public bool IsSigned;

        protected Integer(string name, int size, bool isSigned)
            : base(name, size)
        {
            IsSigned = isSigned;
        }

        public static Integer GetObject(int size, bool isSigned)
            => (size, isSigned) switch
                {
                    (1, true) => SByte,
                    (2, true) => Short,
                    (4, true) => Int,
                    (8, true) => Long,
                    (1, false) => Byte,
                    (2, false) => UShort,
                    (4, false) => UInt,
                    (8, false) => ULong,
                    _ => throw new Exception("Invalid integer size"),
                };

        public override string ToString()
            => $"{(IsSigned ? "" : "U")}Integer({size})";
    }
}