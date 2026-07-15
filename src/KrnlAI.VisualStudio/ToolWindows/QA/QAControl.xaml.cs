#pragma warning disable VSTHRD100
#pragma warning disable VSTHRD001

using System.Windows;
using System.Windows.Controls;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.ToolWindows.QA;

public sealed partial class QAControl : UserControl, IDisposable
{
    private readonly IBacklogService _backlog;
    private bool _disposed;

    public QAControl() : this(new BacklogService()) { }

    public QAControl(IBacklogService backlog)
    {
        InitializeComponent();
        _backlog = backlog;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await Browser.EnsureCoreWebView2Async();
        Browser.CoreWebView2.Settings.IsScriptEnabled = true;
        Browser.CoreWebView2.Settings.IsWebMessageEnabled = true;
        Browser.CoreWebView2.WebMessageReceived += OnWebMessage;
        await LoadHtmlAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }

    private System.Threading.Tasks.Task LoadHtmlAsync()
    {
        var html = GetHtml();
        Browser.NavigateToString(html);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private async void OnWebMessage(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.TryGetWebMessageAsString();
            var msg = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
            if (msg is null) return;

            if (!msg.TryGetValue("type", out var typeEl)) return;
            var type = typeEl.GetString();

            switch (type)
            {
                case "load":
                    await SendRunsAsync();
                    break;
                case "runTest":
                    await RunTestAsync(msg);
                    break;
                case "getEvidence":
                    await SendEvidenceAsync(msg);
                    break;
            }
        }
        catch (Exception ex)
        {
            await PostMessageAsync(new { type = "error", message = ex.Message });
        }
    }

    private async System.Threading.Tasks.Task SendRunsAsync()
    {
        var items = await _backlog.GetItemsAsync();
        if (items is null) return;

        var runs = items.Select(i => new
        {
            id = i.Id,
            targetType = "Backlog",
            status = i.Status,
            title = i.Title,
            evidence = Array.Empty<string>()
        }).ToList();

        await PostMessageAsync(new { type = "runs", runs });
    }

    private async System.Threading.Tasks.Task RunTestAsync(Dictionary<string, System.Text.Json.JsonElement> msg)
    {
        var targetType = GetString(msg, "targetType") ?? "Backlog";
        var title = GetString(msg, "title");

        var item = await _backlog.CreateItemAsync(
            title ?? $"QA Test - {targetType}",
            $"Automated QA test: {targetType}",
            "Medium", null);

        if (item is not null)
            await SendRunsAsync();
    }

    private async System.Threading.Tasks.Task SendEvidenceAsync(Dictionary<string, System.Text.Json.JsonElement> msg)
    {
        var id = GetString(msg, "id");
        if (id is null) return;

        var items = await _backlog.GetItemsAsync();
        var item = items?.FirstOrDefault(i => i.Id == id);

        await PostMessageAsync(new
        {
            type = "evidence",
            id,
            evidence = item is not null ? new[] { $"Item: {item.Title}", $"Status: {item.Status}", $"Priority: {item.Priority}" } : Array.Empty<string>()
        });
    }

    private static string? GetString(Dictionary<string, System.Text.Json.JsonElement> dict, string key)
    {
        return dict.TryGetValue(key, out var el) ? el.GetString() : null;
    }

    private System.Threading.Tasks.Task PostMessageAsync(object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        Browser.Dispatcher.Invoke(() =>
        {
            Browser.CoreWebView2?.PostWebMessageAsJson(json);
        });
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private string GetHtml()
    {
        return @"<!DOCTYPE html>
<html lang=""en""><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1.0"">
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:#1e1e1e;color:#ccc;padding:16px}
h2{margin:0 0 16px;font-weight:600;color:#fff}
.header{display:flex;justify-content:space-between;align-items:center;margin-bottom:16px}
button{background:#0078d4;color:#fff;border:none;padding:6px 12px;border-radius:4px;cursor:pointer}
button:hover{background:#106ebe}
button.secondary{background:#333;color:#ccc}
button.secondary:hover{background:#444}
.run{background:#2d2d2d;border:1px solid #444;border-radius:6px;padding:10px 14px;margin:0 0 8px}
.run .id{font-size:11px;opacity:.5;margin-bottom:2px}
.run .title{font-size:14px;font-weight:500;color:#fff;margin-bottom:4px}
.run .status{display:inline-block;padding:2px 8px;border-radius:4px;font-size:11px;font-weight:600}
.status-Pending{background:#666}.status-InProgress{background:#0078d4}
.status-Review{background:#ff8c00}.status-Done{background:#2ea043}
.status-Cancelled{background:#555}
.evidence{margin-top:8px;padding:8px;background:#1a1a1a;border-radius:4px;font-size:12px;font-family:monospace}
.modal-overlay{display:none;position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,.7);z-index:1000;justify-content:center;align-items:center}
.modal-overlay.open{display:flex}
.modal{background:#2d2d2d;border:1px solid #444;border-radius:8px;padding:20px;width:360px}
.modal h3{margin:0 0 12px;color:#fff}
.modal label{display:block;margin:8px 0 4px;font-size:12px;opacity:.7}
.modal input,.modal select{width:100%;padding:6px 8px;background:#1e1e1e;color:#ccc;border:1px solid #444;border-radius:4px;margin-bottom:8px}
.modal .actions{display:flex;gap:8px;justify-content:flex-end;margin-top:12px}
</style></head><body>
<div class=""header""><h2>🧪 QA Tests</h2><button onclick=""showCreate()"">+ New Test</button></div>
<div id=""runList""></div>
<div class=""modal-overlay"" id=""modal""><div class=""modal"">
<h3>New QA Test</h3>
<label>Target Type</label>
<select id=""targetType""><option>Smoke</option><option>Api</option><option>Frontend</option><option>Performance</option><option>Chaos</option></select>
<label>Title</label><input id=""testTitle"" placeholder=""Test description"">
<div class=""actions""><button class=""secondary"" onclick=""hideModal()"">Cancel</button><button onclick=""startTest()"">Run</button></div>
</div></div>
<script>
(function(){var vscode=window.chrome.webview;var runs=[];
function render(){var container=document.getElementById('runList');container.innerHTML='';
runs.forEach(function(r){var div=document.createElement('div');div.className='run';
div.innerHTML='<div class=""id"">'+escape(r.id)+'</div><div class=""title"">'+escape(r.title||r.targetType)+'</div>'+
'<span class=""status status-'+r.status+'"">'+r.status+'</span>'+
(r._evidence?'<div class=""evidence"">'+r._evidence+'</div>':'');
container.appendChild(div);});}
function showCreate(){document.getElementById('modal').className='modal-overlay open';}
function hideModal(){document.getElementById('modal').className='modal-overlay';}
function startTest(){var targetType=document.getElementById('targetType').value;
var title=document.getElementById('testTitle').value;
vscode.postMessage(JSON.stringify({type:'runTest',targetType:targetType,title:title}));hideModal();}
function escape(s){var d=document.createElement('div');d.textContent=s||'';return d.innerHTML;}
window.chrome.webview.addEventListener('message',function(e){var msg=JSON.parse(e.data);
if(msg.type==='runs'){runs=msg.runs;render();}
if(msg.type==='evidence'){for(var i=0;i<runs.length;i++){if(runs[i].id===msg.id){runs[i]._evidence=msg.evidence.join('\\n');render();break;}}}});
vscode.postMessage(JSON.stringify({type:'load'}));})();
</script></body></html>";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Browser?.Dispose();
        if (_backlog is IDisposable d) d.Dispose();
    }
}
