﻿using System.Reflection;
using FastDeepCloner;
using ZEngine.Architecture.Communication.Messages;
using ZEngine.Architecture.GameObjects;

namespace ZEngine.Architecture.Components;

/// <summary>
/// Abstract class describing basic component attached to game object.
/// </summary>
public abstract class GameComponent : IGameComponent
{
    /// <summary>
    /// Message handler for this component.
    /// </summary>
    private readonly MessageHandler _messageHandler;

    /// <summary>
    /// Backing field.
    /// </summary>
    private bool _enabled;

    /// <inheritdoc />
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            _messageHandler.Handle(_enabled ? SystemMethod.OnEnable : SystemMethod.OnDisable);
        }
    }

    /// <inheritdoc />
    public IGameObject GameObject { get; set; } = null!;

    protected GameComponent()
    {
        _messageHandler = new MessageHandler(this);
    }

    /// <inheritdoc />
    public IGameComponent Clone()
    {
        FastDeepClonerSettings settings = new()
        {
            CloneLevel = CloneLevel.Hierarki,
            FieldType = FieldType.Both
        };

        return this.Clone(settings);
    }

    /// <inheritdoc />
    public void SendMessage(string target)
    {
        _messageHandler.Handle(target);
    }

    /// <inheritdoc />
    public void SendMessage(SystemMethod systemTarget)
    {
        _messageHandler.Handle(systemTarget);
    }
}