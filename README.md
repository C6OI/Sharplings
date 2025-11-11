# Sharplings #️⃣
[![NuGet Version](https://img.shields.io/nuget/vpre/Sharplings)](https://www.nuget.org/packages/Sharplings)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Sharplings)](https://www.nuget.org/packages/Sharplings)

Small exercises to get you used to reading and writing [C#](https://dotnet.microsoft.com/en-us/languages/csharp) code. Inspired by [Rustlings](https://github.com/rust-lang/rustlings)

> [!WARNING]
> Sharplings currently has a limited number of exercises. More exercises will be added over time. Contributions are welcome!

# Quick Start
```bash
# Installation
dotnet tool install -g --prerelease Sharplings
# Initialization
Sharplings init
# Moving into new directory
cd Sharplings
# Starting Sharplings
Sharplings
```

# Setup

## Installing .NET
Before installing Sharplings, you must have the **latest version of .NET** installed. Visit https://dotnet.microsoft.com/download for further instructions.

## Installing Sharplings
The following command will download and compile Sharplings:
```bash
dotnet tool install -g --prerelease Sharplings
```

> [!NOTE]
> We recommend you to use the latest prerelease version of Sharplings, as it will contain the latest exercises, bugfixes and improvements.

<details>
<summary>If the installation fails...</summary>

* Make sure you have the latest .NET version
* [Report the issue](https://github.com/C6OI/Sharplings/issues)
</details>

## Initialization
After installing Sharplings, run the following command to initialize the `Sharplings/` directory:
```bash
Sharplings init
```

> [!TIP]
> You can specify the directory name using the `-o` option:
> ```bash
> Sharplings init -o MyDirectoryName
> ```
> The command above will initialize the Sharplings in the `MyDirectoryName/` directory.

<details>
<summary>If the command `Sharplings` can't be found...</summary>

On Linux after installing a command-line tool with `dotnet tool`, the tool can be executed only from the `~/.dotnet/tools` path.  
To make the tool executable from any directory, update the `PATH` environment variable.  
To make the updated `PATH` environment variable permanent in your shell, update your shell settings. For Bash, this is the `~/.bashrc` file.
</details>

Now, go into the newly initialized directory and launch Sharplings for further instructions on getting started with the exercises:
```bash
cd Sharplings/
Sharplings
```

## Working environment

### Editor
Our general recommendation is [JetBrains Rider](https://jetbrains.com/rider) or [Visual Studio](https://visualstudio.microsoft.com). But any editor that supports C# should be enough for working on the exercises.

### Terminal
While working with Sharplings, please use a modern terminal for the best user experience. The default terminal on Linux and Mac should be sufficient. On Windows, we recommend the [Windows Terminal](https://aka.ms/terminal) or the [PowerShell](https://aka.ms/powershell).

# Usage

## Doing exercises
The exercises are sorted by topic and can be found in the subdirectory `Exercises/<topic>`. For every topic, there is an additional `README.md` file with some information to get you started on the topic. We highly recommend that you have a look at them before you start.

Most exercises contain an error that keeps them from compiling, and it's up to you to fix it! Some exercises contain tests that need to pass for the exercise to be done.

Search for `TODO` and `NotImplementedException` to find out what you need to change. Ask for hints by entering `h` in the *watch mode*.

## Watch Mode
After the [initialization](#initialization), Sharplings can be launched by simply running the command `Sharplings`.

This will start the *watch mode* which walks you through the exercises in a predefined order (what we think is best for newcomers). It will rerun the current exercise automatically every time you change the exercise's file in the `Exercises/` directory.

<details>
<summary>If detecting file changes in the `Exercises/` directory fails…</summary>

You can add the `--manual-run` flag (`Sharplings --manual-run`) to manually rerun the current exercise by entering `r` in the watch mode.

Please [report the issue](https://github.com/C6OI/Sharplings/issues) with some information about your operating system and whether you run Sharplings in a container or a virtual machine (e.g. WSL).
</details>

## Exercise List
In the [watch mode](#watch-mode) (after launching `Sharplings`), you can enter `l` to open the interactive exercise list.

The list allows you to…

* See the status of all exercises (done or pending)
* `c`: Continue at another exercise (temporarily skip some exercises or go back to a previous one)
* `r`: Reset status and file of the selected exercise (you need to *reload/reopen* its file in your editor afterward)
See the footer of the list for all possible keys.

<!-- TODO: Questions section -->

## Continuing On
Once you've completed Sharplings, put your new knowledge to good use! Continue practicing your C# skills by building your own projects, contributing to Sharplings, or finding other open-source projects to contribute to.

> If you want to create your own Sharplings exercises, see the [community exercises](#community-exercises) section.

# Community Exercises

## List of Community Exercises
Unfortunately, there are no community exercises at the moment. But your exercises may be the first on this list! Keep reading!

## Creating Community Exercises
Sharplings' support for community exercises allows you to create your own exercises to focus on some specific topic. You could also offer a translation of the original Sharplings exercises as community exercises.

### Getting Started
To create community exercises, install Sharplings and run `Sharplings dev new PROJECT_NAME`. This command will, similar to `dotnet new console -n PROJECT_NAME`, create the template directory `PROJECT_NAME` with all what you need to get started.

*Read the comments* in the generated `info.toml` file to understand its format. It allows you to set a custom welcome and final message and specify the metadata of every exercise.

### Creating an Exercise
Here is an example of the metadata of one exercise:
```toml
[[exercises]]
name = "Intro1"
hint = """
To finish this exercise, you need to …
These links might help you …"""
```

After entering this in `info.toml`, create the file `Intro1.cs` in the `Exercises/` directory. The exercise needs to contain an entry point (`public static void Main()`) method, but it can be empty. Adding tests is recommended. Look at the official Sharplings exercises for inspiration.

You can optionally add a solution file `Intro1.cs` to the `Solutions/` directory.

Now, run `Sharplings dev check`. It will tell you about any issues with your exercises.

`Sharplings dev check` will also run your solutions (if you have any) to make sure that they run successfully.

That's it! You finished your first exercise!

### .editorconfig
You can modify the `.editorconfig` file as you want.

Learn more at [jetbrains.com](https://www.jetbrains.com/help/rider/Using_EditorConfig.html) or [learn.microsoft.com](https://learn.microsoft.com/visualstudio/ide/create-portable-custom-editor-options)

### Publishing
Now, add more exercises and publish them as a Git repository.

Users just have to clone that repository and run `Sharplings` in it to start working on your exercises (just like the official ones).

One difference to the official exercises is that the solution files will not be hidden until the user finishes an exercise. But you can trust your users to not open the solution too early =)

### Sharing
After publishing your community exercises, open an issue or a pull request in the [Sharplings repository](https://github.com/C6OI/Sharplings), and maybe we will add your project to the [list of community exercises](#list-of-community-exercises)!
