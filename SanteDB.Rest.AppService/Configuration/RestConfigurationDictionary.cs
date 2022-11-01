﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// An implementation of an <see cref="IDictionary{TKey, TValue}"/> where getting by index returns null
    /// </summary>
    public class RestConfigurationDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {

        // Wrapped dictionary
        private readonly IDictionary<TKey, TValue> m_wrapped = new Dictionary<TKey, TValue>();

        /// <inheritdoc/>
        public TValue this[TKey key] {
            get
            {
                if(this.m_wrapped.TryGetValue(key, out var value))
                {
                    return value;
                }
                else
                {
                    return default(TValue);
                }
            }
            set
            {
                if(this.m_wrapped.ContainsKey(key))
                {
                    this.m_wrapped[key] = value;
                }
                else
                {
                    this.m_wrapped.Add(key, value);
                }
            }
        }


        /// <inheritdoc/>
        public ICollection<TKey> Keys => this.m_wrapped.Keys;

        /// <inheritdoc/>
        public ICollection<TValue> Values => this.m_wrapped.Values;

        /// <inheritdoc/>
        public int Count => this.m_wrapped.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => this.m_wrapped.IsReadOnly;

        /// <inheritdoc/>
        public void Add(TKey key, TValue value) => this.m_wrapped.Add(key, value);

        /// <inheritdoc/>
        public void Add(KeyValuePair<TKey, TValue> item) => this.m_wrapped.Add(item);

        /// <inheritdoc/>
        public void Clear() => this.m_wrapped.Clear();

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<TKey, TValue> item) => this.m_wrapped.Contains(item);

        /// <inheritdoc/>
        public bool ContainsKey(TKey key) => this.m_wrapped.ContainsKey(key);

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => this.m_wrapped.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => this.m_wrapped.GetEnumerator();

        /// <inheritdoc/>
        public bool Remove(TKey key) => this.m_wrapped.Remove(key);

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<TKey, TValue> item) => this.m_wrapped.Remove(item);

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value) => this.m_wrapped.TryGetValue(key, out value);

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.m_wrapped.GetEnumerator();
    }
}
