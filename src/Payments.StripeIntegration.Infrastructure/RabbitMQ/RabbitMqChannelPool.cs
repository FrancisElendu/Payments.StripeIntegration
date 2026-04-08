using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace Payments.StripeIntegration.Infrastructure.RabbitMQ
{
    public class RabbitMqChannelPool : IDisposable
    {
        //private readonly IConnection _connection;
        //private readonly ConcurrentBag<IModel> _channels = new();

        //public RabbitMqChannelPool(IConnection connection)
        //{
        //    _connection = connection;
        //}

        //public IModel GetChannel()
        //{
        //    if (_channels.TryTake(out var channel))
        //    {
        //        if (channel.IsOpen)
        //            return channel;
        //    }

        //    return _connection.CreateModel();
        //}

        //public void ReturnChannel(IModel channel)
        //{
        //    if (channel != null && channel.IsOpen)
        //    {
        //        _channels.Add(channel);
        //    }
        //}


        private readonly IConnection _connection;
        private readonly ConcurrentBag<IChannel> _channels = new();
        private readonly CreateChannelOptions _defaultOptions;
        private bool _disposed;

        public RabbitMqChannelPool(IConnection connection) 
        {      
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            // Configure default channel options with publisher confirms enabled
            _defaultOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            );
        }

        public async Task<IChannel> GetChannelAsync(CancellationToken ct)
        {
            var options = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,        // Enable publisher confirms
                    publisherConfirmationTrackingEnabled: true  // Track confirmations for mandatory messages
                    );

            if (_channels.TryTake(out var channel))
            {
                if (channel.IsOpen)
                    return channel;
            }

            return await _connection.CreateChannelAsync(options, ct);
        }

        public void ReturnChannel(IChannel channel)
        {
            if (_disposed) return;
            if (channel == null) return;

            if (channel != null && channel.IsOpen && !channel.IsClosed)
            {
                _channels.Add(channel);
            }
            else
            {
                // Dispose unusable channels
                Task.Run(() => DisposeChannelAsync(channel));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var channel in _channels)
            {
                try
                {
                    if (channel.IsOpen)
                    {
                        channel.CloseAsync().GetAwaiter().GetResult();
                    }
                    channel.DisposeAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _channels.Clear();
        }

        private async Task DisposeChannelAsync(IChannel channel)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync();
                }
                await channel.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Log but don't throw - this is cleanup
                Console.WriteLine($"Error disposing channel: {ex.Message}");
            }
        }
    }
}
