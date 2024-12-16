namespace Cml.Parsing;

internal class LeftShiftAssign(Executable address, Executable value, Location location) : Assign(address, value, location);