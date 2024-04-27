using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics.Eventing.Reader;

public class OrderRequest
{
    public string name { get; set; }
    public string email { get; set; }
    public string tel { get; set; }
    public string printername { get; set; }
    public string description { get; set; }
}


class Program
{

    public static OrderRequest request = new OrderRequest();
    public static string Await = "none";
    // Это клиент для работы с Telegram Bot API, который позволяет отправлять сообщения, управлять ботом, подписываться на обновления и многое другое.
    private static ITelegramBotClient _botClient;

    // Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
    private static ReceiverOptions _receiverOptions;

    static async Task Main()
    {

        _botClient = new TelegramBotClient("6898158089:AAHNLqSFDF8XkWSAl1D0qNhrqbck2lWAjrE"); // Присваиваем нашей переменной значение, в параметре передаем Token, полученный от BotFather
        _receiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
        {
            AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
            {
                UpdateType.Message, // Сообщения (текст, фото/видео, голосовые/видео сообщения и т.д.)
                UpdateType.CallbackQuery
            },
            // Параметр, отвечающий за обработку сообщений, пришедших за то время, когда ваш бот был оффлайн
            // True - не обрабатывать, False (стоит по умолчанию) - обрабаывать
            ThrowPendingUpdates = true,
        };

        using var cts = new CancellationTokenSource();

        // UpdateHander - обработчик приходящих Update`ов
        // ErrorHandler - обработчик ошибок, связанных с Bot API
        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token); // Запускаем бота
        
        var me = await _botClient.GetMeAsync(); // Создаем переменную, в которую помещаем информацию о нашем боте.
        Console.WriteLine($"{me.FirstName} запущен!");

        await Task.Delay(-1); // Устанавливаем бесконечную задержку, чтобы наш бот работал постоянно
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {

        List<string> printernames = new List<string>()
        {
            "Helidorus",
            "Helidorus1",
            "Helidorus2",
            "Helidorus3",
            "Helidorus4",
            "Helidorus5"
        };

        // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
        try
        {
            // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        // эта переменная будет содержать в себе все связанное с сообщениями
                        var message = update.Message;

                        // From - это от кого пришло сообщение (или любой другой Update)
                        var user = message.From;

                        // Выводим на экран то, что пишут нашему боту, а также небольшую информацию об отправителе
                        Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");

                        // Chat - содержит всю информацию о чате
                        var chat = message.Chat;

                        if (message.Text == "/start")
                        {
                            List<KeyboardButton[]> kbList = MakeButtonsFromList(printernames);
                            Console.WriteLine(kbList.Count);

                            List<InlineKeyboardButton[]> kbList1 = MakeInlineButtonsFromList(printernames);

                            /*var replyKeyboard = new ReplyKeyboardMarkup(kbList) { ResizeKeyboard = true, };
                            await botClient.SendTextMessageAsync(chat.Id, "Выберите принтер", replyMarkup: replyKeyboard);*/

                            var inlineKeyboard = new InlineKeyboardMarkup(kbList1);
                            await botClient.SendTextMessageAsync(chat.Id, $"Выберите принтер", replyMarkup: inlineKeyboard);

                            return; 
                        };

                        if(Await == "none")
                        {
                            if (printernames.Contains(message.Text) & Await == "none")
                            {
                                request = new OrderRequest();
                                request.name = $"{user.FirstName} {user.LastName}";
                                request.printername = message.Text;
                                await botClient.SendTextMessageAsync(chat.Id, "Напишите свой EMail");
                                Await = "email";
                                return;
                            }
                            else
                            {
                                List<KeyboardButton[]> kbList = MakeButtonsFromList(printernames);

                                var replyKeyboard = new ReplyKeyboardMarkup(kbList) { ResizeKeyboard = true, };

                                await botClient.SendTextMessageAsync(chat.Id, "Пожалуйста, выберите принтер", replyMarkup: replyKeyboard);
                                return;
                            }
                        }
                        if(Await == "email")
                        {
                            if (EmailCheck(message.Text))
                            {
                                request.email = message.Text;
                                await botClient.SendTextMessageAsync(chat.Id, "Введите свой номер телефона");
                                Await = "tel";
                                return;
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chat.Id, "Пожалуйста, введите свой Email");
                                return;
                            }
                        }
                        if(Await == "tel")
                        {
                            if (TGCheck(message.Text))
                            {
                                request.tel = message.Text;
                                await botClient.SendTextMessageAsync(chat.Id, "Напишите комментарий к заказу");
                                Await = "description";
                                return;
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chat.Id, "Пожалуйста, введите свой номер телефона");
                                return;
                            }
                        }
                        if(Await == "description")
                        {
                            request.description = message.Text;

                            Await = "EndOrder";

                        }
                        if(Await == "EndOrder" || Await.Contains("CH"))
                        {
                            if(Await.Contains("CH") & Await.Contains("name"))
                            {
                                request.name = message.Text;
                            }
                            if (Await.Contains("CH") & Await.Contains("printer"))
                            {
                                if (printernames.Contains(message.Text))
                                {
                                    request.email = message.Text;
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chat.Id, $"Принтер не найден.");
                                }
                            }
                            if (Await.Contains("CH") & Await.Contains("email"))
                            {
                                if (EmailCheck(message.Text))
                                {
                                    request.email = message.Text;
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chat.Id, $"Email введен неккоректно.");
                                }
                            }
                            if (Await.Contains("CH") & Await.Contains("tel"))
                            {
                                if (TGCheck(message.Text))
                                {
                                    request.tel = message.Text;
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chat.Id, $"Телефонный номер введен неккоректно.");
                                }
                            }
                            if (Await.Contains("CH") & Await.Contains("description"))
                            {
                                request.description = message.Text;
                            }
                            await botClient.SendTextMessageAsync(chat.Id, $"предзаказ со следующими данными будет оформлен : \nПринтер: {request.printername} \nИмя: {request.name} \nEmail: {request.email}\nТелефон: {request.tel}\nКомментарий: {request.description}");

                            List<InlineKeyboardButton[]> kbList = new List<InlineKeyboardButton[]>()
                            {
                                new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Вся информация верна, оформляйте предзаказ!", "done"),
                                        },
                                new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Имя", "name"),
                                            InlineKeyboardButton.WithCallbackData("Принтер", "print"),
                                        },
                                new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Email", "email"),
                                            InlineKeyboardButton.WithCallbackData("Телефон", "num"),
                                        },
                                new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Коментарий к заказу", "comm"),
                                        },
                            };
                            var inlineKeyboard = new InlineKeyboardMarkup(kbList);
                            await botClient.SendTextMessageAsync(chat.Id, $"Если данные оказались не верны, вы можете изменить введеные данные", replyMarkup: inlineKeyboard);
                            return;
                        }

                        return;
                    }
                case UpdateType.CallbackQuery:
                {
                    // Переменная, которая будет содержать в себе всю информацию о кнопке, которую нажали
                    var callbackQuery = update.CallbackQuery;

                    // Аналогично и с Message мы можем получить информацию о чате, о пользователе и т.д.
                    var user = callbackQuery.From;

                    // Выводим на экран нажатие кнопки
                    Console.WriteLine($"{user.FirstName} ({user.Id}) нажал на кнопку: {callbackQuery.Data}");

                    // Вот тут нужно уже быть немножко внимательным и не путаться!
                    // Мы пишем не callbackQuery.Chat , а callbackQuery.Message.Chat , так как
                    // кнопка привязана к сообщению, то мы берем информацию от сообщения.
                    var chat = callbackQuery.Message.Chat;

                    // Добавляем блок switch для проверки кнопок
                    switch (callbackQuery.Data)
                    {
                            case "done":
                                {
                                    MakeOrder(request);
                                    request = new OrderRequest();

                                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Предзаказ оформлен!", showAlert: true);
                                    await botClient.SendTextMessageAsync(chat.Id, $"Если вы хотите предзаказать еще один принтер, напишите функцию /start");
                                    Await = "none";
                                    return;
                                }

                            case "name":
                                {
                                    await botClient.SendTextMessageAsync(chat.Id, "Напишите корректное имя");
                                    Await = "CHname";
                                    return;
                                }

                            case "printer":
                                {

                                    List<KeyboardButton[]> kbList = MakeButtonsFromList(printernames);

                                    var replyKeyboard = new ReplyKeyboardMarkup(kbList) { ResizeKeyboard = true, };

                                    await botClient.SendTextMessageAsync(chat.Id, "Пожалуйста, выберите принтер", replyMarkup: replyKeyboard);
                                    Await = "CHprinter";
                                    return;
                                }

                            case "email":
                                {
                                    await botClient.SendTextMessageAsync(chat.Id, "Напишите корректный Email");
                                    Await = "CHemail";
                                    return;
                                }
                            case "num":
                                {
                                    await botClient.SendTextMessageAsync(chat.Id, "Напишите корректный номер телефона");
                                    Await = "CHtel";
                                    return;
                                }

                            case "comm":
                                {
                                    await botClient.SendTextMessageAsync(chat.Id, "Напишите нужный вам комментарий");
                                    Await = "CHdescription";
                                    return;
                                }
                            default:
                                {
                                    if (printernames.Contains(callbackQuery.Data))
                                    {
                                        request = new OrderRequest();
                                        request.name = $"{user.FirstName} {user.LastName}";
                                        request.printername = callbackQuery.Data;
                                        await botClient.SendTextMessageAsync(chat.Id, "Напишите свой EMail");
                                        Await = "email";
                                    }
                                    return;
                                }
                        }
                    return;
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        // Тут создадим переменную, в которую поместим код ошибки и её сообщение 
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
    public static void MakeOrder(OrderRequest r)
    {
        string url = "https://neopixel3d.ru/api/orders";

        using (var wb = new WebClient())
        {
            string data = JsonSerializer.Serialize(r);
            var response = wb.UploadString(url, "POST", data);

            Console.WriteLine(response);

        }

    }

    public static List<KeyboardButton[]> MakeButtonsFromList(List<string> names)
    {
        List<KeyboardButton[]> kbList = new List<KeyboardButton[]>();
        int pCnt = names.Count;
        while (pCnt != 0)
        {
            if (pCnt >= 3)
            {
                kbList.Add(new KeyboardButton[]
                {
                    new KeyboardButton(names[names.Count - pCnt]),
                    new KeyboardButton(names[names.Count - pCnt + 1]),
                    new KeyboardButton(names[names.Count - pCnt] + 2),
                });
                pCnt -= 3;
            }
            else
            {
                KeyboardButton[] kbLast = new KeyboardButton[pCnt];
                for (int i = pCnt; i > 0; i--)
                {
                    kbLast[pCnt - i] = new KeyboardButton(names[names.Count - i]);
                }
                kbList.Add(kbLast);
                pCnt = 0;
            }
        }
        return kbList;
    }
    public static List<InlineKeyboardButton[]> MakeInlineButtonsFromList(List<string> names)
    {
        List<InlineKeyboardButton[]> kbList = new List<InlineKeyboardButton[]>();
        int pCnt = names.Count;
        while (pCnt != 0)
        {
            if (pCnt >= 3)
            {
                kbList.Add(new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData(names[names.Count - pCnt], names[names.Count - pCnt]),
                    InlineKeyboardButton.WithCallbackData(names[names.Count - pCnt + 1], names[names.Count - pCnt + 1]),
                    InlineKeyboardButton.WithCallbackData(names[names.Count - pCnt] + 2, names[names.Count - pCnt] + 2),
                });
                pCnt -= 3;
            }
            else
            {
                InlineKeyboardButton[] kbLast = new InlineKeyboardButton[pCnt];
                for (int i = pCnt; i > 0; i--)
                {
                    kbLast[pCnt - i] = InlineKeyboardButton.WithCallbackData(names[names.Count - i], names[names.Count - i]);
                }
                kbList.Add(kbLast);
                pCnt = 0;
            }
        }
        return kbList;
    }

    // TODO
    public static bool EmailCheck(string text)
    {
        Regex regex = new Regex(@"(\w*)@(\w*).(\w*)");

        return regex.IsMatch(text);
    }

    public static bool TGCheck(string text)
    {
        int outing;

        bool plusFirst = text.StartsWith("+");
        bool CountAcess = false;
        Console.WriteLine(text.Count());
        if (plusFirst)
        {
            CountAcess = text.Count() <= 12;
        }
        else
        {
            CountAcess = text.Count() <= 11;
        }

        return CountAcess & (plusFirst || int.TryParse(text[0].ToString(), out outing));
    }


}