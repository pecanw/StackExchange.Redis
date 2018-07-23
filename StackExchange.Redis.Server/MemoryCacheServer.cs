﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Caching;

namespace StackExchange.Redis.Server
{
    public class MemoryCacheServer : RedisServer
    {
        public MemoryCacheServer(TextWriter output = null) : base(1, output)
            => CreateNewCache();

        private MemoryCache _cache;

        private void CreateNewCache()
        {
            var old = _cache;
            _cache = new MemoryCache(GetType().Name);
            if (old != null) old.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _cache.Dispose();
            base.Dispose(disposing);
        }

        protected override long Dbsize(int database) => _cache.GetCount();
        protected override RedisValue Get(int database, RedisKey key)
            => RedisValue.Unbox(_cache[key]);
        protected override void Set(int database, RedisKey key, RedisValue value)
            => _cache[key] = value.Box();
        protected override bool Del(int database, RedisKey key)
            => _cache.Remove(key) != null;
        protected override void Flushdb(int database)
            => CreateNewCache();

        protected override bool Exists(int database, RedisKey key)
            => _cache.Contains(key);

        protected override IEnumerable<RedisKey> Keys(int database, RedisKey pattern)
        {
            string s = pattern;
            foreach (var pair in _cache)
            {
                if (IsMatch(pattern, pair.Key)) yield return pair.Key;
            }
        }
        protected override bool Sadd(int database, RedisKey key, RedisValue value)
            => GetSet(key, true).Add(value);

        protected override bool Sismember(int database, RedisKey key, RedisValue value)
            => GetSet(key, false)?.Contains(value) ?? false;

        protected override bool Srem(int database, RedisKey key, RedisValue value)
        {
            var set = GetSet(key, false);
            if (set != null && set.Remove(value))
            {
                if (set.Count == 0) _cache.Remove(key);
                return true;
            }
            return false;
        }
        protected override long Scard(int database, RedisKey key)
            => GetSet(key, false)?.Count ?? 0;

        HashSet<RedisValue> GetSet(RedisKey key, bool create)
        {
            var set = (HashSet<RedisValue>)_cache[key];
            if (set == null && create)
            {
                set = new HashSet<RedisValue>();
                _cache[key] = set;
            }
            return set;
        }

    }
}