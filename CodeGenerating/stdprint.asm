format elf64

section '.text' writeable executable

extrn printf
extrn main

public _start
_start:
    call main

    mov rdi, rax
    mov rax, 60
    syscall

public stdprint
stdprint:
    mov rax, 1
    mov rdi, 1
    mov rsi, [rsp + 8]
    mov rdx, [rsp + 16]
    syscall
    ret
