namespace Cml.Parsing.Executables;

public abstract record Executable
(
    Typ ReturnType,
    Location Location
) : Location.ILocatable;

public record UnaryOperation
(
    UnaryOperationTypes OperationType,
    Executable Operand,
    Typ ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record BinaryOperation
(
    BinaryOperationTypes OperationType,
    Executable Left,
    Executable Right,
    Typ ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record FunctionCall
(
    Executable FunctionPointer,
    Executable[] Args,
    Typ ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record CodeBlock
(
    Executable[] Code,
    LocalVariables Locals,
    Location Location
) : Executable(DefaultType.Void, Location);

public record GetMember
(
    Executable Operand,
    Token<string> Member,
    Typ ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record GetElement
(
    Executable Operand,
    Executable Index,
    Typ ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record Identifyer
(
    Definition Definition,
    Typ ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record PostIncrement
(
    Executable Operand,
    bool IsDecrement,
    Typ ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record Literal<T>
(
    T Value,
    Typ ReturnType,
    Location Location
) : Executable(ReturnType, Location);

public record ControlFlow
(
    Executable Condition,
    Executable Body,
    Executable? ElseBody,
    Location Location
) : Executable(DefaultType.Void, Location);

public record WhileLoop
(
    Executable Condition,
    Executable Body,
    Location Location
) : Executable(DefaultType.Void, Location);

public record Return
(
    Executable Value,
    Location Location
) : Executable(DefaultType.Void, Location);

public record Nop
(
    Location Location
) : Executable(DefaultType.Void, Location);

public record NamespaceValue
(
    NamespaceDefinition Namespace,
    Location Location
) : Executable(DefaultType.Void, Location);

public record TypeValue
(
    Typ Type,
    Location Location
) : Executable(DefaultType.Void, Location);
