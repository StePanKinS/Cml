namespace Cml.Parsing.Executables;

public record EnumMemberAccess
(
    EnumType EnumType,
    string MemberName,
    long Value,
    Location Location
) : Executable(EnumType, Location);

public record EnumOfMethod
(
    EnumType EnumType,
    Executable NameArgument,
    Location Location
) : Executable(EnumType, Location);

public record EnumNameMethod
(
    EnumType EnumType,
    Executable EnumValue,
    Location Location
) : Executable(new Pointer(DefaultType.Integer.Byte), Location);

public record EnumMethodValue
(
    EnumType EnumType,
    Executable? Operand,
    string MethodName,
    Location Location
) : Executable(DefaultType.Void, Location);
