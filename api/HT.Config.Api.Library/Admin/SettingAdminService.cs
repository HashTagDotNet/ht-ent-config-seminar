using HT.Config.ConfigApi.Library.Configuration;
using HT.Config.Shared;
using HT.Config.Shared.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using HT.Config.ConfigApi.Library.dbModels;
using System.Data;
using HT.Config.ConfigApi.Library.Cryptography;

namespace HT.Config.ConfigApi.Library.Admin
{
    public class SettingAdminService : ISettingAdminService, IDisposable
    {
        private ILogger<SettingAdminService> _logger;
        private ApiOptions _options;
        private ICryptoService _cryptoService;

        public SettingAdminService(ILogger<SettingAdminService> logger,
            IOptions<ApiOptions> options,
            ICryptoService cryptoService)
        {
            _logger = logger;
            _options = options.Value;
            _cryptoService = cryptoService;
        }
        public async Task<CreateSettingResponse> CreateSetting(CreateSettingRequest request)
        {
            var response = new CreateSettingResponse();
            try
            {
                if ((await validateRequest(request, response)).IsOk == false)
                {
                    return response;
                }
                var sql = @"
INSERT INTO [dbo].[HTSettings] (
    SettingId,
    AppName,
    EnvironmentName,
    Override,
    SettingKey,
    SettingValue
) VALUES (
    @SettingId,
    @AppName,
    @EnvironmentName,
    @Override,
    @SettingKey,
    @SettingValue
)
";
                var cn = await getConnection();
                var dbSetting = toDbSetting(request);
                cn.Execute(sql, dbSetting);
                response.ResponseMessage = "Setting Created";
                response.ResponseCode = "SETTING-CREATED";
                response.StatusCode = (int)HttpStatusCode.Created;
            }
            catch (Exception ex)
            {
                response.ResponseCode = "SYS-ERR";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.ResponseMessage = ex.ToString();
            }
            return response;
        }
        public async Task<UpdateSettingResponse> UpdateSetting(UpdateSettingRequest request)
        {
            var response = new UpdateSettingResponse();
            try
            {
                if ((await validateRequest(request, response)).IsOk == false)
                {
                    return response;
                }
                var sql = @"
UPDATE [dbo].[HTSettings] SET
    AppName = @AppName,
    EnvironmentName = @EnvironmentName,
    Override = @Override,
    SettingKey = @SettingKey,
    SettingValue = @SettingValue
WHERE SettingId = @SettingId
";
                var cn = await getConnection();
                var dbSetting = toDbSetting(request);
                cn.Execute(sql, dbSetting);
                response.ResponseMessage = "Setting Updated";
                response.ResponseCode = "SETTING-UPD";
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.ResponseCode = "SYS-ERR";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.ResponseMessage = ex.ToString();
            }
            return response;
        }

        private async Task<ResponseBase> validateRequest(UpdateSettingRequest request, UpdateSettingResponse response)
        {
            if (string.IsNullOrWhiteSpace(request.SettingUUID))
            {
                response.ResponseMessage = "Setting UUID is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if(!Guid.TryParse(request.SettingUUID,out var _))
            {
                response.ResponseMessage = "Provided setting UUID is not in valid format";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }

            if (string.IsNullOrWhiteSpace(request.EnvironmentName) &&
               string.IsNullOrWhiteSpace(request.AppName))
            {
                response.ResponseMessage = "At least one of AppName or EnvironmentName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }

            if (string.IsNullOrWhiteSpace(request.Key))
            {
                response.ResponseMessage = "Key is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.Value))
            {
                response.ResponseMessage = "Value is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (request.Value.Length > 500)
            {
                response.ResponseMessage = "Key must not be longer than 500 characters";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.Value))
            {
                response.ResponseMessage = "Value is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (request.Value.Length > 1000)
            {
                response.ResponseMessage = "Value must not be longer than 1000 characters";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.ActorHostName))
            {
                response.ResponseMessage = "ActorHostName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.ActorName))
            {
                response.ResponseMessage = "ActorName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }

            var sql = $"SELECT count(SettingId) FROM HTSettings WITH (NOLOCK) WHERE SettingId = '{request.SettingUUID}'";
            var cn = await getConnection();
            using (var cmd = new SqlCommand(sql))
            {
                cmd.Connection = cn;
                var sqlResult = cmd.ExecuteScalar();
                if ((int)sqlResult == 0)
                {
                    response.ResponseMessage = $"Setting '{request.SettingUUID}' does not exist.";
                    response.ResponseCode = "SETTING-MISSING";
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    return response;
                }
            }
            return response;
        }

        private dbSetting toDbSetting(CreateSettingRequest request)
        {
            return new dbSetting()
            {
                AppName = request.AppName,
                EnvironmentName = request.EnvironmentName,
                Override = request.Override,
                SettingId = Guid.NewGuid(),
                SettingKey = request.Key,
                SettingValue = _cryptoService.Encrypt(request.Value)
            };
        }
        private dbSetting toDbSetting(UpdateSettingRequest request)
        {
            return new dbSetting()
            {
                AppName = request.AppName,
                EnvironmentName = request.EnvironmentName,
                Override = request.Override,
                SettingId = Guid.Parse(request.SettingUUID),
                SettingKey = request.Key,
                SettingValue = _cryptoService.Encrypt(request.Value)
            };
        }
        private async Task<ResponseBase> validateRequest(CreateSettingRequest request, CreateSettingResponse response)
        {
            if (string.IsNullOrWhiteSpace(request.EnvironmentName) &&
                string.IsNullOrWhiteSpace(request.AppName))
            {
                response.ResponseMessage = "At least one of AppName or EnvironmentName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                response.ResponseMessage = "Key is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.Value))
            {
                response.ResponseMessage = "Value is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (request.Value.Length > 500)
            {
                response.ResponseMessage = "Key must not be longer than 500 characters";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.Value))
            {
                response.ResponseMessage = "Value is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (request.Value.Length > 1000)
            {
                response.ResponseMessage = "Value must not be longer than 1000 characters";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.ActorHostName))
            {
                response.ResponseMessage = "ActorHostName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.ActorName))
            {
                response.ResponseMessage = "ActorName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }

            var sql = buildSettingExistsSql(request);

            var cn = await getConnection();
            using (var cmd = new SqlCommand(sql))
            {
                cmd.Connection = cn;
                var sqlResult = cmd.ExecuteScalar();
                if ((int)sqlResult != 0)
                {
                    response.ResponseMessage = $"Setting already exists.";
                    response.ResponseCode = "SETTING-EXISTS";
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    return response;
                }
            }

            return response;
        }

        private string buildSettingExistsSql(CreateSettingRequest request)
        {
            var conditions = "";
            var sql = "SELECT count(SettingId) FROM HTSettings WITH (NOLOCK) WHERE ";
            if (!string.IsNullOrWhiteSpace(request.AppName))
            {
                if (conditions.Length > 0) conditions += " AND ";
                conditions += $"AppName = '{request.AppName}'";
            }
            else
            {
                if (conditions.Length > 0) conditions += " AND ";
                conditions += "AppName is NULL";
            }
            if (!string.IsNullOrWhiteSpace(request.EnvironmentName))
            {
                if (conditions.Length > 0) conditions += " AND ";
                conditions += $"EnvironmentName = '{request.EnvironmentName}'";
            }
            else
            {
                if (conditions.Length > 0) conditions += " AND ";
                conditions += "EnvironmentName is NULL";
            }
            if (!string.IsNullOrWhiteSpace(request.Override))
            {
                if (conditions.Length > 0) conditions += " AND ";
                conditions += $"Override = '{request.Override}'";
            }
            else
            {
                if (conditions.Length > 0) conditions += " AND ";
                conditions += "Override is NULL";
            }

            if (conditions.Length > 0) conditions += " AND ";
            conditions += $"SettingKey = '{request.Key}'";

            sql += conditions;

            return sql;
        }

        private SqlConnection _dbConnection;
        private async Task<SqlConnection> getConnection()
        {
            if (_dbConnection != null && _dbConnection.State == System.Data.ConnectionState.Open)
            {
                return _dbConnection;
            }
            _dbConnection = new SqlConnection(_options.DatabaseConnection);
            await _dbConnection.OpenAsync();
            return _dbConnection;
        }



 
        private bool _isDisposed = false;
        public void Dispose(bool isDisposing)
        {
            if (isDisposing && !_isDisposed)
            {
                if (_dbConnection != null)
                {
                    if (_dbConnection.State == System.Data.ConnectionState.Open)
                    {
                        _dbConnection.Close();
                    }
                    _dbConnection.Dispose();
                    _dbConnection = null;
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

        public async Task<UpdateSettingValueResponse> UpdateSettingValue(UpdateSettingValueRequest request)
        {
            var response = new UpdateSettingValueResponse();
            try
            {
                if ((await validateRequest(request, response)).IsOk == false)
                {
                    return response;
                }
                var sql = @"UPDATE [dbo].[HTSettings] SET SettingValue = @SettingValue WHERE SettingId = @SettingId";
                var cn = await getConnection();
               
                cn.Execute(sql, new { SettingValue = _cryptoService.Encrypt(request.Value), SettingId = request.SettingUUID });
                response.ResponseMessage = "Setting Updated";
                response.ResponseCode = "SETTING-UPD";
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.ResponseCode = "SYS-ERR";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.ResponseMessage = ex.ToString();
            }
            return response;
        }

        private async Task<ResponseBase> validateRequest(UpdateSettingValueRequest request, UpdateSettingValueResponse response)
        {
            if (string.IsNullOrWhiteSpace(request.SettingUUID))
            {
                response.ResponseMessage = "Setting UUID is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (!Guid.TryParse(request.SettingUUID, out var _))
            {
                response.ResponseMessage = "Provided setting UUID is not in valid format";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.Value))
            {
                response.ResponseMessage = "Value is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.ActorHostName))
            {
                response.ResponseMessage = "ActorHostName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.ActorName))
            {
                response.ResponseMessage = "ActorName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            var sql = $"SELECT COUNT(SettingId) FROM [dbo].[HTSettings] WHERE SettingId = '{request.SettingUUID}'";

            var cn = await getConnection();
            using (var cmd = new SqlCommand(sql))
            {
                cmd.Connection = cn;
                var sqlResult = cmd.ExecuteScalar();
                if ((int)sqlResult == 0)
                {
                    response.ResponseMessage = $"Setting {request.SettingUUID} does not exist.";
                    response.ResponseCode = "SETTING-MISSING";
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    return response;
                }
            }
            return response;
        }

        public async Task<DeleteSettingResponse> DeleteSetting(DeleteSettingRequest request)
        {
            var response = new DeleteSettingResponse();
            try
            {
                if ((await validateRequest(request, response)).IsOk == false)
                {
                    return response;
                }
                var sql = @"DELETE [dbo].[HTSettings] WHERE SettingId = @SettingId";
                var cn = await getConnection();

                cn.Execute(sql, new { SettingId = request.SettingUUID });
                response.ResponseMessage = "Setting Deleted";
                response.ResponseCode = "SETTING-DEL";
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.ResponseCode = "SYS-ERR";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.ResponseMessage = ex.ToString();
            }
            return response;
        }

        private async Task<ResponseBase> validateRequest(DeleteSettingRequest request, DeleteSettingResponse response)
        {
            var sql = $"SELECT COUNT(SettingId) FROM [dbo].[HTSettings] WHERE SettingId = '{request.SettingUUID}'";
            if (string.IsNullOrWhiteSpace(request.SettingUUID))
            {
                response.ResponseMessage = "Setting UUID is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (!Guid.TryParse(request.SettingUUID, out var _))
            {
                response.ResponseMessage = "Provided setting UUID is not in valid format";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.ActorHostName))
            {
                response.ResponseMessage = "ActorHostName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (string.IsNullOrWhiteSpace(request.ActorName))
            {
                response.ResponseMessage = "ActorName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            var cn = await getConnection();
            using (var cmd = new SqlCommand(sql))
            {
                cmd.Connection = cn;
                var sqlResult = cmd.ExecuteScalar();
                if ((int)sqlResult == 0)
                {
                    response.ResponseMessage = $"Setting {request.SettingUUID} does not exist.";
                    response.ResponseCode = "SETTING-MISSING";
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    return response;
                }
            }

            return response;
        }

        ~SettingAdminService()
        {
            Dispose(false);
        }
    }
}
