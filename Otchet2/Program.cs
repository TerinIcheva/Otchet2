using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonFormatting = Newtonsoft.Json.Formatting;
using System.Collections.Generic;
using System.Text;

namespace PyrusApiClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var config = LoadConfig();
                if (config == null || string.IsNullOrWhiteSpace(config.Login) || string.IsNullOrWhiteSpace(config.SecurityKey))
                {
                    Console.WriteLine("Не удалось загрузить конфигурацию или отсутствуют логин/security_key.");
                    return;
                }

                Console.WriteLine("Выберите тип отчета:");
                Console.WriteLine("1 - Отчет за один день");
                Console.WriteLine("2 - Отчет за период");
                Console.Write("Ваш выбор: ");

                var choice = Console.ReadLine();
                DateTime startDate, endDate;
                string dateRangeFormatted;

                if (choice == "1")
                {
                    Console.Write("Введите дату (например, 18.06.2025): ");
                    if (!DateTime.TryParse(Console.ReadLine(), out startDate))
                    {
                        Console.WriteLine("Некорректный формат даты.");
                        return;
                    }
                    endDate = startDate;
                    dateRangeFormatted = startDate.ToString("dd.MM.yyyy");
                }
                else if (choice == "2")
                {
                    Console.Write("Введите начальную дату (например, 18.06.2025): ");
                    if (!DateTime.TryParse(Console.ReadLine(), out startDate))
                    {
                        Console.WriteLine("Некорректный формат даты.");
                        return;
                    }

                    Console.Write("Введите конечную дату (например, 24.06.2025): ");
                    if (!DateTime.TryParse(Console.ReadLine(), out endDate))
                    {
                        Console.WriteLine("Некорректный формат даты.");
                        return;
                    }

                    dateRangeFormatted = $"{startDate:dd.MM}-{endDate:dd.MM}.{startDate:yyyy}";
                }
                else
                {
                    Console.WriteLine("Некорректный выбор.");
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(config.ApiBaseUrl);
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // Аутентификация и получение токена
                    Console.WriteLine("Пытаюсь аутентифицироваться...");
                    var authToken = await GetAuthToken(client, config.Login, config.SecurityKey);
                    if (string.IsNullOrWhiteSpace(authToken))
                    {
                        Console.WriteLine("Не удалось получить токен аутентификации. Проверьте логин и security_key.");
                        return;
                    }

                    Console.WriteLine("Аутентификация успешна!");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                    // Шаг 1: Получение всех задач в Telegram
                    var telegramTasks = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        include_archived = "y",
                        created_after = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = endDate.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 2: Фильтрация задач по условиям
                    var filteredTasks = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld650 = "76365689,76365693,80222870,77549497,77549500",
                        fld651 = "76365689,76365693,80222870,77549497,77549500",
                        include_archived = "y",
                        created_after = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = endDate.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 3: Задачи, переданные на Бориса
                    var borisTasks = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        include_archived = "y",
                        created_after = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = endDate.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 4: Операторы отметили, что Борис не участвовал
                    var borisNotParticipated = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "4",
                        include_archived = "y",
                        created_after = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = endDate.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 5: Борис полноценно пообщался
                    var borisParticipated = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "1,2,3",
                        include_archived = "y",
                        created_after = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = endDate.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 6: Предложил точное решение
                    var borisSolved = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "1",
                        include_archived = "y",
                        created_after = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = endDate.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 7: Подсказал/помог
                    var borisHelped = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "2",
                        include_archived = "y",
                        created_after = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = endDate.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 8: Ответил неверно
                    var borisWrongAnswers = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "3",
                        include_archived = "y",
                        created_after = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = endDate.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Расчеты для аналитики
                    var notAssignedToBoris = filteredTasks - borisTasks;
                    var borisParticipationRate = borisTasks > 0 ? (double)borisParticipated / borisTasks * 100 : 0;
                    var effectivenessRate = borisParticipated > 0 ? (double)(borisSolved + borisHelped) / borisParticipated * 100 : 0;
                    var overallEffectiveness = telegramTasks > 0 ? (double)(borisSolved + borisHelped) / telegramTasks * 100 : 0;
                    var wrongAnswersRate = borisParticipated > 0 ? (double)borisWrongAnswers / borisParticipated * 100 : 0;
                    var notParticipatedRate = borisTasks > 0 ? (double)borisNotParticipated / borisTasks * 100 : 0;
                    var notAssignedRate = filteredTasks > 0 ? (double)notAssignedToBoris / filteredTasks * 100 : 0;

                    // Формирование тела отчета
                    var reportBody = new StringBuilder();
                    reportBody.AppendLine($"📊 Отчет за {dateRangeFormatted}");
                    reportBody.AppendLine("====================================");
                    reportBody.AppendLine("\n#Всего задач в телеграм: " + telegramTasks);
                    reportBody.AppendLine("#Подходящих под условия (Базовый договор, Cloud, ...): " + filteredTasks);
                    reportBody.AppendLine("#Передано на Бориса: " + borisTasks);
                    reportBody.AppendLine("!Не передано на Бориса: " + notAssignedToBoris);
                    reportBody.AppendLine("#Операторы отметили, что Борис не участвовал: " + borisNotParticipated);
                    reportBody.AppendLine("#Полноценно пообщался: " + borisParticipated + " задач");
                    reportBody.AppendLine("#Предложил решение: " + borisSolved);
                    reportBody.AppendLine("#Подсказал/помог: " + borisHelped);
                    reportBody.AppendLine("#Ответил неверно: " + borisWrongAnswers);

                    reportBody.AppendLine("\n🧾 Итоговая аналитика:");
                    reportBody.AppendLine("====================================");
                    reportBody.AppendLine($"Борису передано {Math.Round((double)borisTasks / telegramTasks * 100, 1)}% от всех задач ({borisTasks} из {telegramTasks})");
                    reportBody.AppendLine($"Из переданных задач он полноценно участвовал в {borisParticipated} задачах ({Math.Round(borisParticipationRate, 1)}% от полученных)");
                    reportBody.AppendLine($"Эффективность в полезных задачах — {Math.Round(effectivenessRate, 1)}% ({borisSolved + borisHelped} из {borisParticipated}), что составляет {Math.Round(overallEffectiveness, 1)}% от всех задач");
                    reportBody.AppendLine($"В {borisWrongAnswers} задачах дал ошибочные или нерелевантные ответы ({Math.Round(wrongAnswersRate, 1)}% среди задач с участием)");
                    reportBody.AppendLine($"По мнению операторов, не участвовал в {borisNotParticipated} задачах — это {Math.Round(notParticipatedRate, 1)}% от полученных");
                    reportBody.AppendLine($"Остались без назначения {notAssignedToBoris} задачи — это {Math.Round(notAssignedRate, 1)}% от подходящих");

                    // Вывод отчета в консоль
                    Console.WriteLine(reportBody.ToString());

                    // Создание задачи в Pyrus с отчетом
                    var taskRequest = new
                    {
                        form_id = 469817,
                        author = new { id = config.AuthorId },
                        fields = new List<object> // Явно указываем тип коллекции
                        {
                            new
                            {
                                id = 10,
                                type = "person",
                                name = "Ответственный",
                                value = new { id = config.ResponsibleId }
                            },
                            new
                            {
                                id = 1,
                                value = $"[Отчет работа Бориса] [{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}]"
                            },
                            new
                            {
                                id = 2,
                                value = reportBody.ToString()
                            },
                            new
                            {
                                id = 16,
                                value = new { choice_id = 1 }
                            },
                            new
                            {
                                id = 15,
                                value = new { choice_id = 1 }
                            },
                            new
                            {
                                id = 434,
                                value = new { choice_id = 1 }
                            },
                            new
                            {
                                id = 805,
                                value = new { choice_id = 1 }
                            },
                            new
                            {
                                id = 766,
                                value = new { choice_id = 1 }
                            }
                        }
                    };

                    var taskResponse = await CreateTask(client, taskRequest);
                    if (taskResponse != null && taskResponse.task != null)
                    {
                        Console.WriteLine($"\nЗадача успешно создана: {config.ApiBaseUrl}/#task/{taskResponse.task.id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
        static async Task<string> GetAuthToken(HttpClient client, string login, string securityKey)
        {
            try
            {
                var authRequest = new
                {
                    login = login,
                    security_key = securityKey
                };

                var jsonContent = JsonConvert.SerializeObject(authRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

              
                var response = await client.PostAsync("/v4/auth", content);


                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
                    return authResponse?.access_token ?? authResponse?.AccessToken;
                }

                Console.WriteLine($"Ошибка аутентификации. Код: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при аутентификации: {ex.Message}");
                return null;
            }
        }
        static async Task<int> GetTasksCount(HttpClient client, object requestBody)
        {
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/v4/forms/469817/register", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tasksResponse = JsonConvert.DeserializeObject<TasksResponse>(responseContent);
                return tasksResponse?.Tasks?.Count ?? 0;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка API: {errorContent}");
                return 0;
            }
        }

        static async Task<dynamic> CreateTask(HttpClient client, object requestBody)
        {
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/v4/tasks", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<dynamic>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при создании задачи: {errorContent}");
                return null;
            }
        }

        static Config LoadConfig()
        {
            try
            {
                var configPath = "config.json";
                if (!File.Exists(configPath))
                {
                    var defaultConfig = new Config
                    {
                        ApiBaseUrl = "https://api.pyrus.com",
                        Login = "terina.babicheva.02@mail.ru",
                        SecurityKey = "ydgGwWpSc8e30lnVbyzYfPc3Mx~px3CGr87mTg0YlgJ1m4NBPMfYxL2QDNOCwnxfR144t5IGmbPjcTJylawdKj93hGZyhza0",
                        AuthorId = 1223544,
                        ResponsibleId = 1223544
                    };
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
                    Console.WriteLine("Создан файл конфигурации. Заполните его своими данными.");
                    return null;
                }

                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки конфигурации: {ex.Message}");
                return null;
            }
        }
    }

    public class AuthResponse
    {
        public string access_token { get; set; }
        public string AccessToken { get; set; } // Для совместимости с разными версиями API
    }

    public class Config
    {
        public string ApiBaseUrl { get; set; } = "https://api.pyrus.com";
        public string Login { get; set; }
        public string SecurityKey { get; set; }
        public int AuthorId { get; set; }
        public int ResponsibleId { get; set; }
    }

    public class TasksResponse
    {
        public List<object> Tasks { get; set; }
    }
}