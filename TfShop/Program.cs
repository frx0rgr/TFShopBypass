using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;


static IEnumerable<string> GetCurrentComputerAddressesOrHosts()
{
    yield return Dns.GetHostName();

    foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
    {
        if (networkInterface.OperationalStatus != OperationalStatus.Up)
            continue;
        foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
        {
            var address = ip.Address;
            if (address.AddressFamily == AddressFamily.InterNetwork /*|| address.AddressFamily == AddressFamily.InterNetworkV6*/)
            {
                yield return address.ToString();
            }
        }
    }
}
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseRouting();

app.MapControllers();

var version = Assembly.GetExecutingAssembly().GetName().Version!;

app.Logger.LogInformation($"Welcome to TFShop Bypass v{version.Major}.{version.Minor}.{version.Build}");

var ipAddress = string.Join("", GetCurrentComputerAddressesOrHosts().Select(loadingError => $"{Environment.NewLine}{loadingError}")); 
app.Logger.LogInformation($"Server Host/IP:{ipAddress}");
app.Run();

app.Logger.LogInformation("sadsd");

