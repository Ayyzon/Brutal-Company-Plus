﻿// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

using System;
using System.Linq;
using BrutalCompanyPlus.Api;
using Unity.Netcode;
using UnityEngine;

namespace BrutalCompanyPlus.Objects;

public class BCNetworkManager : NetworkBehaviour {
    private const string Tag = $"[{nameof(BCNetworkManager)}]";

    public static BCNetworkManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
        Log($"{nameof(BCNetworkManager)} initialized!");
    }

    private void Update() {
        if (!IsServer || !IsHost) return; // only host should execute this
        if (EventManager.CurrentEvent == null) return; // no event is active
        EventManager.CurrentEvent.UpdateServer();
    }

    [ClientRpc]
    public void SyncEnemyTypeClientRpc(ulong NetworkId, bool IsOutside) {
        if (IsServer || IsHost) return; // only clients should execute this
        Log($"Syncing enemy type (id: {NetworkId}, outside: {IsOutside})... (client)");
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkId, out var networkObject)) {
            Log($"Bad network id {NetworkId} received from server.");
            return;
        }

        // Get the enemy AI component
        var enemy = networkObject.GetComponent<EnemyAI>();
        if (enemy == null) {
            Log($"Bad enemy received from server (id: {NetworkId}).");
            return;
        }

        // Sync the outside enemy flag. We might be too late at this point (Start might've already been called),
        // so set isOutside and allAINodes as well, just in case.
        enemy.enemyType.isOutsideEnemy = IsOutside;
        enemy.isOutside = IsOutside;
        enemy.allAINodes = GameObject.FindGameObjectsWithTag("AINode");
    }

    [ClientRpc]
    public void StartEventClientRpc(int EventId, int LevelId) {
        if (IsServer || IsHost) return; // only clients should execute this
        Log($"Received start event request: eid = {EventId}, lid = {LevelId}");

        // Parse event id
        var @event = EventRegistry.GetEvent(EventId);
        if (@event == null) {
            Log($"Bad event id {EventId} received from server.");
            return;
        }

        // Parse level id
        var level = StartOfRound.Instance.levels.ElementAtOrDefault(LevelId);
        if (level == null) {
            Log($"Bad level id {LevelId} received from server.");
            return;
        }

        // Execute event
        EventManager.StartEventClient(level, @event);
    }

    [ClientRpc]
    public void EndEventClientRpc() {
        if (IsServer || IsHost) return; // only clients should execute this
        Log("Received end event request... (client)");
        EventManager.EndEventClient(RoundManager.Instance.currentLevel);
    }

    [ClientRpc]
    public void SyncWeatherClientRpc(int LevelId, LevelWeatherType WeatherType) {
        Log($"Syncing weather type {WeatherType} on level {LevelId}...");

        // Parse level id
        var level = StartOfRound.Instance.levels.ElementAtOrDefault(LevelId);
        if (level == null) {
            Log($"Bad level id {LevelId} received from server.");
            return;
        }

        // Sync weather
        level.currentWeather = WeatherType;
    }

    private static void Log(string Message) => Plugin.Logger.LogWarning($"{Tag} {Message}");
}