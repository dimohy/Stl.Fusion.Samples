using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.CommandR.Configuration;
using Stl.Fusion;

namespace Samples.HelloBlazorHybrid.Abstractions
{
    public class ChatService : IChatService
    {
        private volatile ImmutableList<IChatService.Message> _messages = ImmutableList<IChatService.Message>.Empty;

        private readonly object _lock = new();

        [CommandHandler]
        public virtual Task PostMessageAsync(IChatService.PostCommand command, CancellationToken cancellationToken = default)
        {
            if (Computed.IsInvalidating()) {
                GetMessageCountAsync().Ignore();
                GetAnyTailAsync().Ignore();
                return Task.CompletedTask;
            }

            var (name, message) = command;
            lock (_lock) {
                _messages = _messages.Add(new(DateTime.Now, name, message));
            }
            return Task.CompletedTask;
        }

        [ComputeMethod]
        public virtual Task<int> GetMessageCountAsync()
            => Task.FromResult(_messages.Count);

        [ComputeMethod]
        public virtual async Task<IChatService.Message[]> GetMessagesAsync(
            int count, CancellationToken cancellationToken = default)
        {
            // Fake dependency used to invalidate all GetMessagesAsync(...) independently on count argument
            await GetAnyTailAsync();
            return _messages.TakeLast(count).ToArray();
        }

        [ComputeMethod]
        public virtual Task<Unit> GetAnyTailAsync() => TaskEx.UnitTask;
    }
}
