using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
namespace CremaTools;

public static class UpdateChecks
{
    sealed class FakeHandler(string manifest) : HttpMessageHandler { public bool Corrupt; protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel) { var data = request.RequestUri!.AbsolutePath.EndsWith(".json") ? Encoding.UTF8.GetBytes(manifest) : Encoding.UTF8.GetBytes(Corrupt ? "BAD DOWNLOAD" : "NOT A REAL INSTALLER - CREMATOOLS SELF TEST"); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(data) }); } }
    public static async Task Run()
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("CremaTools.UpdateTestFixture.json")!; using var reader = new StreamReader(resource); string valid = reader.ReadToEnd(); var payload = UpdateEngine.Verify(valid); if (payload.Version != "2.0.0") throw new Exception("Update signature verification failed."); var envelope = JsonSerializer.Deserialize<SignedUpdate>(valid)!; var bytes = Convert.FromBase64String(envelope.Payload); bytes[5] ^= 1; var tampered = JsonSerializer.Serialize(new SignedUpdate(Convert.ToBase64String(bytes), envelope.Signature)); bool rejected = false; try { UpdateEngine.Verify(tampered); } catch { rejected = true; }
        if (!rejected) throw new Exception("Tampered update manifest accepted.");
        var fake = new FakeHandler(valid); UpdateEngine.Http = new HttpClient(fake); UpdateEngine.Settings = new ToolSettings { UpdateUrl = "https://test.invalid/stable.json", AutoUpdate = false }; var next = await UpdateEngine.Check(default); if (next is null) throw new Exception("New update not detected."); fake.Corrupt = true; rejected = false; try { await UpdateEngine.Download(next, new Progress<string>(), default); } catch { rejected = true; }
        if (!rejected) throw new Exception("Corrupt update download accepted."); fake.Corrupt = false; await UpdateEngine.Download(next, new Progress<string>(), default); if (!UpdateEngine.Ready || !File.ReadAllText(UpdateEngine.Settings.PendingInstaller).StartsWith("NOT A REAL")) throw new Exception("Valid signed update was not staged."); UpdateEngine.Settings.PendingInstaller = ""; UpdateEngine.Save();
    }
}
