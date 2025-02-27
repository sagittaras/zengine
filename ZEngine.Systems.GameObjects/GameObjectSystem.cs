﻿using Microsoft.Extensions.Logging;
using ZEngine.Architecture.Communication.Events;
using ZEngine.Architecture.Communication.Messages;
using ZEngine.Architecture.GameObjects;
using ZEngine.Core;
using ZEngine.Systems.GameObjects.Events;

namespace ZEngine.Systems.GameObjects;

/// <summary>
///     System responsible for managing game objects.
/// </summary>
public class GameObjectSystem : IGameSystem
{
    /// <summary>
    ///     Thread-safe lock for adding new game objects.
    /// </summary>
    private static readonly object AddingLock = new();

    /// <summary>
    ///     Thread-safe lock for removing game objects.
    /// </summary>
    private static readonly object RemovingLock = new();

    /// <summary>
    ///     Collection of all game objects, that were destroyed in the current frame.
    /// </summary>
    private readonly HashSet<IGameObject> _destroyedGameObjects = new();

    /// <summary>
    ///     Event Mediator serves for notifying about new game object or removed ones.
    /// </summary>
    private readonly IEventMediator _eventMediator;

    /// <summary>
    ///     Collection of all game objects available in the game.
    /// </summary>
    private readonly HashSet<IGameObject> _gameObjects = new();

    /// <summary>
    ///     Logs exceptions catched from game objects.
    /// </summary>
    private readonly ILogger<GameObjectSystem> _logger;

    /// <summary>
    ///     Collection of all game objects, that were created in the current frame.
    /// </summary>
    private readonly HashSet<IGameObject> _newGameObjects = new();

    /// <summary>
    ///     Used to satisfy game objects component dependencies.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    public GameObjectSystem(IEventMediator eventMediator, ILogger<GameObjectSystem> logger, IServiceProvider serviceProvider)
    {
        _eventMediator = eventMediator;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    ///     Every update, we want to iterate over active game objects and those, who are not children of any other game object.
    /// </summary>
    /// <remarks>
    ///     Children should be updated by their parents.
    /// </remarks>
    private IEnumerable<IGameObject> ActiveRootObjects => _gameObjects.Where(x => x is { Active: true, Transform.Parent: null });

    /// <summary>
    ///     From the native systems, this system has the highest priority.
    /// </summary>
    public int Priority => 1;

    /// <inheritdoc />
    public void Initialize()
    {
        GameObjectManager.CreateInstance(this, _serviceProvider);
    }

    /// <inheritdoc />
    public void Update()
    {
        DestroyObjects();
        AddNewObjects();

        foreach (IGameObject gameObject in ActiveRootObjects)
            try
            {
                gameObject.SendMessage(SystemMethod.Update);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occured while updating game object {GameObjectName}", gameObject.Name);
            }
    }

    /// <inheritdoc />
    public void CleanUp()
    {
        foreach (IGameObject gameObject in ActiveRootObjects)
            try
            {
                gameObject.SendMessage(SystemMethod.OnDestroy);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occured while destroying game object {GameObjectName}", gameObject.Name);
            }
    }

    /// <summary>
    ///     Registers a game object in the system.
    /// </summary>
    /// <param name="gameObject"></param>
    public void Register(IGameObject gameObject)
    {
        try
        {
            gameObject.SendMessage(SystemMethod.Awake);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An exception occured while awaking game object {GameObjectName}", gameObject.Name);
        }

        lock (AddingLock)
        {
            _newGameObjects.Add(gameObject);
        }
    }

    /// <summary>
    ///     Marks a game object as destroyed.
    /// </summary>
    /// <param name="gameObject"></param>
    public void Unregister(IGameObject gameObject)
    {
        lock (RemovingLock)
        {
            _destroyedGameObjects.Add(gameObject);
        }
    }

    /// <summary>
    ///     Adds new game objects to the collection of all game objects.
    /// </summary>
    private void AddNewObjects()
    {
        lock (AddingLock)
        {
            foreach (IGameObject gameObject in _newGameObjects)
            {
                try
                {
                    gameObject.Active = true;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "An exception occured while activating game object {GameObjectName}", gameObject.Name);
                }

                _eventMediator.Notify(new GameObjectAdded(gameObject));
                _gameObjects.Add(gameObject);
            }

            _newGameObjects.Clear();
        }
    }

    /// <summary>
    ///     Destroys game objects, that were marked as destroyed.
    /// </summary>
    private void DestroyObjects()
    {
        lock (RemovingLock)
        {
            // We need to copy the collection, if there is any object destroyed under this frame, it will be added to the collection
            HashSet<IGameObject> toDestroy = _destroyedGameObjects.ToHashSet();
            foreach (IGameObject gameObject in toDestroy)
            {
                try
                {
                    gameObject.SendMessage(SystemMethod.OnDestroy);
                    _destroyedGameObjects.Remove(gameObject);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception occured while destroying game object {GameObjectName}", gameObject.Name);
                }

                _eventMediator.Notify(new GameObjectRemoved(gameObject));
                _gameObjects.Remove(gameObject);
            }
        }
    }
}