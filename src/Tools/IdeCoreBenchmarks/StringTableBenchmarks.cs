// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace IdeCoreBenchmarks;

[MemoryDiagnoser]
public class StringTableBenchmarks
{
    private string[] _7kstrings;
    private TernaryStringTable _oldTable;
    private TernaryStringTableNew _newTable;
    private readonly int _loopCount = 100;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _7kstrings = File.ReadAllLines(@"c:\temp\7kstrings.txt");
        _oldTable = GetFilledOldTable();
        _newTable = GetFilledNewTable();
    }

    private TernaryStringTable GetFilledOldTable()
    {
        var table = new TernaryStringTable();

        foreach (var s in _7kstrings)
        {
            table.Intern(s);
        }

        return table;
    }

    private TernaryStringTableNew GetFilledNewTable()
    {
        var table = new TernaryStringTableNew();

        foreach (var s in _7kstrings)
        {
            table.Intern(s);
        }

        return table;
    }

    [Benchmark]
    public void OldIntern()
    {
        for (var i = 0; i < _loopCount; i++)
            _ = GetFilledOldTable();
    }

    [Benchmark]
    public void NewIntern()
    {
        for (var i = 0; i < _loopCount; i++)
            _ = GetFilledNewTable();
    }

    [Benchmark]
    public void OldGet()
    {
        for (var i = 0; i < _loopCount; i++)
        {
            for (var j = 0; j < _oldTable.Capacity; j++)
                _ = _oldTable.Get(j);
        }
    }

    [Benchmark]
    public void NewGet()
    {
        for (var i = 0; i < _loopCount; i++)
        {
            for (var j = 0; j < _newTable.Capacity; j++)
                _ = _newTable.Get(j);
        }
    }

    [Benchmark]
    public void OldInternAndGetAndVerify()
    {
        for (var i = 0; i < _loopCount; i++)
        {
            var table = new TernaryStringTable();
            var expectedValues = new List<(int, string)>();

            foreach (var s in _7kstrings)
            {
                var internedValue = table.Intern(s);
                expectedValues.Add((internedValue, s));
            }

            foreach (var (internedValue, s) in expectedValues)
            {
                var actual = table.Get(internedValue);
                Debug.Assert(actual == s);
            }
        }
    }

    [Benchmark]
    public void NewInternAndGetAndVerify()
    {
        for (var i = 0; i < _loopCount; i++)
        {
            var table = new TernaryStringTableNew();
            var expectedValues = new List<(int, string)>();

            foreach (var s in _7kstrings)
            {
                var internedValue = table.Intern(s);
                expectedValues.Add((internedValue, s));
            }

            foreach (var (internedValue, s) in expectedValues)
            {
                var actual = table.Get(internedValue);
                Debug.Assert(actual == s);
            }
        }
    }
}

internal interface IStringTable
{
    int Capacity { get; }
    int Intern(string value);
    string Get(int index);
}

internal sealed class TernaryStringTableNew : IStringTable
{
    private const int RootParentNodeId = int.MaxValue;
    private const int EmptyStringId = int.MaxValue;

    private readonly List<ReadOnlyMemory<char>> _cachedSplitList = new List<ReadOnlyMemory<char>>(); // Cache this to reduce the number of allocations.
    private readonly List<Node> _nodes;

    public TernaryStringTableNew(int capacity = 1)
    {
        _nodes = new List<Node>(capacity)
        {
            new Node
            {
                ParentNodeId = RootParentNodeId
            }
        };
    }

    public int Capacity
    {
        get => _nodes.Count;
    }

    /// <summary>
    /// Returns an integer that can be used to recreate the specified string at a later time by calling Get.
    /// </summary>
    public int Intern(string value)
    {
        int nodeIndex = 0;
        using (List<ReadOnlyMemory<char>>.Enumerator wordIt = Split(value).GetEnumerator())
        {
            if (!wordIt.MoveNext())
            {
                return EmptyStringId; // This is an empty string.
            }

            while (true)
            {
                Node node = _nodes[nodeIndex];
                if (node.Value != null)
                {
                    // This node has a value. We must compare the current word against the value of the node to determine which
                    // direction we should traverse (Low, Mid, or High).
                    int comparer = Compare(node.Value, wordIt.Current.Span);
                    if (comparer < 0) // Traverse the Low node.
                    {
                        if (node.LowNodeId == 0) // This node does not have a low node so we must create it.
                        {
                            int newIndex = NewNode(nodeIndex);
                            node.LowNodeId = newIndex;
                            _nodes[nodeIndex] = node;
                        }

                        nodeIndex = node.LowNodeId;
                    }
                    else if (comparer == 0) // Traverse the Mid node.
                    {
                        if (node.MidNodeId == 0) // This node does not have a mid node so we must create it.
                        {
                            int newIndex = NewNode(nodeIndex);
                            node.MidNodeId = newIndex;
                            _nodes[nodeIndex] = node;
                        }

                        if (!wordIt.MoveNext())
                        {
                            break;
                        }

                        nodeIndex = node.MidNodeId;
                    }
                    else // Traverse the High node.
                    {
                        if (node.HighNodeId == 0) // This node does not have a high node so we must create it.
                        {
                            int newIndex = NewNode(nodeIndex);
                            node.HighNodeId = newIndex;
                            _nodes[nodeIndex] = node;
                        }

                        nodeIndex = node.HighNodeId;
                    }
                }
                else
                {
                    // This node does not have a value.
                    node.Value = wordIt.Current.ToString();
                    _nodes[nodeIndex] = node;

                    // This string no longer shares prefixes with any other strings, so create mid _nodes with the remaining parts.
                    while (wordIt.MoveNext())
                    {
                        node = _nodes[nodeIndex];
                        int newIndex = NewNode(nodeIndex, wordIt.Current.ToString());
                        node.MidNodeId = newIndex;
                        _nodes[nodeIndex] = node;
                        nodeIndex = newIndex;
                    }

                    break;
                }
            }
        }

        return nodeIndex;
    }

    /// <summary>
    /// Gets a string for the specified index.
    /// </summary>
    public string Get(int index)
    {
        if (index == EmptyStringId)
        {
            return string.Empty;
        }

        Stack<string> parts = new Stack<string>();

        int currentIndex = index;
        while (true)
        {
            Node node = _nodes[currentIndex];
            if (index == currentIndex && node.Value != null)
            {
                parts.Push(node.Value);
            }

            if (node.ParentNodeId == RootParentNodeId)
            {
                break; // We've reached the root node.
            }

            Node parent = _nodes[node.ParentNodeId];
            if (parent.MidNodeId == currentIndex) // If the parent's mid node is the current node, then the text in the parent node is part of the string.
            {
                parts.Push(parent.Value);
            }

            currentIndex = node.ParentNodeId;
        }

        return string.Concat(parts);
    }

    /// <summary>
    /// Splits the specified string into parts.
    /// </summary>
    internal List<ReadOnlyMemory<char>> Split(string text)
    {
        _cachedSplitList.Clear();

        // The first string should include everything from the first character up through the first semicolon.
        int i = 0;
        int start = 0;
        while (i < text.Length)
        {
            if (text[i] == ';')
            {
                start = i + 1;
                _cachedSplitList.Add(text.AsMemory().Slice(0, i + 1));
                break;
            }

            i++;
        }

        // Split the remaining parts of the string on the forward slash character.
        bool foundSlash = false;
        for (; i < text.Length; i++)
        {
            if (text[i] == '/')
            {
                foundSlash = true;
            }
            else if (foundSlash)
            {
                _cachedSplitList.Add(text.AsMemory().Slice(start, i - start));
                start = i;
                foundSlash = false;
            }
        }

        if (start != text.Length)
        {
            _cachedSplitList.Add(text.AsMemory().Slice(start));
        }

        return _cachedSplitList;
    }

    private int Compare(string left, ReadOnlySpan<char> right)
    {
        // The strings don't have to be compared lexicographically, it just needs a deterministic comparer.
        // We can use hash code for a quick comparison which avoids a character by character comparison in many cases
        // and also keeps the tree balanced in the case where strings are added lexicographically.

        int hasCodeDelta = GetHashCode(left.AsSpan()) - GetHashCode(right);
        if (hasCodeDelta == 0)
        {
            return left.AsSpan().CompareTo(right, StringComparison.Ordinal);
        }

        return hasCodeDelta;
    }

    internal const int FnvOffsetBias = unchecked((int)2166136261);
    internal const int FnvPrime = 16777619;

    public static int GetHashCode(ReadOnlySpan<char> s)
    {
        int hashCode = FnvOffsetBias;
        for (int i = 0; i < s.Length; i++)
        {
            hashCode = unchecked((hashCode ^ s[i]) * FnvPrime);
        }

        return hashCode;
    }

    /// <summary>
    /// Creates a node with the specified parentId and returns its Id.
    /// </summary>
    private int NewNode(int parentId, string value = null)
    {
        int id = _nodes.Count;
        _nodes.Add(new Node
        {
            ParentNodeId = parentId,
            Value = value
        });

        return id;
    }

    /// <summary>
    /// Represents a node in a ternary tree.
    /// </summary>
    private struct Node
    {
        public int LowNodeId;
        public int MidNodeId;
        public int HighNodeId;
        public int ParentNodeId;
        public string Value;
    }
}

internal sealed class TernaryStringTable : IStringTable
{
    private const int RootParentNodeId = int.MaxValue;
    private const int EmptyStringId = int.MaxValue;

    private readonly List<string> _cachedSplitList = new List<string>(); // Cache this to reduce the number of allocations.
    private readonly List<Node> _nodes;

    public TernaryStringTable(int capacity = 1)
    {
        _nodes = new List<Node>(capacity)
        {
            new Node
            {
                ParentNodeId = RootParentNodeId
            }
        };
    }

    public int Capacity
    {
        get => _nodes.Count;
    }

    /// <summary>
    /// Returns an integer that can be used to recreate the specified string at a later time by calling Get.
    /// </summary>
    public int Intern(string value)
    {
        int nodeIndex = 0;
        using (List<string>.Enumerator wordIt = Split(value).GetEnumerator())
        {
            if (!wordIt.MoveNext())
            {
                return EmptyStringId; // This is an empty string.
            }

            while (true)
            {
                Node node = _nodes[nodeIndex];
                if (node.Value != null)
                {
                    // This node has a value. We must compare the current word against the value of the node to determine which
                    // direction we should traverse (Low, Mid, or High).
                    int comparer = Compare(node.Value, wordIt.Current);
                    if (comparer < 0) // Traverse the Low node.
                    {
                        if (node.LowNodeId == 0) // This node does not have a low node so we must create it.
                        {
                            int newIndex = NewNode(nodeIndex);
                            node.LowNodeId = newIndex;
                            _nodes[nodeIndex] = node;
                        }

                        nodeIndex = node.LowNodeId;
                    }
                    else if (comparer == 0) // Traverse the Mid node.
                    {
                        if (node.MidNodeId == 0) // This node does not have a mid node so we must create it.
                        {
                            int newIndex = NewNode(nodeIndex);
                            node.MidNodeId = newIndex;
                            _nodes[nodeIndex] = node;
                        }

                        if (!wordIt.MoveNext())
                        {
                            break;
                        }

                        nodeIndex = node.MidNodeId;
                    }
                    else // Traverse the High node.
                    {
                        if (node.HighNodeId == 0) // This node does not have a high node so we must create it.
                        {
                            int newIndex = NewNode(nodeIndex);
                            node.HighNodeId = newIndex;
                            _nodes[nodeIndex] = node;
                        }

                        nodeIndex = node.HighNodeId;
                    }
                }
                else
                {
                    // This node does not have a value.
                    node.Value = wordIt.Current;
                    _nodes[nodeIndex] = node;

                    // This string no longer shares prefixes with any other strings, so create mid _nodes with the remaining parts.
                    while (wordIt.MoveNext())
                    {
                        node = _nodes[nodeIndex];
                        int newIndex = NewNode(nodeIndex, wordIt.Current);
                        node.MidNodeId = newIndex;
                        _nodes[nodeIndex] = node;
                        nodeIndex = newIndex;
                    }

                    break;
                }
            }
        }

        return nodeIndex;
    }

    /// <summary>
    /// Gets a string for the specified index.
    /// </summary>
    public string Get(int index)
    {
        if (index == EmptyStringId)
        {
            return string.Empty;
        }

        Stack<string> parts = new Stack<string>();

        int currentIndex = index;
        while (true)
        {
            Node node = _nodes[currentIndex];
            if (index == currentIndex && node.Value != null)
            {
                parts.Push(node.Value);
            }

            if (node.ParentNodeId == RootParentNodeId)
            {
                break; // We've reached the root node.
            }

            Node parent = _nodes[node.ParentNodeId];
            if (parent.MidNodeId == currentIndex) // If the parent's mid node is the current node, then the text in the parent node is part of the string.
            {
                parts.Push(parent.Value);
            }

            currentIndex = node.ParentNodeId;
        }

        return string.Concat(parts);
    }

    /// <summary>
    /// Splits the specified string into parts.
    /// </summary>
    internal List<string> Split(string text)
    {
        _cachedSplitList.Clear();

        // The first string should include everything from the first character up through the first semicolon.
        int i = 0;
        int start = 0;
        while (i < text.Length)
        {
            if (text[i] == ';')
            {
                start = i + 1;
                _cachedSplitList.Add(text.Substring(0, i + 1));
                break;
            }

            i++;
        }

        // Split the remaining parts of the string on the forward slash character.
        bool foundSlash = false;
        for (; i < text.Length; i++)
        {
            if (text[i] == '/')
            {
                foundSlash = true;
            }
            else if (foundSlash)
            {
                _cachedSplitList.Add(text.Substring(start, i - start));
                start = i;
                foundSlash = false;
            }
        }

        if (start != text.Length)
        {
            _cachedSplitList.Add(text.Substring(start));
        }

        return _cachedSplitList;
    }

    private int Compare(string left, string right)
    {
        // The strings don't have to be compared lexicographically, it just needs a deterministic comparer.
        // We can use hash code for a quick comparison which avoids a character by character comparison in many cases
        // and also keeps the tree balanced in the case where strings are added lexicographically.

        int hasCodeDelta = StringComparer.Ordinal.GetHashCode(left) - StringComparer.Ordinal.GetHashCode(right);
        if (hasCodeDelta == 0)
        {
            return StringComparer.Ordinal.Compare(left, right);
        }

        return hasCodeDelta;
    }

    /// <summary>
    /// Creates a node with the specified parentId and returns its Id.
    /// </summary>
    private int NewNode(int parentId, string value = null)
    {
        int id = _nodes.Count;
        _nodes.Add(new Node
        {
            ParentNodeId = parentId,
            Value = value
        });

        return id;
    }

    /// <summary>
    /// Represents a node in a ternary tree.
    /// </summary>
    private struct Node
    {
        public int LowNodeId;
        public int MidNodeId;
        public int HighNodeId;
        public int ParentNodeId;
        public string Value;
    }
}
