namespace NSL.Node.LobbyServerExample.Shared.Enums
{
    public enum ClientReceivePacketEnum : ushort
    {
        CreateRoomResult = 1,
        NewRoomMessage,
        ChangeTitleRoomInfo,
        ChangeRoomInfo,
        RoomMemberJoinMessage,
        RoomMemberLeaveMessage,
        JoinRoomResult,
        LeaveRoomResult,
        ChatMessage,
        NewUserIdentity,
        RoomStartedMessage,
        RoomRemoveMessage,
        GetRoomListResult
    }
}
