namespace NSL.Node.LobbyServerExample.Shared.Enums
{
    public enum ClientReceivePacketEnum : ushort
    {
        Response = 1,
        NewRoomMessage,
        ChangeTitleRoomInfoMessage,
        ChangeRoomInfo,
        RoomMemberJoinMessage,
        RoomMemberLeaveMessage,
        ChatMessage,
        NewUserIdentity,
        RoomStartedMessage,
        RoomRemoveMessage
    }
}
