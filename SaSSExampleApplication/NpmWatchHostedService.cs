using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SaSSExampleApplication
{
#if DEBUG
    internal sealed class NpmWatchHostedService : IHostedService, IDisposable
    {
        private readonly bool _enabled;
        private readonly ILogger<NpmWatchHostedService> _logger;

        private Process _process;

        public NpmWatchHostedService(bool enabled, ILogger<NpmWatchHostedService> logger)
        {
            _enabled = enabled;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_enabled)
            {
                StartProcess();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_process != null)
            {
                _process.Close();
                _process.Dispose();
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _process?.Dispose();
            _process = null;
        }

        private void StartProcess()
        {
            _process = new Process();
            _process.StartInfo.FileName = Path.Join(Directory.GetCurrentDirectory(), "node_modules/.bin/sass" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ""));
            _process.StartInfo.Arguments = "--watch Styles:wwwroot/css";
            _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            _process.EnableRaisingEvents = true;

            _process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    _logger.LogInformation(args.Data);
            };
            _process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    _logger.LogError(args.Data);
            };

            _process.Exited += HandleProcessExit;

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _logger.LogInformation("Started NPM watch");
        }

        private async void HandleProcessExit(object sender, object args)
        {
            _process.Dispose();
            _process = null;

            _logger.LogWarning("npm watch exited, restarting in 1 second.");

            await Task.Delay(1000);
            StartProcess();
        }
    }
#endif
}
