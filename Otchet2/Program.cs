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
                // Загрузка конфигурации
                var config = LoadConfig();

                if (config == null || string.IsNullOrWhiteSpace(config.AuthToken))
                {
                    Console.WriteLine("Не удалось загрузить конфигурацию или токен отсутствует.");
                    Console.WriteLine("Проверьте файл config.json и наличие токена авторизации.");
                    return;
                }

                // Запрос даты у пользователя
                Console.Write("Введите дату (например, 09.07.2025): ");
                var dateInput = Console.ReadLine();
                if (!DateTime.TryParse(dateInput, out DateTime reportDate))
                {
                    Console.WriteLine("Некорректный формат даты.");
                    return;
                }

                // Установка временных границ
                var createdAfter = reportDate.Date;
                var createdBefore = createdAfter.AddDays(1);
                var closedBefore = DateTime.UtcNow;

                // Создание HTTP клиента
                using (var client = new HttpClient())
                {
                    // Установка базового адреса API
                    client.BaseAddress = new Uri(config.ApiBaseUrl);

                    // Добавление заголовка авторизации с токеном
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.AuthToken}");
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // Шаг 1: Получение всех задач в Telegram
                    var telegramTasks = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        include_archived = "y",
                        created_after = createdAfter.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = createdBefore.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = closedBefore.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 2: Фильтрация задач по условиям
                    var filteredTasks = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld650 = "76365689,76365693,80222870,77549497,77549500",
                        fld651 = "76365689,76365693,80222870,77549497,77549500",
                        include_archived = "y",
                        created_after = createdAfter.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = createdBefore.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = closedBefore.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 3: Задачи, переданные на Бориса
                    var borisTasks = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        include_archived = "y",
                        created_after = createdAfter.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = createdBefore.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = closedBefore.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 4: Операторы отметили, что Борис не участвовал
                    var borisNotParticipated = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "4",
                        include_archived = "y",
                        created_after = createdAfter.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = createdBefore.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = closedBefore.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 5: Борис полноценно пообщался
                    var borisParticipated = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "1,2,3",
                        include_archived = "y",
                        created_after = createdAfter.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = createdBefore.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = closedBefore.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 6: Предложил точное решение
                    var borisSolved = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "1",
                        include_archived = "y",
                        created_after = createdAfter.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = createdBefore.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = closedBefore.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 7: Подсказал/помог
                    var borisHelped = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "2",
                        include_archived = "y",
                        created_after = createdAfter.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = createdBefore.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = closedBefore.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Шаг 8: Ответил неверно
                    var borisWrongAnswers = await GetTasksCount(client, new
                    {
                        field_ids = "",
                        fld434 = "5",
                        fld805 = "2,3,4,5",
                        fld822 = "3",
                        include_archived = "y",
                        created_after = createdAfter.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        created_before = createdBefore.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        closed_before = closedBefore.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });

                    // Расчеты для аналитики
                    var notAssignedToBoris = filteredTasks - borisTasks;
                    var borisParticipationRate = borisTasks > 0 ? (double)borisParticipated / borisTasks * 100 : 0;
                    var effectivenessRate = borisParticipated > 0 ? (double)(borisSolved + borisHelped) / borisParticipated * 100 : 0;
                    var overallEffectiveness = telegramTasks > 0 ? (double)(borisSolved + borisHelped) / telegramTasks * 100 : 0;
                    var wrongAnswersRate = borisParticipated > 0 ? (double)borisWrongAnswers / borisParticipated * 100 : 0;
                    var notParticipatedRate = borisTasks > 0 ? (double)borisNotParticipated / borisTasks * 100 : 0;
                    var notAssignedRate = filteredTasks > 0 ? (double)notAssignedToBoris / filteredTasks * 100 : 0;

                    // Вывод отчета
                    Console.WriteLine("\n#Всего задач в телеграм: " + telegramTasks);
                    Console.WriteLine("#Подходящих под условия (Базовый договор, Cloud, ...): " + filteredTasks);
                    Console.WriteLine("#Передано на Бориса: " + borisTasks);
                    Console.WriteLine("!Не передано на Бориса: " + notAssignedToBoris);
                    Console.WriteLine("#Операторы отметили, что Борис не участвовал: " + borisNotParticipated);
                    Console.WriteLine("#Полноценно пообщался: " + borisParticipated + " задач");
                    Console.WriteLine("#Предложил решение: " + borisSolved);
                    Console.WriteLine("#Подсказал/помог: " + borisHelped);
                    Console.WriteLine("#Ответил неверно: " + borisWrongAnswers);

                    Console.WriteLine("\n🧾 Итоговая аналитика:");
                    Console.WriteLine($"Борису передано {Math.Round((double)borisTasks / telegramTasks * 100, 1)}% от всех задач ({borisTasks} из {telegramTasks})");
                    Console.WriteLine($"Из переданных задач он полноценно участвовал в {borisParticipated} задачах ({Math.Round(borisParticipationRate, 1)}% от полученных)");
                    Console.WriteLine($"Эффективность в полезных задачах — {Math.Round(effectivenessRate, 1)}% ({borisSolved + borisHelped} из {borisParticipated}), что составляет {Math.Round(overallEffectiveness, 1)}% от всех задач");
                    Console.WriteLine($"В {borisWrongAnswers} задачах дал ошибочные или нерелевантные ответы ({Math.Round(wrongAnswersRate, 1)}% среди задач с участием)");
                    Console.WriteLine($"По мнению операторов, не участвовал в {borisNotParticipated} задачах — это {Math.Round(notParticipatedRate, 1)}% от полученных");
                    Console.WriteLine($"Остались без назначения {notAssignedToBoris} задачи — это {Math.Round(notAssignedRate, 1)}% от подходящих");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static async Task<int> GetTasksCount(HttpClient client, object requestBody)
        {
            try
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
                    var error = JsonConvert.DeserializeObject<ApiError>(errorContent);

                    Console.WriteLine($"Ошибка при запросе: {response.StatusCode} - {response.ReasonPhrase}");
                    Console.WriteLine($"Код ошибки: {error?.ErrorCode}");
                    Console.WriteLine($"Сообщение: {error?.Error}");

                    if (error?.ErrorCode == "access_denied_project")
                    {
                        Console.WriteLine("\n⚠️ Внимание: Нет доступа к указанной форме!");
                        Console.WriteLine("1. Проверьте правильность ID формы (469817)");
                        Console.WriteLine("2. Убедитесь, что ваш API-токен имеет права на эту форму");
                        Console.WriteLine("3. Обратитесь к администратору Pyrus для получения доступа");
                    }

                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при выполнении запроса: {ex.Message}");
                return 0;
            }
        }

        public class ApiError
        {
            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("error_code")]
            public string ErrorCode { get; set; }
        }
        static Config LoadConfig()
        {
            try
            {
                var configPath = "config.json";
                if (!File.Exists(configPath))
                {
                    var exampleConfig = new Config
                    {
                        ApiBaseUrl = "https://api.pyrus.com",
                        AuthToken = "your_auth_token_here"
                    };

                    File.WriteAllText(configPath, JsonConvert.SerializeObject(exampleConfig, JsonFormatting.Indented));
                    Console.WriteLine($"Создан пример конфигурационного файла: {configPath}");
                    Console.WriteLine("Пожалуйста, заполните его своими данными и запустите приложение снова.");
                    return null;
                }

                var json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<Config>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке конфигурации: {ex.Message}");
                return null;
            }
        }

        static string FormatJson(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, JsonFormatting.Indented);
            }
            catch
            {
                return json;
            }
        }
    }

    public class Config
    {
        public string ApiBaseUrl { get; set; }
        public string AuthToken { get; set; }
    }

    public class TasksResponse
    {
        public List<object> Tasks { get; set; }
    }
}