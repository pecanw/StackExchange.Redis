﻿using System;
using System.Net;
using System.Threading.Tasks;

namespace StackExchange.Redis.KeyspaceIsolation
{
    internal class SubscriberWrapper : ISubscriber
    {
        public ISubscriber Inner { get; }

        internal RedisChannel Prefix { get; }

        public SubscriberWrapper(ISubscriber inner, byte[] prefix)
        {
            Inner = inner;
            Prefix = new RedisChannel(prefix, RedisChannel.PatternMode.Literal);
        }

        public ConnectionMultiplexer Multiplexer => Inner.Multiplexer;

        public bool TryWait(Task task) => Inner.TryWait(task);

        public void Wait(Task task) => Inner.Wait(task);

        public T Wait<T>(Task<T> task) => Inner.Wait(task);

        public void WaitAll(params Task[] tasks) => Inner.WaitAll(tasks);

        public string ClientGetName(CommandFlags flags = CommandFlags.None) => Inner.ClientGetName(flags);

        public Task<string> ClientGetNameAsync(CommandFlags flags = CommandFlags.None) => Inner.ClientGetNameAsync(flags);

        public void Quit(CommandFlags flags = CommandFlags.None) => Inner.Quit(flags);

        public TimeSpan Ping(CommandFlags flags = CommandFlags.None) => Inner.Ping(flags);

        public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None) => Inner.PingAsync(flags);

        public EndPoint IdentifyEndpoint(RedisChannel channel, CommandFlags flags = CommandFlags.None) => Inner.IdentifyEndpoint(ToInner(channel), flags);

        public Task<EndPoint> IdentifyEndpointAsync(RedisChannel channel, CommandFlags flags = CommandFlags.None) => Inner.IdentifyEndpointAsync(ToInner(channel), flags);

        public bool IsConnected(RedisChannel channel = default(RedisChannel)) => Inner.IsConnected(ToInner(channel));

        public long Publish(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None) => Inner.Publish(ToInner(channel), message, flags);

        public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None) => Inner.PublishAsync(ToInner(channel), message, flags);

        public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None) =>
            Inner.Subscribe(ToInner(channel), ToInner(handler), flags);

        public Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None) =>
            Inner.SubscribeAsync(ToInner(channel), ToInner(handler), flags);

        public EndPoint SubscribedEndpoint(RedisChannel channel) => Inner.SubscribedEndpoint(ToInner(channel));

        public void Unsubscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler = null, CommandFlags flags = CommandFlags.None) =>
            Inner.Unsubscribe(ToInner(channel), ToInner(handler), flags);

        public void UnsubscribeAll(CommandFlags flags = CommandFlags.None)
        {
            if (Prefix.IsNullOrEmpty)
                Inner.UnsubscribeAll(flags);
            else
                Inner.Unsubscribe(new RedisChannel(Prefix + "*", RedisChannel.PatternMode.Pattern), null, flags);
        }

        public Task UnsubscribeAllAsync(CommandFlags flags = CommandFlags.None)
        {
            if (Prefix.IsNullOrEmpty)
                return Inner.UnsubscribeAllAsync(flags);
            else
                return Inner.UnsubscribeAsync(new RedisChannel(Prefix + "*", RedisChannel.PatternMode.Pattern), null, flags);
        }

        public Task UnsubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler = null, CommandFlags flags = CommandFlags.None) =>
            Inner.UnsubscribeAsync(ToInner(channel), ToInner(handler), flags);

        public RedisChannel ToInner(RedisChannel outer)
        {
            if (Prefix.IsNullOrEmpty) return outer;

            if (outer.IsNullOrEmpty) return Prefix;

            byte[] outerArr = outer;
            byte[] prefixArr = Prefix;

            var innerArr = new byte[prefixArr.Length + outerArr.Length];
            Buffer.BlockCopy(prefixArr, 0, innerArr, 0, prefixArr.Length);
            Buffer.BlockCopy(outerArr, 0, innerArr, prefixArr.Length, outerArr.Length);

            var patternMode = outer.IsPatternBased ? RedisChannel.PatternMode.Pattern : RedisChannel.PatternMode.Literal;

            return new RedisChannel(innerArr, patternMode);
        }

        protected Action<RedisChannel, RedisValue> ToInner(Action<RedisChannel, RedisValue> handler) => (channel, value) => handler(ToOuter(channel), value);

        public RedisChannel ToOuter(RedisChannel inner)
        {
            if (Prefix.IsNullOrEmpty || inner.IsNullOrEmpty) return inner;

            byte[] innerArr = inner;
            byte[] prefixArr = Prefix;

            if (innerArr.Length <= prefixArr.Length) return inner;

            for (var i = 0; i < prefixArr.Length; i++)
            {
                if (prefixArr[i] != innerArr[i]) return inner;
            }

            var outerLength = innerArr.Length - prefixArr.Length;
            var outerArr = new byte[outerLength];
            Buffer.BlockCopy(innerArr, prefixArr.Length, outerArr, 0, outerLength);

            var patternMode = inner.IsPatternBased ? RedisChannel.PatternMode.Pattern : RedisChannel.PatternMode.Literal;

            return new RedisChannel(outerArr, patternMode);
        }
    }
}