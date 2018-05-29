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
    public class ShootThemAllHub : BaseSignalRHub
    {
        const string receiveMethod = "receiveMessage";
        private static List<RoomViewModel> rooms = new List<RoomViewModel>();

        public static List<RoomViewModel> Rooms { get => rooms; set => rooms = value; }

        [HubMethodName("updatePlayer")]
        public void UpdatePlayer(PlayerViewModel request)
        {
            SendToGroup(request, GameReponseKey.UpdatePlayer, request.Room, false);
        }

        [HubMethodName("joinGame")]
        public async Task JoinGame(PlayerViewModel request)
        {
            if (!string.IsNullOrEmpty(request.UID))
            {
                request.ConnectionId = Context.ConnectionId;
                var connectingUser = await PlayerViewModel.Repository.GetSingleModelAsync(p => p.Uid == request.UID);
                if (!connectingUser.IsSucceed)
                {
                    request.IsOnline = true;
                    request.JoinedDate = DateTime.UtcNow;
                    connectingUser = await request.SaveModelAsync();
                    SendToAll(connectingUser.Data, GameReponseKey.JoinGame);
                    await UpdateOnlineStatus(connectingUser.Data);
                }
                else
                {
                    connectingUser.Data.IsOnline = true;
                    connectingUser.Data.ConnectionId = Context.ConnectionId;
                    SendToAll(connectingUser.Data, GameReponseKey.JoinGame);
                    await UpdateOnlineStatus(connectingUser.Data);
                }
                SendToClient(Rooms.Where(r => !r.StartDate.HasValue), GameReponseKey.GetRooms, Context.ConnectionId);
            }
        }

        [HubMethodName("createRoom")]
        public async Task CreateRoom(RoomViewModel request)
        {
            request.HostConnectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(request.HostId))
            {
                var connectingUser = await PlayerViewModel.CreateRoomAsync(request, Rooms);
                if (connectingUser.IsSucceed)
                {
                    connectingUser.Data.IsOnline = true;
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
        public void StartRoom(PlayerViewModel request)
        {
            request.ConnectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(request.UID))
            {
                var room = Rooms.FirstOrDefault(r => r.RoomId == request.Room);
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

        [HubMethodName("joinRoom")]
        public async Task JoinRoom(PlayerViewModel request)
        {
            request.ConnectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(request.UID))
            {
                var expiredRooms = Rooms.Where(r => r.StartDate.HasValue).ToList();
                expiredRooms.ForEach(r => Rooms.Remove(r));

                var connectingUser = await PlayerViewModel.JoinRoomAsync_v2(request, Rooms);
                if (connectingUser.IsSucceed)
                {
                    connectingUser.Data.IsOnline = true;
                    var data = new ConnectViewModel()
                    {
                        Player = connectingUser.Data,
                        Others = PlayerViewModel.Repository.GetModelListBy(p => p.Uid != connectingUser.Data.UID
                                                                         && p.Room == connectingUser.Data.Room && !string.IsNullOrEmpty(p.ConnectionId)).Data
                    };
                    SendToClient(data, GameReponseKey.JoinRoom, Context.ConnectionId);
                    SendToAll(data.Player, GameReponseKey.NewMember, true, false);
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
            SendToClient(Rooms.Where(r => !r.StartDate.HasValue), GameReponseKey.GetRooms, Context.ConnectionId);
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
        public void Render(RenderObjectViewModel request)
        {
            SendToGroup(request);
        }

        [HubMethodName("shoot")]
        public void Shoot(RenderObjectViewModel request)
        {
            SendToGroup(request, GameReponseKey.Shoot, request.Room);
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
            request.ConnectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(request.UID))
            {
                var expiredRooms = Rooms.Where(r => r.TimeRemain < 300).ToList();
                expiredRooms.ForEach(r => Rooms.Remove(r));

                var connectingUser = await PlayerViewModel.JoinGameAsync(request, Rooms);
                if (connectingUser.IsSucceed)
                {
                    connectingUser.Data.IsOnline = true;
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
            if (player.IsOnline && !string.IsNullOrEmpty(player.Room))
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
                var currentRoom = Rooms.FirstOrDefault(r => r.HostId == user.UID);
                if (currentRoom != null)
                {
                    currentRoom.Players.Remove(user);
                    if (currentRoom.HostId == user.UID)
                    {
                        SendToAll(currentRoom, GameReponseKey.RemoveRoom, true, false);
                        Rooms.Remove(currentRoom);
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
            //SendToClient(Rooms.Where(r => !r.StartDate.HasValue), GameReponseKey.GetRooms, Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        #endregion
    }
}
