# Primitive types
## Integer
### Unsigned
u8, u16, u32, u64
### Signed
s8, s16, s32, s64
## Floating point
f32, f64
## Boolean
bool
## Pointers
void*, s32*, bool*, ...

# Literals
## Integer
4567654, 0b101010101, 0x27ab9d
## Floaing point
34.123
## Boolenan
true, false

# Strucures
struct `name` {  
    `type` `field name`,  
    `type` `field2 name`,  
    ...
}

# Functions
`return type` `function name`(`type` `argument name`, `type` `agrument2 name`, ...) \{ `code` \}

# Imports
import `file name`;

# Function pointers
?

# Operators
## Binary operators
+, -, *, /, %, &, &&, |, ||, ^, ~, <<, >>, ==, !=, \<=, >=, <, >, =, +=, -=, *=, /=, %=, <<=, >>=, &=, |=, ^=
## Unary operators
-, !, &, *, ++, --

# Operation priority
1. Left to right
    - func()
    - [] (value at index)
    - . (member access)
1. Right to lefft
    - ++ (prefix)
    - \-- (prefix)
    - (`type`) (cast)
    - \- (unary)
    - !
    - ~
    - &
    - \* (dereference)
1. Left to right
    - \*
    - /
    - %
1. Left to right
    - \+
    - \- 
1. Left to right
    - <<
    - \>>
1. Left to right
    - <
    - <=
    - \>=
    - \>
1. Left to right
    - ==
    - !=
1. Left to right
    - & (bitwise and)
1. Left to right
    - ^
1. Left to right
    - | (bitwise or)
1. Left to right
    - && 
1. Left to right
    - ||
1. Right to left
    - =
    - +=
    - -=
    - *=
    - /=
    - %=
    - <<=
    - \>>=
    - &=
    - |=,
    - ^=
1. Left to right
    - ++ (postfix)
    - \-- (postfix)
