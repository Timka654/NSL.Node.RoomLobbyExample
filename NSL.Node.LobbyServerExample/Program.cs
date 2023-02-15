using NSL.BuilderExtensions.SocketCore;
using NSL.BuilderExtensions.WebSocketsServer.AspNet;
using NSL.Node.BridgeLobbyClient.AspNetCore;
using NSL.Node.BridgeServer;
using NSL.Node.BridgeServer.RS;
using NSL.Node.LobbyServerExample.Managers;
using NSL.Node.LobbyServerExample.Shared.Models;
using NSL.Node.RoomServer;

namespace NSL.Node.LobbyServerExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            BridgeServerStartupEntry.CreateDefault().Run();
            RoomServerStartupEntry.CreateDefault().Run();

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<LobbyManager>();

            builder.Services.AddBridgeLobbyClient(
                builder.Configuration.GetValue<string>("bridge:server:url"),
                builder.Configuration.GetValue<string>("bridge:server:identity"),
                builder.Configuration.GetValue<string>("bridge:server:key"),
                (services, builder) => {

                });

            var app = builder.Build();

            app.UseWebSockets();

            app.MapWebSocketsPoint<LobbyNetworkClientModel>("/lobby_ws", builder =>
            {
                builder.AddExceptionHandle((ex, c) =>
                {
                    Console.WriteLine($"Exception {Environment.NewLine}{ex}{Environment.NewLine} from client");
                });

                builder.AddReceiveHandle((client, pid, len) =>
                {
                    Console.WriteLine($"receive pid : {pid} from {client.GetRemotePoint()}");
                });

                builder.AddSendHandle((client, pid, len,stack) =>
                {
                    Console.WriteLine($"send pid : {pid} to {client.GetRemotePoint()}");
                });

                app.Services.GetRequiredService<LobbyManager>().BuildNetwork(builder);
            });

            app.UseRouting();

            app.RunBridgeLobbyClient(app.Services.GetRequiredService<LobbyManager>().BridgeValidateSessionAsync);

            app.Run();
        }
    }
}