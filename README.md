
# BFC

This is a [Brainfuck](https://en.wikipedia.org/wiki/Brainfuck) compiler that can target multiple languages and is easily extendable.

*The word Brainfuck will be abbreviated as BF further down*

## About BF

BF is a very basic esoteric programming language.
It is [turing complete](https://en.wikipedia.org/wiki/Turing_completeness), meaning you can theoretically solve every possible computational problem with it. While you're not supposed to use it (yet some people do), it's simplicity makes it a good candidate to check if another language is turing complete.
No matter how this other language works, if you can compile a BF program into this language (or any program in this language to BF), it's also turing complete.

BF consists of 8 instructions that directly map to common code constructs in many other languages:

| BF | C# |
| -- | -- |
| `+` | `++mem[ptr];` |
| `-` | `--mem[ptr];` |
| `>` | `++ptr;` |
| `<` | `--ptr;` |
| `[` | `while(mem[ptr]!=0){` |
| `]` | `}` |
| `.` | `Console.Write((char)mem[ptr]);` |
| `,` | `mem[ptr]=(byte)Console.ReadKey().KeyChar;` |

### Specs

- BF expects the memory to be set to zero.
- Before the BF code runs, `var mem=new byte[30000];int ptr=0;` is executed.
- After the BF code, `return (int)mem[ptr];` is executed (not part of BF, but allows error reporting by a BF program).
- Any unsupported character encountered in a BF file is considered a comment and is discarded.
- overflow behavior of memory cells is undefined.
- `ptr` does not wrap around and will simply go out of bounds.

### Defaults

The original BF compiler was written in C.
It used a `char` array of 30'000 elements as memory.
All built-in engines will default to that but some allow customization via arguments.
Your custom engines should follow this principle too.

The original BF compiler made no special distinction between signed and unsigned.
Signed overflow is undefined behavior in C, so you should default to an unsigned 8-bit data type unless you need something else.
Do not use data types that are too large.
The only way to make a number larger in BF is to add 1 to it.
A signed 32 bit memory cell takes over 2 billion increments to fill up.

## Usage

This is a command line utility that has 3 different ways of using it:

### Compiling BF code

An example file is provided in the bf directory.

    bfc [/y] [/o] /e engine [/a arg [/a ...]] <infile> [outfile]

- **/y**: Suppresses prompts to overwrite the destination
- **/o**: Optimizes the code (see "Optimizer"  below)
- **/e engine**: Selects the engine used to compile the code (see "List Engines" below)
- **/a arg**: Supplies an argument to the engine (repeatable for each engine argument).
- **infile**: File containing BF code
- **outfile**: File where the output is written to. If missing, replaces the file extension of `infile` with the one supplied by the engine.

The `outfile` argument must be anywhere after the `infile` argument.
Apart from this, the order of arguments is irrelevant.

### List Engines

    bfc /l

This command lists all engines available to the compiler.
It lists built-in engines first and then all externally loaded engines.
It shows the name (this is used for the `/e` argument), a description, version, and default output file extension.

### Help

    bfc /? [engine]

This command shows either the generic help, or the engine specific help if an engine name is supplied.

## Parser

The parser in BFC operates as follows:

1. Read file with BF instructions
2. Discard all non-bf characters
3. (optional) Optimize
4. Tokenize
5. Pass to engine
6. Write output

## Optimizer

This compiler contains an optimizer that is activated using the `/o` switch.
This switch will introduce new BF instructions that your custom code generator has to deal with.
Currently there's only one optimization that's performed:

| Original | Replacement | Optimized | Description |
|--|--|--|--|
| `[-]` | `!` | `mem[ptr]=0;` | Set current memory cell to zero |

If your engine can't handle optimized instructions, it should either replace them with the original BF instructions,
or throw an exception, informing the user that the `/o` switch is not supported by your engine.
This should then also be part of the engine help.

## Tokenizer

The tokenizer will bundle identical BF instructions together.
All built-in engines support this and will appropriately emit instructions.
For example the C99 engine will turn `+++++` into `mem[ptr]+=5;` instead of 5 times `++mem[ptr];`

If you struggle with this, or you compile to a target that doesn't supports this,
you can use the `Token.GetRepeated()` method to get individual tokens from a combined one.
This means `Tokens=Tokens.SelectMany(m => m.GetRepeated())` will essentially undo most of the work the tokenizer did
and will leave you with exactly as many tokens as there are individual BF instructions.
This will not undo work done by the optimizer.

## Built-in Engines

This project comes with three built-in engines of various interest.

### C#

Creates C# code for a console application.
The code does not depend on any particular framework version and should compile with every framework.
The managed nature of the language makes this safe against out of bounds reads/writes,
but it's not quite as fast as C code.

The code is properly indented.

#### Demonstrates

- Generating code for a simple to use language with strong control flow structures
- Indent code
- Different instructions for token runs of 1 vs runs of 2 or more
- Detect unbalanced brackets
- Custom arguments

### C99

This engine creates standard C99 code.
It should compile with all warnings turned on and the `-pedantic` setting too.
Provided your BF code does not depends on memory cell overflow, it should be portable.

This generator will not protect against out of bounds reads/writes.

The code is properly indented.

#### Demonstrates

- Generating code for a ubiquitous language with strong control flow structures
- Indent code
- Different instructions for token runs of 1 vs runs of 2 or more
- Detect unbalanced brackets
- Custom arguments

### DOS

This engine creates Intel 386 compatible assembly for a single page DOS executable (.com file).
The assembly syntax is [Flat Assembler (FASM)](https://flatassembler.net/) compatible.
FASM can assemble the output of this compiler into an executable for DOS.
You can assemble it directly in your main OS, or you can assemble it inside of DOS itself.
A FASM port for DOS is available, but be aware that it needs a [DPMI host](https://en.wikipedia.org/wiki/DOS_Protected_Mode_Interface).

The assembled executable will not run on 64 bit Windows.
To run your code reliably, you can use an easy to use emulator like [DosBOX](https://www.dosbox.com/).
DosBOX emulates an old computer.
If you want to run a long-running BF application,
you can hold down `ALT`+`F12` to run it at unlocked speed.

A single page application has a few weird properties:

- Runs in [real mode](https://en.wikipedia.org/wiki/Real_mode).
- Code, working memory, and stack share the same memory space.
- Nothing is protected against accidental or malicious writes.
- Limited to 64k of memory.

These properties cause a few potentially problematic things to happen:

- As the code size grows, free memory shrinks.
- Pushing too much onto the stack will overwrite other objects (and eventually code) in memory.
- A (malicious) BF program can run any machine instruction it wants by overwriting itself or jumping into memory.

BF does not use a stack, so this problem is non existent for a BF application.
The stack is still initialized by DOS to point to the very end of the memory page.
The executable in memory has this layout (numbers in hexadecimal):

| Type   | From | Length | To   | Description              |
|--------|------|--------|------|--------------------------|
| PSP    | 0000 | 0100   | 00FF | DOS supplied information |
| Code   | 0100 | *      | *    | BF code                  |
| Memory | *    | *      | FFFF | Usable memory            |
| Stack  | FFFF | *      | 0    | Stack (not used)         |

Note that the BF code will be prepended and appended with a few other assembly instructions.
Before BF starts, a few assembly instructions will zero the available memory.
After BF ends, a DOS exit call is run to properly return control back to DOS.
After the exit call are two subroutines for reading and writing characters.

If you want to screw around with the PSP, [you can find the layout here](https://en.wikipedia.org/wiki/Program_Segment_Prefix).
Remember that you have to leave the BF program memory for this to work.
Since the memory is initialized to zero, you can essentially just go to the right until it's not zero anymore.
The PSP starts with the bytes `0xCD 0x20` but you can just search for the first non-zero cell.

    Moves the memory cursor to the right until `0xCD` is found
    +++++++++++++++++++++++++++++++++++++++++++++++++++
    [
    ---------------------------------------------------
    >
    +++++++++++++++++++++++++++++++++++++++++++++++++++
    ]
    Memory pointer now at start of PSP
    BF memory still zeroed

The example code above is not optimized for size in any way.

How it works:

    +-----+
    |Start|
    +-----+
       |
       |
       v
    +----------------------+
    |Add 51 to current cell+<---------+
    +--+-------------------+          |
       |                              |
       |                              |
       v                              |
    +--+---------+    +----------+    |
    |Is cell zero+-Y->+At PSP now|    |
    +--+---------+    +----------+    |
       |                              |
       N                              |
       v                              |
    +--+--------------------------+   |
    |Subtract 51 from current cell|   |
    +--+--------------------------+   |
       |                              |
       |                              |
       v                              |
    +--+------------+                 |
    |Go to next cell+-----------------+
    +---------------+


It's easier if you write a code generator that copies the required values into BF memory.
The BF pointer doesn't has to start at zero in the memory.
Offsetting the pointer and filling the memory before the new start location will not break existing BF programs,
but programs that are aware of the content before cell zero can access it.

Note that DOS will not properly check for CTRL+C.
To do so, subtract 3 from the input and check if it's zero.

#### Demonstrates

- Generating very low level code with weak control flow structures (only jumps and labels)
- Optimize unnecessary jumps away
- Different instructions for token runs of 1 vs runs of 2 or more
- Detect unbalanced brackets
- Engine without arguments

## External Engines

This repository comes with a secondary project that creates "fake" assembly code.
This is used to demonstrate how to create external engines and can be used as base code for such.

## Extending

This application supports loading custom engines.
One such engine is provided as an example.
It outputs code that looks like assembly but is not actually.
You can use this as a base for your own engines.

To write your own engine, all you have to do is provide an implementation of the `ICodeGenerator` interface.

Your implementation must provide an empty constructor,
so it can be instantiated to call the methods to obtain name, description, etc.

BFC comes with a `bfc.dll` and a `bfc.exe` file.
You can reference the DLL to get access to the required interface.
As an alternative, you can download this repository and then add a project reference instead.

A single DLL file can contain multiple engines.

### Limitations

- The engine will not be available if no parameterless constructor is found
- If a duplicate engine name is encountered, it will not be added to the list
- Name, Version, and default file extension are mandatory

### Custom arguments

If your engine requires custom arguments, you can tell so in the help.
Note that arguments are passed into engine in the order they're supplied on the command line. If your engine expects a certain order of arguments, you should properly number them in the help.

You should throw an Exception if the argument count doesn't matches (too few or too many)

All arguments will be passed as a string array in a single call to `SetArguments()` before requesting to generate code. The function is only called if custom arguments were supplied by the user. It will not be called with an empty array or `null`. Do not confuse an empty array with an array of empty strings. By calling `bfc /a ""` the user can supply empty strings.

### `IEnumerable<char> GenerateCode(IEnumerable<Token> Instructions)` method

This method is used to convert the supplied tokens into code that BFC will write to the user supplied (or automatically determined) output file.
How you generate the code is up to you, including but not limited to loading other engines (see `bfc.Common` class), making network requests, actually running the code, etc.

If you return `null`, bfc will report this as an error to the user.

## About IEnumerable\<char\>

BF code can technically be of any length, and it has to if you want it to be turing complete. Using IEnumerable instead of strings allows us to compile BF code that doesn't actually fits into memory.
Note that code is still occasionally converted to strings to make the program more understandable but it strictly speaking is not necessary. Regular C# strings are considered an implementation of `IEnumerable<char>` which means you don't have to call `.ToCharArray()` when returning a string from your `GenerateCode` method.

**CAUTION!** A `char` in C# is UTF-16 and not a single byte. Do not blindly convert between characters and bytes unless you know that the character will fit a byte.

## Exercises for the Reader

Optimize `[+]` into `!` too.

Discard starting loops in BF.
If a BF program starts with a loop it will never run the code inside because memory is inizialized to zero.
These loops are sometimes used to add a header comment,
because they essentially make anything inside of them skipped over.

Change the C99 generator to use actual single character input (`getchar()` only fires once an entire line has been entered)
but make sure it still stays portable, at least between Linux and Windows.

Generator for JS.

Generator that outputs indented BF (essentially a code formatter).

Actual BF interpreter. Bonus points if it has a live code and memory viewer.

Generator for another esotheric language that is not a trivial substitution of BF.
