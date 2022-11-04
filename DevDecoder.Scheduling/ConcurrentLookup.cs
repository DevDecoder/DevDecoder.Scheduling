// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace DevDecoder.Scheduling;

/// <summary>
///     Implements a concurrent lookup, which allows a set of objects to be grouped and manipulated by a key.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
public class ConcurrentLookup<TKey, TValue> : ILookup<TKey, TValue> where TKey : notnull
{
    /// <summary>
    ///     A concurrent dictionary that holds the underlying data.
    /// </summary>
    private readonly ConcurrentDictionary<TKey, Grouping> _dictionary;

    /// <summary>
    ///     The value comparer, used to check for equality.
    /// </summary>
    private readonly IEqualityComparer<TValue> _valueComparer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConcurrentLookup&lt;TKey, TValue&gt;" /> class.
    /// </summary>
    /// <param name="concurrencyLevel">
    ///     <para>The concurrency level.</para>
    ///     <para>This is the estimated number of threads that will update the lookup concurrently.</para>
    ///     <para>By default this is 4 * the processor count.</para>
    /// </param>
    /// <param name="capacity">
    ///     <para>The initial number of elements that the lookup can contain.</para>
    ///     <para>By default this is 32.</para>
    /// </param>
    /// <param name="comparer">The comparer to use when comparing keys.</param>
    /// <param name="valueComparer">The value comparer, used to check for equality.</param>
    public ConcurrentLookup(
        int concurrencyLevel = 0,
        int capacity = 0,
        IEqualityComparer<TKey>? comparer = null,
        IEqualityComparer<TValue>? valueComparer = null)
        : this(null, concurrencyLevel, capacity, comparer, valueComparer)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConcurrentLookup&lt;TKey, TValue&gt;" /> class.
    /// </summary>
    /// <param name="collection">The collection to copy into the lookup.</param>
    /// <param name="concurrencyLevel">
    ///     <para>The concurrency level.</para>
    ///     <para>This is the estimated number of threads that will update the lookup concurrently.</para>
    ///     <para>By default this is 4 * the processor count.</para>
    /// </param>
    /// <param name="capacity">
    ///     <para>The initial number of elements that the lookup can contain.</para>
    ///     <para>By default this is 32.</para>
    /// </param>
    /// <param name="comparer">The comparer to use when comparing keys.</param>
    /// <param name="valueComparer">The value comparer, used to check for equality.</param>
    public ConcurrentLookup(
        IEnumerable<KeyValuePair<TKey, TValue>>? collection,
        int concurrencyLevel = 0,
        int capacity = 0,
        IEqualityComparer<TKey>? comparer = null,
        IEqualityComparer<TValue>? valueComparer = null)
    {
        _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

        // Create underlying dictionary.
        _dictionary = new ConcurrentDictionary<TKey, Grouping>(
            concurrencyLevel < 1 ? 4 * Environment.ProcessorCount : concurrencyLevel,
            capacity < 1 ? 32 : capacity,
            comparer ?? EqualityComparer<TKey>.Default);

        if (collection == null)
        {
            return;
        }

        foreach (var kvp in collection)
        {
            Add(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    ///     Tries to retrieve the group of values at the specified key.
    /// </summary>
    /// <param name="key">The key of the values to get.</param>
    /// <param name="grouping">The group of values to retrieve.</param>
    /// <returns>
    ///     Returns <see langword="true" /> if the value is retrieved; otherwise returns <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="key" /> is a <see langword="null" />.
    /// </exception>
    public bool TryGet([NotNull] TKey key, [NotNullWhen(true)] out IGrouping<TKey, TValue>? grouping)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            grouping = value;
            return true;
        }

        grouping = default;
        return false;
    }

    /// <summary>
    ///     Adds the specified kvp to the lookup. If the key already exists then the value is updated instead.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The corresponding values to add.</param>
    /// <returns>The new/updated values at the specified <paramref name="key" />.</returns>
    public IEnumerable<TValue> Add([NotNull] TKey key, TValue value) =>
        _dictionary.AddOrUpdate(key, k => new Grouping(this, k, value), (_, g) => g.Add(value));

    /// <summary>
    ///     Removes the entire group of values at the specified key.
    /// </summary>
    /// <param name="key">The key of the group to remove.</param>
    /// <returns>
    ///     Returns <see langword="true" /> if the group was removed successfully; otherwise returns <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="key" /> is a <see langword="null" />.
    /// </exception>
    public bool Remove([NotNull] TKey key) => _dictionary.TryRemove(key, out _);

    /// <summary>
    ///     Removes the entire group of values at the specified key.
    /// </summary>
    /// <param name="key">The key of the group to remove.</param>
    /// <param name="grouping"></param>
    /// <returns>
    ///     Returns <see langword="true" /> if the group was removed successfully; otherwise returns <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="key" /> is a <see langword="null" />.
    /// </exception>
    public bool TryRemove([NotNull] TKey key, out IGrouping<TKey, TValue>? grouping)
    {
        if (_dictionary.TryRemove(key, out var value))
        {
            grouping = value;
            return true;
        }

        grouping = default;
        return false;
    }

    /// <summary>
    ///     Removes the specified value from the group.
    /// </summary>
    /// <param name="key">The key of the group to remove the value from.</param>
    /// <param name="value">The value to remove.</param>
    /// <returns>
    ///     Returns <see langword="true" /> if the <paramref name="value" /> was removed; otherwise returns
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="key" /> is a <see langword="null" />.
    /// </exception>
    public bool Remove([NotNull] TKey key, TValue value) =>
        _dictionary.TryGetValue(key, out var grouping) && grouping.Remove(value);

    #region Nested type: Grouping

    /// <summary>
    ///     A collection of objects that share a common key.
    /// </summary>
    private class Grouping : IGrouping<TKey, TValue>
    {
        /// <summary>
        ///     The dictionary of values.  Note we use a dictionary to support removal.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, TValue> _dictionary = new();

        /// <summary>
        ///     The parent, which is the lookup that the group is contained in.
        /// </summary>
        private readonly ConcurrentLookup<TKey, TValue> _parent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Grouping" /> class.
        /// </summary>
        /// <param name="parent">The lookup that the group is contained in.</param>
        /// <param name="key">The key.</param>
        internal Grouping(ConcurrentLookup<TKey, TValue> parent, [NotNull] TKey key)
        {
            Key = key;
            _parent = parent;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Grouping" /> class.
        /// </summary>
        /// <param name="parent">The lookup that the group is contained in.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The values that correspond to <paramref name="key" />.</param>
        internal Grouping(
            ConcurrentLookup<TKey, TValue> parent,
            [NotNull] TKey key,
            TValue value)
        {
            Key = key;
            _parent = parent;
            Add(value);
        }

        /// <summary>
        ///     Adds the specified value to the group.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The new <paramref name="value" /> added to the group.</returns>
        public Grouping Add(TValue value)
        {
            var guid = Guid.NewGuid();
            _dictionary.AddOrUpdate(
                guid,
                _ => value,
                (_, _) => value);
            return this;
        }

        /// <summary>
        ///     Removes the specified value from the group.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns>
        ///     Returns <see langword="true" /> if the value was successfully removed; otherwise returns <see langword="false" />.
        /// </returns>
        public bool Remove(TValue value)
        {
            var valueComparer = _parent._valueComparer;
            foreach (var kvp in _dictionary)
            {
                if (!valueComparer.Equals(kvp.Value, value))
                {
                    continue;
                }

                _dictionary.TryRemove(kvp.Key, out _);
                if (_dictionary.Count < 1)
                {
                    _parent._dictionary.TryRemove(Key, out _);
                }

                return true;
            }

            return false;
        }

        #region IGrouping<TKey,TValue> Members

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<TValue> GetEnumerator() => _dictionary.Values.GetEnumerator();

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        ///     Gets the key that corresponds to this group.
        /// </summary>
        [NotNull]
        public TKey Key { get; }

        #endregion
    }

    #endregion

    #region ILookup<TKey,TValue> Members

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
    /// </returns>
    /// <filterpriority>1</filterpriority>
    public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator() => _dictionary.Values.GetEnumerator();

    /// <summary>
    ///     Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
    /// </returns>
    /// <filterpriority>2</filterpriority>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     Determines whether a specified key exists in the lookup.
    /// </summary>
    /// <param name="key">The key to search for in the lookup.</param>
    /// <returns>
    ///     Returns <see langword="true" /> if the specified <paramref name="key" /> is in the lookup; otherwise returns
    ///     <see langword="false" />.
    /// </returns>
    public bool Contains([NotNull] TKey key) => _dictionary.ContainsKey(key);

    /// <summary>
    ///     Gets the number of key/value collection pairs in the lookup.
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    ///     Retrieves the sequence of values indexed by a specified key.
    /// </summary>
    /// <param name="key">The key of the desired sequence of values.</param>
    /// <value>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1">IEnumerable</see> containing the sequence of values
    ///     indexed by
    ///     the specified<paramref name="key" />.
    /// </value>
    public IEnumerable<TValue> this[[NotNull] TKey key] =>
        TryGet(key, out var grouping) ? grouping : new Grouping(this, key);

    #endregion
}
