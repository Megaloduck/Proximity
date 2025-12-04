using System;
using System.Collections.Generic;
using System.Text;
using Proximity.Models;
using System.Collections.ObjectModel;

namespace Proximity.Services;

public class RoomService
{
    private readonly ObservableCollection<RoomInfo> _availableRooms = new();
    private readonly ObservableCollection<RoomInfo> _myRooms = new();
    private readonly DiscoveryService _discoveryService;

    public ObservableCollection<RoomInfo> AvailableRooms => _availableRooms;
    public ObservableCollection<RoomInfo> MyRooms => _myRooms;

    public event EventHandler<RoomInfo> RoomJoined;
    public event EventHandler<RoomInfo> RoomLeft;
    public event EventHandler<RoomInfo> RoomCreated;

    public RoomService(DiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
        InitializeSampleRooms();
    }

    private void InitializeSampleRooms()
    {
        // Sample rooms for testing
        _availableRooms.Add(new RoomInfo
        {
            RoomId = Guid.NewGuid().ToString(),
            RoomName = "General Chat",
            Description = "Main discussion room for everyone",
            RoomIcon = "💬",
            CreatedBy = "Admin",
            MemberCount = 5,
            HasVoice = true,
            HasWhiteboard = false,
            IsPrivate = false,
            IsJoined = false,
            CreatedAt = DateTime.Now.AddHours(-2)
        });

        _availableRooms.Add(new RoomInfo
        {
            RoomId = Guid.NewGuid().ToString(),
            RoomName = "Tech Support",
            Description = "Get help with technical issues",
            RoomIcon = "🛠️",
            CreatedBy = "Support",
            MemberCount = 3,
            HasVoice = true,
            HasWhiteboard = true,
            IsPrivate = false,
            IsJoined = false,
            CreatedAt = DateTime.Now.AddHours(-5)
        });

        _availableRooms.Add(new RoomInfo
        {
            RoomId = Guid.NewGuid().ToString(),
            RoomName = "Project Alpha",
            Description = "Private workspace for Project Alpha team",
            RoomIcon = "🚀",
            CreatedBy = "Manager",
            MemberCount = 8,
            HasVoice = true,
            HasWhiteboard = true,
            IsPrivate = true,
            IsJoined = false,
            CreatedAt = DateTime.Now.AddDays(-1)
        });
    }

    public async Task<RoomInfo> CreateRoomAsync(string name, string description, string icon,
        bool hasVoice, bool hasWhiteboard, bool isPrivate)
    {
        var room = new RoomInfo
        {
            RoomId = Guid.NewGuid().ToString(),
            RoomName = name,
            Description = description,
            RoomIcon = icon,
            CreatedBy = _discoveryService.LocalName,
            MemberCount = 1,
            HasVoice = hasVoice,
            HasWhiteboard = hasWhiteboard,
            IsPrivate = isPrivate,
            IsJoined = true,
            CreatedAt = DateTime.Now
        };

        _availableRooms.Add(room);
        _myRooms.Add(room);

        RoomCreated?.Invoke(this, room);
        return await Task.FromResult(room);
    }

    public async Task JoinRoomAsync(RoomInfo room)
    {
        room.IsJoined = true;
        room.MemberCount++;

        if (!_myRooms.Contains(room))
        {
            _myRooms.Add(room);
        }

        RoomJoined?.Invoke(this, room);
        await Task.CompletedTask;
    }

    public async Task LeaveRoomAsync(RoomInfo room)
    {
        room.IsJoined = false;
        room.MemberCount--;

        _myRooms.Remove(room);

        RoomLeft?.Invoke(this, room);
        await Task.CompletedTask;
    }

    public async Task<List<RoomInfo>> GetAvailableRoomsAsync()
    {
        // In real implementation, this would query network for rooms
        return await Task.FromResult(_availableRooms.ToList());
    }

    public async Task SendRoomMessageAsync(string roomId, string message)
    {
        // Implement room message sending
        await Task.CompletedTask;
    }

    public void BroadcastRoomUpdate(RoomInfo room)
    {
        // Broadcast room update to network
    }
}