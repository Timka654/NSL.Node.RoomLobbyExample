using NSL.Node.LobbyServerExample.Shared.Enums;
using NSL.SocketCore.Utils;
using NSL.SocketCore.Utils.Buffer;
using NSL.WebSockets.Server.AspNetPoint;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace NSL.Node.LobbyServerExample.Shared.Models
{
    public class LobbyRoomInfoModel : BaseLobbyRoomModel
    {
        public string Password { get; set; }

        public bool PasswordEnabled => !string.IsNullOrWhiteSpace(Password);

        public int MaxMembers { get; set; }

        public Guid OwnerId { get; set; }

        public LobbyRoomState State { get; set; }

        private ConcurrentDictionary<Guid, LobbyRoomMemberModel> members = new ConcurrentDictionary<Guid, LobbyRoomMemberModel>();

        public JoinResultEnum JoinMember(LobbyNetworkClientModel client)
        {
            var member = new LobbyRoomMemberModel()
            {
                Client = client
            };

            if (members.ContainsKey(member.Client.UID)) // already exists
                return JoinResultEnum.Ok;

            if (State != LobbyRoomState.Lobby)
                return JoinResultEnum.NotFound;

            if (members.Count == MaxMembers)
                return JoinResultEnum.MaxMemberCount;

            members.TryAdd(member.Client.UID, member);

            client.CurrentRoom = this;

            BroadcastJoinMember(member);

            return JoinResultEnum.Ok;
        }

        public void LeaveMember(LobbyNetworkClientModel client)
        {
            if (members.TryRemove(client.UID, out var member))
            {
                client.CurrentRoom = default;
                BroadcastLeaveMember(member);
            }

            if (client.UID.Equals(OwnerId))
            {
                RemoveRoom();

                return;
            }
        }

        public void StartRoom(IConfiguration configuration)
        {
            State = LobbyRoomState.Processing;

            BroadcastStartRoom(configuration);
        }

        public void RemoveRoom()
        {
            foreach (var memberID in members.Keys.ToArray())
            {
                if (members.TryRemove(memberID, out var member))
                {
                    LeaveMember(member.Client);
                }
            }

            BroadcastRemoveRoom();
        }

        internal void SendChatMessage(LobbyNetworkClientModel client, string v)
        {
            BroadcastChatMessage(client, v);
        }

        public bool ExistsMember(Guid uid)
        {
            return members.ContainsKey(uid);
        }

        public int MemberCount()
            => members.Count;

        public IEnumerable<LobbyRoomMemberModel> GetMembers()
            => members.Values;

        #region Broadcast

        private void BroadcastStartRoom(IConfiguration configuration)
        {
            foreach (var item in members)
            {
                var packet = OutputPacketBuffer.Create(ClientReceivePacketEnum.RoomStartedMessage);

                packet.WriteGuid(Id);
                packet.WriteString16($"{item.Value.Client.UID}"); // split data with ':' char
                packet.WriteString16(configuration.GetValue<string>("bridge:server:identity"));
                packet.WriteCollection(
                    Enumerable.Repeat(configuration.GetValue<string>("bridge:server:clients_endpoint"), 1),
                    item => packet.WriteString16(item));
                packet.WriteInt32(members.Count);

                item.Value.Client.Network.Send(packet);
            }
        }

        private void BroadcastChatMessage(LobbyNetworkClientModel client, string text)
        {
            var packet = OutputPacketBuffer.Create(ClientReceivePacketEnum.ChatMessage);

            packet.WriteGuid(client.UID);
            packet.WriteString16(text);

            BroadcastMessage(packet);
        }

        private void BroadcastJoinMember(LobbyRoomMemberModel member)
        {
            var packet = OutputPacketBuffer.Create(ClientReceivePacketEnum.RoomMemberJoinMessage);

            packet.WriteGuid(member.Client.UID);

            BroadcastMessage(packet);
        }

        private void BroadcastLeaveMember(LobbyRoomMemberModel member)
        {
            var packet = OutputPacketBuffer.Create(ClientReceivePacketEnum.RoomMemberLeaveMessage);

            packet.WriteGuid(member.Client.UID);

            BroadcastMessage(packet);
        }

        private void BroadcastRemoveRoom()
        {
            var packet = OutputPacketBuffer.Create(ClientReceivePacketEnum.RoomRemoveMessage);

            packet.WriteGuid(Id);

            BroadcastMessage(packet);
        }

        private async void BroadcastMessage(OutputPacketBuffer packet)
        {
            await Task.Run(() =>
            {
                foreach (var item in members)
                {
                    item.Value.Client.Network.Send(packet, false);
                }

                packet.Dispose();
            });
        }

        #endregion
    }

    public class LobbyRoomMemberModel
    {
        public LobbyNetworkClientModel Client { get; set; }
    }

    public enum LobbyRoomState
    {
        Lobby,
        Processing,
        Runned
    }
}
