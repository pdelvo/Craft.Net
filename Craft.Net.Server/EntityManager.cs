﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Craft.Net.Data;
using Craft.Net.Data.Entities;
using Craft.Net.Server.Packets;

namespace Craft.Net.Server
{
    /// <summary>
    /// Manages transmission of entities from client to client
    /// </summary>
    public class EntityManager
    {
        internal static int nextEntityId = 1;
        internal MinecraftServer server;

        public EntityManager(MinecraftServer server)
        {
            this.server = server;
        }

        public void SpawnEntity(World world, Entity entity)
        {
            entity.Id = nextEntityId++;
            world.Entities.Add(entity);
            // Get nearby clients in the same world
            var clients = GetClientsInWorld(world)
                .Where(c => !c.IsDisconnected && c.Entity.Position.DistanceTo(entity.Position) < (c.ViewDistance * Chunk.Width));
            entity.PropertyChanged += EntityOnPropertyChanged;

            if (clients.Count() != 0)
            {
                // Spawn entity on relevant clients
                if (entity is PlayerEntity)
                {
                    // Isolate the client being spawned
                    var client = clients.First(c => c.Entity == entity);
                    client.Entity.BedStateChanged += EntityOnUpdateBedState;
                    client.Entity.BedTimerExpired += EntityOnBedTimerExpired;
                    clients = clients.Where(c => c.Entity != entity);
                    clients.ToList().ForEach(c => {
                        c.SendPacket(new SpawnNamedEntityPacket(client));
                        c.KnownEntities.Add(client.Entity.Id);
                    });
                }
            }
            server.ProcessSendQueue();
        }

        private void EntityOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // Handles changes in entity properties
            var entity = sender as Entity;
            if (entity is PlayerEntity)
            {
                var player = entity as PlayerEntity;
                var client = GetClient(player);
                switch (propertyChangedEventArgs.PropertyName)
                {
                    case "Health":
                    case "Food":
                    case "FoodSaturation":
                        client.SendPacket(new UpdateHealthPacket(player.Health, player.Food, player.FoodSaturation));
                        if (player.Health <= 0)
                            KillEntity(player);
                        break;
                    case "SpawnPoint":
                        client.SendPacket(new SpawnPositionPacket(player.SpawnPoint));
                        break;
                    case "GameMode":
                        client.SendPacket(new ChangeGameStatePacket(GameState.ChangeGameMode, client.Entity.GameMode));
                        break;
                }
            }
        }

        private void EntityOnBedTimerExpired(object sender, EventArgs eventArgs)
        {
            var player = sender as PlayerEntity;
            var world = GetEntityWorld(player);
            var clients = GetClientsInWorld(world);
            foreach (var minecraftClient in clients)
            {
                if (minecraftClient.Entity.BedPosition == -Vector3.One)
                    return;
            }
            var level = server.GetLevel(world);
            level.Time = 0;
            foreach (var minecraftClient in clients)
            {
                minecraftClient.SendPacket(new AnimationPacket(minecraftClient.Entity.Id, Animation.LeaveBed));
                foreach (var client in GetKnownClients(minecraftClient.Entity))
                    client.SendPacket(new AnimationPacket(minecraftClient.Entity.Id, Animation.LeaveBed));
                minecraftClient.SendPacket(new TimeUpdatePacket(level.Time));
                minecraftClient.Entity.BedPosition = -Vector3.One;
            }
            server.ProcessSendQueue();
        }

        private void EntityOnUpdateBedState(object sender, EventArgs eventArgs)
        {
            var player = sender as PlayerEntity;
            var clients = GetKnownClients(player);
            if (player.BedPosition == -Vector3.One)
            {
                // Leave bed
                GetClient(player).SendPacket(new AnimationPacket(player.Id, Animation.LeaveBed));
                foreach (var minecraftClient in clients)
                    minecraftClient.SendPacket(new AnimationPacket(player.Id, Animation.LeaveBed));
            }
            else
            {
                if (server.GetLevel(GetEntityWorld(player)).Time % 24000 < 12000)
                {
                    GetClient(player).SendChat("You can only sleep at night.");
                    return;
                }
                // Enter bed
                GetClient(player).SendPacket(new UseBedPacket(player.Id, player.BedPosition));
                foreach (var minecraftClient in clients)
                    minecraftClient.SendPacket(new UseBedPacket(player.Id, player.BedPosition));
                player.SpawnPoint = player.BedPosition;
            }
            server.ProcessSendQueue();
        }

        public void SendClientEntities(MinecraftClient client)
        {
            var world = GetEntityWorld(client.Entity);
            var clients = GetClientsInWorld(world)
                .Where(c => !c.IsDisconnected && c.Entity.Position.DistanceTo(client.Entity.Position) < 
                    (c.ViewDistance * Chunk.Width) && c != client);
            foreach (var _client in clients)
            {
                client.KnownEntities.Add(_client.Entity.Id);
                client.SendPacket(new SpawnNamedEntityPacket(_client));
                client.SendPacket(new EntityEquipmentPacket(_client.Entity.Id, EntityEquipmentSlot.HeldItem, _client.Entity.Inventory[_client.Entity.SelectedSlot]));
            }
        }

        public void DespawnEntity(Entity entity)
        {
            DespawnEntity(GetEntityWorld(entity), entity);
        }

        public void DespawnEntity(World world, Entity entity)
        {
            if (world == null)
                return;
            if (!world.Entities.Contains(entity))
                return;
            entity.PropertyChanged -= EntityOnPropertyChanged;
            world.Entities.Remove(entity);
            var clients = GetClientsInWorld(world).Where(c => c.KnownEntities.Contains(entity.Id));
            foreach (var client in clients)
            {
                client.KnownEntities.Remove(entity.Id);
                client.SendPacket(new DestroyEntityPacket(entity.Id));
            }
            server.ProcessSendQueue();
        }

        /// <summary>
        /// Note: This will not correctly kill players. If you wish to kill
        /// a player, set its health to zero and EntityManager will automatically
        /// kill the player through the correct means.
        /// </summary>
        public void KillEntity(LivingEntity entity)
        {
            entity.DeathAnimationComplete += (sender, args) =>
                {
                    if (entity.Health <= 0)
                        DespawnEntity(entity);
                };
            entity.Kill();
            foreach (var client in GetKnownClients(entity))
                client.SendPacket(new EntityStatusPacket(entity.Id, EntityStatus.Dead));
        }

        public void UpdateEntity(Entity entity)
        {
            if (!server.Levels.Any(l => l.World.Entities.Contains(entity)))
                return;
            var world = GetEntityWorld(entity);
            var flooredPosition = entity.Position.Floor();

            // check for walked on blocks
            if (flooredPosition.Y == entity.Position.Y && entity.OldPosition.Floor() != entity.OldPosition)
            {
                if ((flooredPosition + Vector3.Down).Y >= 0 && (flooredPosition + Vector3.Down).Y <= Chunk.Height)
                {
                    var blockOn = world.GetBlock(flooredPosition + Vector3.Down);
                    blockOn.OnBlockWalkedOn(world, flooredPosition + Vector3.Down, entity);
                }
            }

            if ((int)(entity.Position.X) != (int)(entity.OldPosition.X) ||
                (int)(entity.Position.Y) != (int)(entity.OldPosition.Y) ||
                (int)(entity.Position.Z) != (int)(entity.OldPosition.Z))
            {
                if (flooredPosition.Y >= 0 && flooredPosition.Y <= Chunk.Height)
                {
                    var blockIn = world.GetBlock(flooredPosition);
                    blockIn.OnBlockWalkedIn(world, flooredPosition, entity);
                }
            }

            // Update location with known clients
            if (entity.Position.DistanceTo(entity.OldPosition) > 0.1d ||
                entity.Pitch != entity.OldPitch || entity.Yaw != entity.OldYaw)
            {
                var knownClients = GetClientsInWorld(world).Where(c => c.KnownEntities.Contains(entity.Id));
                foreach (var client in knownClients)
                {
                    client.SendPacket(new EntityTeleportPacket(entity));
                    if (entity.Yaw != entity.OldYaw)
                        client.SendPacket(new EntityHeadLookPacket(entity));
                    // TODO: Further research into relative movement
                    // When relative movement packets are used, the remote
                    // clients inevitably see each other in a very inaccurate
                    // position.
                }
                server.ProcessSendQueue();
                entity.OldPosition = entity.Position;
            }
        }

        public IEnumerable<MinecraftClient> GetKnownClients(Entity entity)
        {
            return GetClientsInWorld(GetEntityWorld(entity)).Where(c => c.KnownEntities.Contains(entity.Id));
        }

        public IEnumerable<MinecraftClient> GetClientsInWorld(World world)
        {
            return server.Clients.Where(c => world.Entities.Contains(c.Entity));
        }

        public MinecraftClient GetClient(PlayerEntity entity)
        {
            var clients = server.Clients.Where(c => c.Entity == entity);
            if (!clients.Any())
                return null;
            return clients.First();
        }

        public World GetEntityWorld(Entity entity)
        {
            var firstOrDefault = server.Levels.FirstOrDefault(level => level.World.Entities.Contains(entity));
            if (firstOrDefault != null)
                return firstOrDefault.World;
            return server.DefaultWorld;
        }
    }
}
