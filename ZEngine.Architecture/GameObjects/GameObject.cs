﻿using System.Collections.Immutable;
using ZEngine.Architecture.Communication.Messages;
using ZEngine.Architecture.Components;

namespace ZEngine.Architecture.GameObjects;

/// <summary>
/// Implementation of standard game object.
/// </summary>
public class GameObject : IGameObject
{
    /// <summary>
    /// Internal hash set of existing components.
    /// </summary>
    private readonly Dictionary<Type, IGameComponent> _components = new();

    /// <summary>
    /// Internal message handler.
    /// </summary>
    private readonly MessageHandler _messageHandler;

    /// <summary>
    /// Current state of game object.
    /// </summary>
    private bool _active;

    public GameObject(string name = "New Game Object", bool active = true)
    {
        _messageHandler = new MessageHandler(this);
        Name = name;
        _active = active;

        Initialize();
    }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public bool Active
    {
        get => _active;
        set
        {
            _active = value;
            SendMessage(_active ? SystemMethod.OnEnable : SystemMethod.OnDisable);
        }
    }

    /// <inheritdoc />
    public Transform Transform { get; private set; } = null!;

    /// <summary>
    /// For the update of children, we want only active game objects.
    /// </summary>
    private IEnumerable<IGameObject> ActiveChildren => Transform.Select(x => x.GameObject).Where(x => x.Active);

    /// <summary>
    /// Initializes the game object by adding required components.
    /// </summary>
    private void Initialize()
    {
        Transform = AddComponent<Transform>();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IGameComponent> Components => _components.Values.ToImmutableHashSet();

    /// <inheritdoc />
    public TComponent AddComponent<TComponent>() where TComponent : IGameComponent
    {
        return (TComponent) AddComponent(typeof(TComponent));
    }

    /// <inheritdoc />
    public IGameComponent AddComponent(Type componentType)
    {
        if (!componentType.IsAssignableTo(typeof(IGameComponent)))
        {
            throw new ArgumentException("Component type must be assignable to IGameComponent.");
        }

        if (_components.ContainsKey(componentType))
        {
            throw new ArgumentException($"Component of type {componentType.FullName} already exists on {Name}.");
        }

        IGameComponent? component = (IGameComponent?) Activator.CreateInstance(componentType);
        if (component is null)
        {
            throw new ArgumentException($"Component of type {componentType.FullName} could not be created.");
        }

        component.GameObject = this;
        _components.Add(componentType, component);
        component.SendMessage(SystemMethod.Awake);
        component.Enabled = true;

        return component;
    }

    /// <inheritdoc />
    public TComponent? GetComponent<TComponent>() where TComponent : IGameComponent
    {
        return (TComponent?) GetComponent(typeof(TComponent));
    }

    /// <inheritdoc />
    public IGameComponent? GetComponent(Type componentType)
    {
        _components.TryGetValue(componentType, out IGameComponent? component);
        return component;
    }

    /// <inheritdoc />
    public TComponent GetRequiredComponent<TComponent>() where TComponent : IGameComponent
    {
        return (TComponent) GetRequiredComponent(typeof(TComponent));
    }

    /// <inheritdoc />
    public IGameComponent GetRequiredComponent(Type componentType)
    {
        if (!_components.ContainsKey(componentType))
        {
            throw new ArgumentException($"Component of type {componentType.FullName} does not exist on {Name}.");
        }

        return _components[componentType];
    }

    /// <inheritdoc />
    public bool HasComponent<TComponent>() where TComponent : IGameComponent
    {
        return HasComponent(typeof(TComponent));
    }

    /// <inheritdoc />
    public bool HasComponent(Type componentType)
    {
        return _components.ContainsKey(componentType);
    }

    /// <inheritdoc />
    public bool RemoveComponent<TComponent>() where TComponent : IGameComponent
    {
        return RemoveComponent(typeof(TComponent));
    }

    /// <inheritdoc />
    public bool RemoveComponent(Type type)
    {
        IGameComponent component = GetRequiredComponent(type);
        component.SendMessage(SystemMethod.OnDestroy);

        return _components.Remove(type);
    }

    /// <inheritdoc />
    public void SendMessage(string target)
    {
        throw new NotSupportedException("Game Object can be messaged only by System Methods.");
    }

    /// <inheritdoc />
    public void SendMessage(SystemMethod systemTarget)
    {
        _messageHandler.Handle(systemTarget);
    }

    /// <summary>
    /// Enables all registered components.
    /// </summary>
    private void OnEnable()
    {
        foreach (IGameComponent component in _components.Values)
        {
            component.SendMessage(SystemMethod.OnEnable);
        }
    }

    /// <summary>
    /// Disables all registered components.
    /// </summary>
    private void OnDisable()
    {
        foreach (IGameComponent component in _components.Values)
        {
            component.SendMessage(SystemMethod.OnDisable);
        }
    }

    /// <summary>
    /// Updates all child game objects and components.
    /// </summary>
    private void Update()
    {
        foreach (IGameObject gameObject in ActiveChildren)
        {
            gameObject.SendMessage(SystemMethod.Update);
        }

        foreach (IGameComponent gameComponent in _components.Values)
        {
            gameComponent.SendMessage(SystemMethod.Update);
        }
    }
}