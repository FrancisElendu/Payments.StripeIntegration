using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace Payments.StripeIntegration.Infrastructure.RabbitMQ
{
    public class RabbitMqChannelPool
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
        //private readonly CreateChannelOptions _createChannelOptions = new (true, true );

        public RabbitMqChannelPool(IConnection connection) //, CreateChannelOptions createChannelOptions
        {
            _connection = connection;
            //_createChannelOptions = createChannelOptions;
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

            return await _connection.CreateChannelAsync(options,ct);
        }

        public void ReturnChannel(IChannel channel)
        {
            if (channel != null && channel.IsOpen)
            {
                _channels.Add(channel);
            }
        }
    }
}
