using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;

namespace Sharplings;

[PublicAPI]
public class ProcessBuilder {
    ProcessStartInfo StartInfo { get; } = new();
    TextWriter? StdOutWriter { get; set; }
    TextWriter? StdErrWriter { get; set; }

    public ProcessBuilder WithFileName(string? fileName) {
        StartInfo.FileName = fileName;
        return this;
    }

    public ProcessBuilder AddArgument(string argument) {
        StartInfo.ArgumentList.Add(argument);
        return this;
    }

    public ProcessBuilder AddArguments(params IEnumerable<string> arguments) {
        foreach (string argument in arguments) AddArgument(argument);
        return this;
    }

    public ProcessBuilder WithStdOut(TextWriter? stdOutWriter) {
        StdOutWriter = stdOutWriter;
        return this;
    }

    public ProcessBuilder WithStdErr(TextWriter? stdErrWriter) {
        StdErrWriter = stdErrWriter;
        return this;
    }

    public ProcessBuilder RedirectStdOut(bool value = true) {
        StartInfo.RedirectStandardOutput = value;
        StartInfo.StandardOutputEncoding = Encoding.Default;
        return this;
    }

    public ProcessBuilder RedirectStdErr(bool value = true) {
        StartInfo.RedirectStandardError = value;
        StartInfo.StandardErrorEncoding = Encoding.Default;
        return this;
    }

    public ProcessBuilder CreateNoWindow(bool value = true) {
        StartInfo.CreateNoWindow = value;
        return this;
    }

    [MustDisposeResource]
    public Process StartProcess() {
        Process process = new() {
            StartInfo = StartInfo,
        };

        if (StdOutWriter != null) {
            RedirectStdOut();
            process.OutputDataReceived += (_, e) => StdOutWriter.WriteLine(e.Data);
        }

        if (StdErrWriter != null) {
            RedirectStdErr();
            process.ErrorDataReceived += (_, e) => StdErrWriter.WriteLine(e.Data);
        }

        process.Start();

        if (StdOutWriter != null) process.BeginOutputReadLine();
        if (StdErrWriter != null) process.BeginErrorReadLine();

        return process;
    }
}
