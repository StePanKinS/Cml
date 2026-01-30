using Cml.Lexing;
using Cml.Types;

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
) : Executable(new Pointer(DefaultType.Char), Location);

public record EnumStaticMethodValue
(
    EnumType EnumType,
    string MethodName,
    Location Location
) : Executable(DefaultType.Void, Location);

public record EnumInstanceMethodValue
(
    Executable Operand,
    EnumType EnumType,
    string MethodName,
    Location Location
) : Executable(DefaultType.Void, Location);
