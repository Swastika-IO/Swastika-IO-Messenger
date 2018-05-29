using Messenger.Lib.Helpers;
using Microsoft.AspNetCore.SignalR;
using Swastika.Domain.Core.ViewModels;
using Swastika.Messenger.Lib.ViewModels.Hub;
using Swastika.Messenger.Lib.ViewModels.Messenger;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Swastika.UI.Core.SignalR;
using Swastika.Messenger.Lib.SignalR.Models.Games.Zoombies;
using Newtonsoft.Json.Linq;

namespace Swastika.Messenger.Lib.SignalR.Hubs
{
    public class GameHub : BaseSignalRHub
    {
        const string receiveMethod = "receiveMessage";
        const string defaultRoom = "zoombies";
        public static List<RoomViewModel> rooms = new List<RoomViewModel>();
        [HubMethodName("updatePlayer")]
        public void UpdatePlayer(PlayerViewModel request)
        {
            SendToGroup(request, GameReponseKey.UpdatePlayer, request.Room, false);
        }
        [HubMethodName("createRoom")]
        public async Task CreateRoom(PlayerViewModel request)
        {
            request.ConnectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(request.UID))
            {
                var connectingUser = await PlayerViewModel.CreateRoomAsync(request, rooms);
                if (connectingUser.IsSucceed)
                {
                    string ip = Context.Connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                        ? Context.Connection.RemoteIpAddress.MapToIPv6().ToString()
                        : Context.Connection.RemoteIpAddress.MapToIPv4().ToString();
                    connectingUser.Data.IsOnline = true;
                    connectingUser.Data.Ip = ip;
                    var data = new ConnectViewModel()
                    {
                        Player = connectingUser.Data,
                        Others = PlayerViewModel.Repository.GetModelListBy(p => p.Uid != connectingUser.Data.UID
                                                                         && p.Room == connectingUser.Data.Room && !string.IsNullOrEmpty(p.ConnectionId)).Data
                    };
                    SendToClient(data, GameReponseKey.CreateRoom, Context.ConnectionId);
                    SendToAll(data.Player.CurrentRoom, GameReponseKey.NewRoom, true, false);
                    await UpdateOnlineStatus(connectingUser.Data);
                }
                else
                {
                    await Clients.Client(Context.ConnectionId).SendAsync(receiveMethod, connectingUser);
                }
            }
        }
        [HubMethodName("startRoom")]
        public async Task StartRoom(PlayerViewModel request)
        {
            request.ConnectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(request.UID))
            {
                var room = rooms.FirstOrDefault(r => r.RoomId == request.Room);
                if (room != null)
                {
                    room.StartDate = DateTime.UtcNow;
                    SendToGroup(room, GameReponseKey.StartRoom, room.RoomId);
                    SendToAll(room, GameReponseKey.RoomStarted);
                }
                else
                {
                    SendToClient(room, GameReponseKey.StartRoom, Context.ConnectionId, false);
                }
            }
        }

        [HubMethodName("joinRoom")]
        public async Task JoinRoom(PlayerViewModel request)
        {
            request.ConnectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(request.UID))
            {
                var expiredRooms = rooms.Where(r => r.StartDate.HasValue).ToList();
                expiredRooms.ForEach(r => rooms.Remove(r));

                var connectingUser = await PlayerViewModel.JoinRoomAsync_v2(request, rooms);
                if (connectingUser.IsSucceed)
                {
                    string ip = Context.Connection.RemoteIpAddress.ToString();
                    connectingUser.Data.IsOnline = true;
                    connectingUser.Data.Ip = ip;
                    var data = new ConnectViewModel()
                    {
                        Player = connectingUser.Data,
                        Others = PlayerViewModel.Repository.GetModelListBy(p => p.Uid != connectingUser.Data.UID
                                                                         && p.Room == connectingUser.Data.Room && !string.IsNullOrEmpty(p.ConnectionId)).Data
                    };
                    SendToClient(data, GameReponseKey.JoinRoom, Context.ConnectionId);
                    SendToGroup(data.Player, GameReponseKey.NewMember, data.Player.Room, false);
                    if (connectingUser.Data.CurrentRoom.Players.Count >= 4)
                    {
                        SendToAll(connectingUser.Data.CurrentRoom, GameReponseKey.RoomStarted);
                    }
                    await UpdateOnlineStatus(connectingUser.Data);
                }
                else
                {
                    await Clients.Client(Context.ConnectionId).SendAsync(receiveMethod, connectingUser);
                }
            }
        }

        [HubMethodName("getRooms")]
        public void GetRooms(PlayerViewModel request)
        {
            SendToClient(rooms.Where(r => !r.StartDate.HasValue && r.Players.Count > 0 && r.Players.Count < r.MaxMember && r.CreateDate.AddMinutes(5) < DateTime.UtcNow), GameReponseKey.AvailableRooms, Context.ConnectionId);
        }


        [HubMethodName("renderObj")]
        public void RenderObj(GameRequest<JObject> request)
        {
            SendToGroup(request);
        }

        [HubMethodName("renderArray")]
        public void RenderArray(GameRequest<JArray> request)
        {
            SendToGroup(request);
        }

        [HubMethodName("render")]
        public async Task RenderAsync(RenderObjectViewModel request)
        {
            SendToGroup(request);
            switch (request.ObjectType.ToLower())
            {
                case "player":
                    switch (request.Action.ToLower())
                    {
                        case "kill":
                            await Groups.RemoveAsync(Context.ConnectionId, request.Room);
                            break;
                        case "leaveroom":
                            var room = rooms.FirstOrDefault(r => r.HostId == request.Uid);
                            if (room != null)
                            {
                                SendToAll(room, GameReponseKey.RemoveRoom, true, false);
                                rooms.Remove(room);
                            }
                            await Groups.RemoveAsync(Context.ConnectionId, request.Room);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        [HubMethodName("shoot")]
        public void Shoot(RenderObjectViewModel request)
        {
            SendToGroup(request, GameReponseKey.Shoot, request.Room);
        }

        [HubMethodName("removeRoom")]
        public void RemoveRoom(string roomId)
        {
            var currentRoom = rooms.FirstOrDefault(r => r.RoomId == roomId);
            if (currentRoom != null)
            {
                SendToAll(currentRoom, GameReponseKey.RemoveRoom, true, false);
                rooms.Remove(currentRoom);
            }
        }

        [HubMethodName("addZoombie")]
        public void AddZoombie(RenderObjectViewModel request)
        {
            SendToGroup(request, GameAction.Add, GameObject.Zoombie);
        }

        [HubMethodName("killZoombie")]
        public void KillZoombie(RenderObjectViewModel request)
        {
            SendToGroup(request, GameAction.Kill, GameObject.Zoombie);
        }

        [HubMethodName("join")]
        public async Task Join(PlayerViewModel request)
        {
            string errorMsg = string.Empty;
            request.ConnectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(request.UID))
            {
                var expiredRooms = rooms.Where(r => r.TimeRemain < 300).ToList();
                expiredRooms.ForEach(r => rooms.Remove(r));

                var connectingUser = await PlayerViewModel.JoinGameAsync(request, rooms);
                if (connectingUser.IsSucceed)
                {
                    string ip = Context.Connection.RemoteIpAddress.ToString();
                    connectingUser.Data.IsOnline = true;
                    connectingUser.Data.Ip = ip;

                    var data = new ConnectViewModel()
                    {
                        Player = connectingUser.Data,
                        Others = PlayerViewModel.Repository.GetModelListBy(p => p.Uid != connectingUser.Data.UID
                                                                         && p.Room == connectingUser.Data.Room && !string.IsNullOrEmpty(p.ConnectionId)).Data
                    };
                    SendToClient(data, GameReponseKey.Connect, Context.ConnectionId);
                    await UpdateOnlineStatus(connectingUser.Data);
                }
                else
                {
                    await Clients.Client(Context.ConnectionId).SendAsync(receiveMethod, connectingUser);
                }
            }
        }

        public async Task UpdateOnlineStatus(PlayerViewModel player)
        {
            //SendToGroup(player, GameReponseKey.UpdateOnlineStatus, player.Room, false);
            if (player.IsOnline)
            {
                await Groups.AddAsync(Context.ConnectionId, player.Room);
            }
        }

        private void SendToGroup<T>(T message, GameReponseKey action, string groupName, bool isMySelf = true)
        {
            if (!string.IsNullOrEmpty(groupName))
            {

                RepositoryResponse<T> result = new RepositoryResponse<T>()
                {
                    IsSucceed = true,
                    Data = message,
                    ResponseKey = GetResponseKey(action)
                };

                if (isMySelf)
                {
                    Clients.Group(groupName).SendAsync(receiveMethod, result);
                }
                else
                {
                    Clients.OthersInGroup(groupName).SendAsync(receiveMethod, result);
                }
            }
        }

        private void SendToGroup(RenderObjectViewModel message, bool isMySelf = true)
        {
            RepositoryResponse<RenderObjectViewModel> result = new RepositoryResponse<RenderObjectViewModel>()
            {
                IsSucceed = true,
                Data = message,
                ResponseKey = message.Action
            };
            if (isMySelf)
            {
                Clients.Group(message.Room).SendAsync(receiveMethod, result);
            }
            else
            {
                Clients.OthersInGroup(message.Room).SendAsync(receiveMethod, result);
            }
        }


        private void SendToGroup<T>(GameRequest<T> message)
        {
            GameResponse<T> result = new GameResponse<T>()
            {
                IsSucceed = true,
                Data = message.Data,
                ResponseKey = message.Action
            };
            if (message.IsMySelf)
            {
                Clients.Group(message.Room).SendAsync(receiveMethod, result);
            }
            else
            {
                Clients.OthersInGroup(message.Room).SendAsync(receiveMethod, result);
            }
        }

        private void SendToGroup(List<RenderObjectViewModel> listMessage, GameAction action, string roomName, bool isSucceed = true, bool isMySelf = true)
        {
            GameResponse<List<RenderObjectViewModel>> result = new GameResponse<List<RenderObjectViewModel>>()
            {
                IsSucceed = isSucceed,
                Data = listMessage,
                ResponseKey = GetResponseKey(action)
            };
            if (string.IsNullOrEmpty(roomName))
            {

                listMessage.ForEach(message => message.Room = roomName);
            }
            if (isMySelf)
            {
                Clients.Group(roomName).SendAsync(receiveMethod, result);
            }
            else
            {
                Clients.OthersInGroup(roomName).SendAsync(receiveMethod, result);
            }
        }

        private void SendToGroup(RenderObjectViewModel message, GameAction action, GameObject obj, bool isMySelf = true)
        {
            message.Action = GetResponseKey(action);
            message.ObjectType = GetResponseKey(obj);
            RepositoryResponse<RenderObjectViewModel> result = new RepositoryResponse<RenderObjectViewModel>()
            {
                IsSucceed = true,
                Data = message,
                ResponseKey = message.Action
            };
            if (isMySelf)
            {
                Clients.Group(message.Room).SendAsync(receiveMethod, result);
            }
            else
            {
                Clients.OthersInGroup(message.Room).SendAsync(receiveMethod, result);
            }
        }

        private void SendToClient<T>(T message, GameReponseKey action, string connectionId, bool isSucceed = true)
        {
            RepositoryResponse<T> result = new RepositoryResponse<T>()
            {
                IsSucceed = isSucceed,
                Data = message,
                ResponseKey = GetResponseKey(action)
            };
            Clients.Client(connectionId).SendAsync(receiveMethod, result);
        }

        private void SendToAll<T>(T message, GameReponseKey action, bool isSucceed = true, bool isMyself = true)
        {
            RepositoryResponse<T> result = new RepositoryResponse<T>()
            {
                IsSucceed = isSucceed,
                Data = message,
                ResponseKey = GetResponseKey(action)
            };
            if (isMyself)
            {
                Clients.All.SendAsync(receiveMethod, result);
            }
            else
            {
                Clients.Others.SendAsync(receiveMethod, result);
            }

        }

        private string GetResponseKey<T>(T e)
        {
            return Enum.GetName(typeof(T), e);
        }

        #region Overrides
        public override async Task OnDisconnectedAsync(Exception exception)
        {

            var getUser = await PlayerViewModel.Repository.GetSingleModelAsync(
                u => u.ConnectionId == Context.ConnectionId);
            if (getUser.IsSucceed)
            {
                var user = getUser.Data;
                var currentRoom = rooms.FirstOrDefault(r => r.HostId == user.UID);
                if (currentRoom != null)
                {
                    currentRoom.Players.Remove(user);
                    if (currentRoom.HostId == user.UID)
                    {
                        SendToAll(currentRoom, GameReponseKey.RemoveRoom, true, false);
                        rooms.Remove(currentRoom);
                    }
                }
                user.IsOnline = false;
                user.ConnectionId = null;

                await user.RemoveModelAsync();
                await UpdateOnlineStatus(user);
                SendToGroup(user, GameReponseKey.RemoveMember, user.Room, false);
            }
            await base.OnDisconnectedAsync(exception);
        }
        public override Task OnConnectedAsync()
        {
            SendToClient(rooms.Where(r => !r.StartDate.HasValue && r.Players.Count > 0 && r.Players.Count < r.MaxMember && r.CreateDate.AddMinutes(5) < DateTime.UtcNow), GameReponseKey.GetRooms, Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        #endregion
    }
}
