namespace Cml.Parsing;

internal class JustAssign(Executable address, Executable value, Location location) : Assign(address, value, location);