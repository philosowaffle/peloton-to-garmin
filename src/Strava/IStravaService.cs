using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using P2G.Strava.Dto;

namespace P2G.Strava
{
    public interface IStravaService
    {
        /// <summary>
        /// Получает список последних активностей атлета
        /// </summary>
        /// <param name="numActivities">Количество активностей для загрузки</param>
        /// <param name="afterDate">Дата после которой загружать активности (для инкрементальной синхронизации)</param>
        /// <returns>Список активностей</returns>
        Task<List<StravaActivity>> GetRecentActivitiesAsync(int numActivities, DateTime? afterDate = null);

        /// <summary>
        /// Получает детальную информацию об активности включая потоки данных
        /// </summary>
        /// <param name="activityId">ID активности</param>
        /// <returns>Детали активности с потоками данных</returns>
        Task<StravaActivityWithStreams> GetActivityDetailsAsync(long activityId);

        /// <summary>
        /// Получает данные текущего атлета
        /// </summary>
        /// <returns>Данные атлета</returns>
        Task<StravaAthlete> GetAthleteDataAsync();

        /// <summary>
        /// Проверяет валидность токена доступа
        /// </summary>
        /// <returns>True если токен валиден</returns>
        Task<bool> IsAuthenticatedAsync();
    }
}
