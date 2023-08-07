﻿using System.Collections;
using System.Numerics;

namespace ZEngine.Architecture.Components;

/// <summary>
/// Basic component providing transform functionality.
/// </summary>
public class Transform : GameComponent, IEnumerable<Transform>
{
    /// <summary>
    /// Collection of all available children of this transform.
    /// </summary>
    private readonly HashSet<Transform> _children = new();
    
    /// <summary>
    /// Backing field holding actual position of the game object.
    /// </summary>
    private Vector3 _position = Vector3.Zero;
    
    /// <summary>
    /// Backing field holding actual position of the game object relative to its parent.
    /// </summary>
    private Vector3 _localPosition = Vector3.Zero;

    /// <summary>
    /// Position of the game object.
    /// </summary>
    public Vector3 Position
    {
        get => _position;
        set
        {
            _position = value;
            _localPosition = Parent is null ? value : value - Parent.Position;
        }
    }

    /// <summary>
    /// Position of the game object relative to its parent.
    /// </summary>
    /// <remarks>
    /// If parent is null, this property is equal to <see cref="Position"/>.
    /// </remarks>
    public Vector3 LocalPosition
    {
        get => _localPosition;
        set
        {
            _localPosition = value;
            _position = Parent is null ? value : value + Parent.Position;
        }
    }
    
    /// <summary>
    /// Parent of the game object. If null, game object is a root object.
    /// </summary>
    public Transform? Parent { get; private set; }

    /// <summary>
    /// Sets parent of this transform.
    /// </summary>
    /// <param name="parent"></param>
    public void SetParent(Transform? parent)
    {
        if (parent is null)
        {
            Parent?._children.Remove(this);
            Parent = null;
            _localPosition = _position;
            return;
        }

        Parent = parent;
        Parent?._children.Add(this);
        _localPosition = _position - parent.Position;
    }
    
    /// <inheritdoc />
    public IEnumerator<Transform> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}