format elf64

section '.text' writeable executable

extrn printf

public stdprint
stdprint:
    mov rax, 1
    mov rdi, 1
    mov rsi, [rsp + 8]
    mov rdx, [rsp + 16]
    syscall
    ret
