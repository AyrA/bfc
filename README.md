
# BFC

This is a [Brainfuck](https://en.wikipedia.org/wiki/Brainfuck) compiler that can target multiple languages and is easily extendable.

*The Word Brainfuck will be abbreviated as BF further down*

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
| `,` | `mem[ptr]=(int)Console.ReadKey().KeyChar;` |

Before the BF code runs, `var mem=new byte[30000];int ptr=0;` is executed.
After the BF code, `return (int)mem[ptr];` is executed.

Any unsupported character encountered in a BF file is discarded.

### Defaults

The original BF compiler was written in C. It used a `char` array of 30'000 elements as memory. All built-in engines will default to that but some allow customization via arguments. Your custom engines should follow this principle too.

The BF compiler made no special distinction between signed and unsigned.
Signed overflow is undefined behavior in C, so you should default to an unsigned 8-bit data type unless you need something else.
Do not use data types that are too large.
The only way to make a number larger in BF is to add 1 to it. A signed 32 bit memory cell takes over 2 billion increments to fill up.

## Usage

This is a command line utility that has 3 different ways of using it:

### Compiling BF code

    bfc [/y] [/o] /e engine [/a arg [...]] <infile> [outfile]

- **/y**: Suppresses prompts to overwrite the destination
- **/o**: Optimizes the code (see "Optimizer"  below)
- **/e engine**: Selects the engine used to compile the code (see "List Engines" below)
- **/a arg**: Supplies an argument to the engine.
- **infile**: File containing BF code
- **outfile**: File where the output is written to. If missing, replaces the file extension of `infile` with the one supplied by the engine.

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
This switch will introduce new BF instructions that your custom code generator has to deal with. Currently there's only one optimization that's performed:

| Original | Replacement | Optimized | Description |
|--|--|--|--|
| `[-]` | `!` | `mem[ptr]=0;` | Set current memory cell to zero |

## Tokenizer

The tokenizer will bundle identical BF instructions together.
All built-in engines support this and will appropriately emit instructions.
For example the C99 engine will turn `+++++` into `mem[ptr]+=5;` instead of 5 times `++mem[ptr];`

If you struggle with this, or you compile to a target that doesn't supports this,
you can use the `Token.GetRepeated()` method to get individual tokens from a combined one. This means `Tokens=Tokens.SelectMany(m => m.GetRepeated())` will essentially undo most of the work the tokenizer did and will leave you with exactly as many tokens as there are individual BF instructions.

## Extending

This application supports loading custom engines.
One such engine is provided as an example. It outputs code that looks like assembly but is not actually. You can use this as a base for your own engines.

To write your own engine, all you have to do is provide an implementation of the `ICodeGenerator` interface.

Your implementation must provide an empty constructor, so it can be instantiated to call the methods to obtain name, description, etc.

BFC comes with a `bfc.dll` and a `bfc.exe` file. You can reference the DLL to get access to the required interface. As an alternative, you can download this repository and then add a project reference instead.

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
