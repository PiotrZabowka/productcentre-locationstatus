using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    public class LocationStatusRepository
    {
        private readonly string connectionString;
        public LocationStatusRepository()
        {
            connectionString = WorkerConfiguration.Config.GetValue<string>("ConnectionString");
        }
        public async Task UpdateMultipleLocationStatuses(IEnumerable<UpdateLocationStatusRequest> updateRequests)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "update_multiple_location_statuses";

                    var messagesJson = JsonConvert.SerializeObject(updateRequests.OrderBy(r => r.LocationId).ToArray());
                    cmd.Parameters.AddWithValue("@messages", NpgsqlDbType.Json, messagesJson);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UpdateMultipleLocationStatusSessionDate(IEnumerable<UpdateLocationStatusSessionRequest> updateRequests)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "update_multiple_location_status_session_date";

                    var messagesJson = JsonConvert.SerializeObject(updateRequests.OrderBy(r => r.SessionId).ToArray());
                    cmd.Parameters.AddWithValue("@messages", NpgsqlDbType.Json, messagesJson);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

    }
}
