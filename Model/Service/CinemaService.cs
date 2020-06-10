using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Cinema.Model;
using Cinema.Model.Dictionary;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Cinema.Service
{
    public class CinemaService
    {

        private string _token;
        private string _baseUrl;
        private string _cinemaBaseUrl;

        private string _timeout;
        private string _update;
        private int _offset = 0;
        private string _chatId;

        readonly HttpClient client = new HttpClient();
        readonly DictionaryReceiveMessage dictionaryReceiveMessage = new DictionaryReceiveMessage();
        private readonly IConfiguration Configuration;

        public CinemaService(IConfiguration configuration)
        {
            Configuration = configuration;

            _token =  Settings.Secret;
            _baseUrl = "https://api.telegram.org/bot" + _token;
            _cinemaBaseUrl = "https://www.jcnet.com.br/cinema/index.html";
            _timeout = "?tileout=100";
        }

        public async Task InitAsync()
        {
            while (true)
            {
                _update = await GetUpdates(_baseUrl, _timeout, _offset);


                if (!string.IsNullOrWhiteSpace(_update))
                {
                    Response response = JsonConvert.DeserializeObject<Response>(_update);

                    if (response.result != null)
                    {
                        foreach (var result in response.result)
                        {
                            string text = result.message.text;
                            _chatId = result.message.from.id;
                            _offset = result.update_id;

                            if (!String.IsNullOrEmpty(text) && dictionaryReceiveMessage._messages.TryGetValue(text, out string value))
                            {
                                Type cinemaType = Assembly.GetExecutingAssembly().GetType(typeof(CinemaService).ToString());
                                MethodInfo invoke = cinemaType.GetMethod(value);
                                object[] parametersArray = new object[] { result };


                                try
                                {
                                    await (Task)invoke.Invoke(this, parametersArray);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }

                            }
                            else
                            {
                                await AjudaAsync(result);

                            }

                        }
                    }

                }

            }

        }

        public async Task<string> GetUpdates(string baseUrl, string timeout, int offsetSpecoal)
        {
            var method = "/getUpdates";
            var offsetMaster = offsetSpecoal + 1;
            var url = baseUrl + method + timeout + "&offset=" + offsetMaster;

            try
            {
                var response = "";
                response = await client.GetStringAsync(url);

                return response;

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }

        }

        public async Task AjudaAsync(Result result)
        {

            var text = $"Olá, {result.message.from.first_name} {result.message.from.last_name} vimos que você precisa de ajuda e vamos ajuda-lo."+
                       $"\nOs comandos para você utilizar nossas ferramentas são:"+
                       $"\n/cinema: Mostra todos os filmes em cartazes, com informações como: titulo, sinpose e detalhes sobre cada filme;" +
                       $"\n/horarios: Envia horarios de cada filme de cada cinema de bauru;" + 
                       $"\n/trailer: Mostra todos os filmes  e envia trailer de cada um;" +
                       $"\n/ajuda: Envia lista de comandos." + 
                       $"\nObrigado por usar o bot CinemasBauru";

            await sendMessage(_chatId, text);

        }

        public async Task CinemaAsync(Result result)
        {

            var request = await client.GetAsync(_cinemaBaseUrl);
            var response = await request.Content.ReadAsStreamAsync();

            var parser = new HtmlParser();

            var html = parser.ParseDocument(response);
            var blueListItemsLinq = html.All.Where(m => m.LocalName == "li" && m.ClassList.Contains("filme")).ToList();

            string cabecalho;

            foreach (var item in blueListItemsLinq)
            {
                var titulo = item.QuerySelector("h2").TextContent;

                var sinopse = item.GetElementsByClassName("sinopse").SingleOrDefault().TextContent;

                var detalhes = item.GetElementsByClassName("ficha").SingleOrDefault().TextContent;

                cabecalho = string.Empty;

                foreach (var img in item.QuerySelectorAll("img"))
                {
                    cabecalho = "<a href='" + img.GetAttribute("src") + "'>" + titulo + "</a>";
                }

                var text = cabecalho + "\n" + "\n" + sinopse + "\n" + detalhes;
                await sendMessage(_chatId, text);
            }

        }

        public async Task HorariosAsync(Result result)
        {

            var request = await client.GetAsync(_cinemaBaseUrl);
            var response = await request.Content.ReadAsStreamAsync();

            var parser = new HtmlParser();

            var html = parser.ParseDocument(response);
            var blueListItemsLinq = html.All.Where(m => m.LocalName == "li" && m.ClassList.Contains("filme")).ToList();

            string horario;
            string cabecalho;

            foreach (var item in blueListItemsLinq)
            {
                var titulo = item.QuerySelector("h2").TextContent;

                horario = string.Empty;

                foreach (var horarios in item.GetElementsByClassName("detalhes").ToList())
                {
                    horario += "\n" + horarios.TextContent.Replace("\n", "");
                }

                cabecalho = string.Empty;

                foreach (var img in item.QuerySelectorAll("img"))
                {
                    cabecalho = "<a href='" + img.GetAttribute("src") + "'>" + titulo + "</a>";
                }

                var text = cabecalho + "\n" + horario;
                await sendMessage(_chatId, text);
            }

        }

        public async Task TrailerAsync(Result result)
        {

            var request = await client.GetAsync(_cinemaBaseUrl);
            var response = await request.Content.ReadAsStreamAsync();

            var parser = new HtmlParser();

            var html = parser.ParseDocument(response);
            var blueListItemsLinq = html.All.Where(m => m.LocalName == "li" && m.ClassList.Contains("filme")).ToList();

            string trailer;

            foreach (var item in blueListItemsLinq)
            {
                var titulo = item.QuerySelector("h2").TextContent;

                trailer = "";

                foreach (var tr in item.QuerySelectorAll("iframe"))
                {
                    trailer = "<a href='" + tr.GetAttribute("src") + "'>" + titulo + "</a>";
                }

                var text = trailer;

                await sendMessage(_chatId, text);
            }

        }

        private async Task sendMessage(string chatId = "858079849", string textSend = "")
        {
            var method = "/sendMessage?";
            var url = _baseUrl + method + _timeout + "&chat_id=" + chatId + "&text=" + textSend + "&parse_mode=HTML&disable_web_page_preview=false";

            await client.GetStringAsync(url);
        }

        private async Task sendMessageReply(string chatId = "858079849", string textSend = "", string messageId = "")
        {
            var method = "/sendMessage?";
            var url = _baseUrl + method + _timeout + "&chat_id=" + chatId + "&text=" + textSend + $"&parse_mode=HTML&disable_web_page_preview=false&reply_to_msg_id={messageId}";

            await client.GetStringAsync(url);
        }
    }
}