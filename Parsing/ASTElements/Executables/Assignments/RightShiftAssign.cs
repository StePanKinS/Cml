namespace Cml.Parsing;

internal class RightShiftAssign(Executable address, Executable value, Location location) : Assign(address, value, location);