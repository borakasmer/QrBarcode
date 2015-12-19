using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace CodersBarcode.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public string CreateBarcode(string connectionID)
        {
            string Code = "http://172.20.10.2:80/ControlPage/" + connectionID;
            string width = "150";
            string height = "150";
            var url = string.Format($"http://chart.apis.google.com/chart?cht=qr&chs={width}x{height}&chl={Code}");
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            System.Drawing.Image img = System.Drawing.Image.FromStream(stream);
            Guid imgName = Guid.NewGuid();
            img.Save(Server.MapPath("/QrBarcode/" + imgName + ".png"));
            return "<img src='/QrBarcode/" + imgName + ".png' width=" + width + " height=" + height + " alt='QrBarcode' class='barcode'/>æ"+imgName;
        }

        public void DeleteImage(string imgName)
        {
            DirectoryInfo directory = new DirectoryInfo(Server.MapPath("/QrBarcode"));
            directory.Empty(imgName);
        }

        public async Task MoveRobot(string command, string connectionID)
        {
            if (Session["hubproxy"] == null)
            {
                HubConnection hubConnection = new HubConnection("http://172.20.10.2:80/");
                IHubProxy hubproxy = hubConnection.CreateHubProxy("Product");
                Session.Add("hubproxy", hubproxy);
                await hubConnection.Start(new LongPollingTransport());
                hubproxy.Invoke("MoveRobot", command, connectionID);
            }
            else {
                ((IHubProxy)Session["hubproxy"]).Invoke("MoveRobot", command, connectionID);
            }
        }
        public async Task<ActionResult> ControlPage(string connectionID)
        {
            ViewBag.connectionID = connectionID;
            await MoveRobot("connected", connectionID);
            return View();
        }
    }
    public class Product : Hub
    {
        public override async Task OnConnected()
        {
            await Clients.Caller.getConnectionID(Context.ConnectionId);
        }

        public async Task MoveRobot(string command, string connectionID)
        {
            await Clients.Client(connectionID).MoveRobot(command);
        }
    }

    public static class Tool
    {
        public static void Empty(this DirectoryInfo directory, string imgName)
        {
            foreach(FileInfo file in directory.GetFiles())
            {
                if (file.Name.Contains(imgName))
                {
                    file.Delete();
                }
            }
        }
    }
}