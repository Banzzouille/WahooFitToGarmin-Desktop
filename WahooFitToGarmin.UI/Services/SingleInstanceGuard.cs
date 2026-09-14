using System.IO.Pipes;
using System.Text;

namespace WahooFitToGarmin.UI.Services;

/// <summary>
/// Ensures one running instance, and lets a second launch activate the first.
/// </summary>
/// <remarks>
/// This becomes necessary the moment closing the window stops terminating the
/// process. Without it, launching again — from the dock, the start menu, or an
/// autostart entry — leaves two processes watching the same folder, which means
/// the same activity uploaded twice. The service's duplicate detection would be
/// the only thing preventing visible damage.
///
/// A named pipe is used rather than a lock file: a lock file left behind by a
/// crash blocks every later launch, whereas a pipe disappears with its process.
/// </remarks>
public sealed class SingleInstanceGuard : IDisposable
{
    private const string PipeName = "wahoo-fit-to-garmin.instance";
    private const string ActivateMessage = "activate";

    private readonly CancellationTokenSource _listening = new();
    private Task? _listener;

    /// <summary>Raised when another launch asked for the window.</summary>
    public event Action? ActivationRequested;

    /// <summary>
    /// True when this process is the only instance. False means another one is
    /// already running and has been asked to show itself.
    /// </summary>
    public bool TryAcquire()
    {
        if (SignalExistingInstance())
        {
            return false;
        }

        _listener = Task.Run(ListenAsync);
        return true;
    }

    private static bool SignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);

            // A short timeout: a pipe that does not answer promptly is not a
            // running instance worth deferring to.
            client.Connect(300);

            var payload = Encoding.UTF8.GetBytes(ActivateMessage);
            client.Write(payload, 0, payload.Length);
            client.Flush();

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private async Task ListenAsync()
    {
        while (!_listening.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(_listening.Token).ConfigureAwait(false);

                var buffer = new byte[64];
                var read = await server.ReadAsync(buffer, _listening.Token).ConfigureAwait(false);

                if (Encoding.UTF8.GetString(buffer, 0, read) == ActivateMessage)
                {
                    ActivationRequested?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException)
            {
                // A client that disconnected mid-handshake. Listen again.
            }
        }
    }

    public void Dispose()
    {
        _listening.Cancel();
        _listening.Dispose();
    }
}
