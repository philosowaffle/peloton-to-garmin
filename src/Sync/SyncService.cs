using Common;
using Common.Database;
using Common.Observe;
using Conversion;
using Garmin;
using Peloton;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sync
{
    public interface ISyncService
    {
        Task<SyncResult> SyncAsync(int numWorkouts);
    }

    public class SyncService : ISyncService
    {
        private static readonly ILogger _logger = LogContext.ForClass<SyncService>();
        private static readonly Histogram SyncHistogram = Prometheus.Metrics.CreateHistogram("p2g_sync_duration_seconds", "The histogram of sync jobs that have run.");

        private readonly Settings _config;
        private readonly IPelotonService _pelotonService;
        private readonly IGarminUploader _garminUploader;
        private readonly IEnumerable<IConverter> _converters;
        private readonly ISyncStatusDb _db;

        public SyncService(Settings config, IPelotonService pelotonService, IGarminUploader garminUploader, IEnumerable<IConverter> converters, ISyncStatusDb dbClient)
        {
            _config = config;
            _pelotonService = pelotonService;
            _garminUploader = garminUploader;
            _converters = converters;
            _db = dbClient;
        }

        public async Task<SyncResult> SyncAsync(int numWorkouts)
        {
            using var timer = SyncHistogram.NewTimer();
            using var activity = Tracing.Trace($"{nameof(SyncService)}.{nameof(SyncAsync)}");

            var response = new SyncResult();
            var syncTime = await _db.GetSyncStatusAsync();

            try
            {
                await _pelotonService.DownloadLatestWorkoutDataAsync(numWorkouts);
                response.PelotonDownloadSuccess = true;

            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to download workouts from Peleoton.");
                response.SyncSuccess = false;
                response.PelotonDownloadSuccess = false;
                response.Errors.Add(new ErrorResponse() { Message = "Failed to download workouts from Peloton. Check logs for more details." });
                return response;
            }

            try
            {
                foreach (var converter in _converters)
                {
                    converter.Convert();
                }
                response.ConversionSuccess = true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to convert workouts to FIT format.");
                response.SyncSuccess = false;
                response.ConversionSuccess = false;
                response.Errors.Add(new ErrorResponse() { Message = "Failed to convert workouts to FIT format. Check logs for more details." });
                return response;
            }

            try
            {
                await _garminUploader.UploadToGarminAsync();
                response.UploadToGarminSuccess = true;
            }
            catch (GarminUploadException e)
            {
                _logger.Error(e, "GUpload returned an error code. Failed to upload workouts.");
                _logger.Warning("GUpload failed to upload files. You can find the converted files at {@Path} \n You can manually upload your files to Garmin Connect, or wait for P2G to try again on the next sync job.", _config.App.OutputDirectory);

                response.SyncSuccess = false;
                response.UploadToGarminSuccess = false;
                response.Errors.Add(new ErrorResponse() { Message = "Failed to upload to Garmin Connect. Check logs for more details." });
                return response;
            }

            syncTime.LastSyncTime = DateTime.Now;
            syncTime.LastSuccessfulSyncTime = DateTime.Now;
            await _db.UpsertSyncStatusAsync(syncTime);

            response.SyncSuccess = true;
            return response;
        }
    }
}