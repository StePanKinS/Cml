namespace Cml.Parsing;

internal class AndAssign(Executable address, Executable value, Location location) : Assign(address, value, location);