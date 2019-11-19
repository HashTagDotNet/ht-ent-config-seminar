using Dapper;
using HT.Config.ConfigApi.Library.Configuration;
using HT.Config.ConfigApi.Library.Cryptography;
using HT.Config.ConfigApi.Library.dbModels;
using HT.Config.Shared;
using HT.Config.Shared.SettingsServiceModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HT.Config.ConfigApi.Library.Settings
{
    public class SettingsService : ISettingsService, IDisposable
    {
        private ILogger<SettingsService> _logger;
        private ApiOptions _options;
        private ICryptoService _cryptoService;

        public SettingsService(ILogger<SettingsService> logger, IOptions<ApiOptions> options,ICryptoService cryptoService)
        {
            _logger = logger;
            _options = options.Value;
            _cryptoService = cryptoService;
        }
        public async Task<SettingsResponse> GetSettings(SettingsRequest request)
        {
            var response = new SettingsResponse();
            try
            {
                if (validateRequest(request, response).IsOk == false)
                {
                    return response;
                }
                var sql = buildGetSettingsSql(request);
                var cn = await getConnection();
               using(var multiReader = await cn.QueryMultipleAsync(sql))
                {
                    while (!multiReader.IsConsumed)
                    {
                        mergeResults(response, multiReader.Read<dbSetting>());
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.ResponseCode = "SYS-ERR";
                response.ResponseMessage = ex.ToString();
                return response;
            }
        }

        private void mergeResults(SettingsResponse response, IEnumerable<dbSetting> results)
        {
            if (results == null) return;
            foreach(var dbResult in results)
            {
                response.Settings = response.Settings ?? new SortedDictionary<string, SettingDescription>();
                if (response.Settings.ContainsKey(dbResult.SettingKey))
                {

                    response.Settings[dbResult.SettingKey] = mapper(dbResult);
                }
                else
                {
                    response.Settings.Add(dbResult.SettingKey, mapper(dbResult));
                }
            }
        }

        private SettingDescription mapper(dbSetting dbResult)
        {
            return new SettingDescription()
            {
                Key = dbResult.SettingKey,
                Value = _cryptoService.Decrypt(dbResult.SettingValue),
                AppName = dbResult.AppName,
                EnvironmentName = dbResult.EnvironmentName,                
                Override = dbResult.Override,
                Version = 0,
                Description = "",
                SettingUUID = dbResult.SettingId.ToString(),
            };
        }

        private string buildGetSettingsSql(SettingsRequest request)
        {
            var sqlStatements = new List<string>();
            
            var sql = "SELECT * FROM HTSettings WITH (NOLOCK) WHERE ";
            
            if (!string.IsNullOrWhiteSpace(request.AppName))
            {
                sqlStatements.Add($"{sql} (AppName = '{request.AppName}' AND EnvironmentName IS NULL AND Override IS NULL)");
            }
            if (!string.IsNullOrWhiteSpace(request.AppName) && !string.IsNullOrWhiteSpace(request.Override))
            {
                sqlStatements.Add($"{sql} (AppName = '{request.AppName}' AND EnvironmentName IS NULL AND Override = '{request.Override}')");
            }

            if (!string.IsNullOrWhiteSpace(request.EnvironmentName))
            {
                sqlStatements.Add($"{sql} (AppName IS NULL AND EnvironmentName = '{request.EnvironmentName}' AND Override IS NULL)");
            }

            if (!string.IsNullOrWhiteSpace(request.EnvironmentName) && !string.IsNullOrWhiteSpace(request.Override))
            {                
                sqlStatements.Add($"{sql} (AppName IS NULL AND EnvironmentName = '{request.EnvironmentName}' AND Override = '{request.Override}')");
            }

            if (!string.IsNullOrWhiteSpace(request.AppName) && !string.IsNullOrWhiteSpace(request.EnvironmentName))
            {
                sqlStatements.Add($"{sql} (AppName = '{request.AppName}' AND EnvironmentName = '{request.EnvironmentName}' AND Override IS NULL)");
            }
            if (!string.IsNullOrWhiteSpace(request.AppName) && !string.IsNullOrWhiteSpace(request.EnvironmentName) && !string.IsNullOrWhiteSpace(request.Override))
            {
                sqlStatements.Add($"{sql} (AppName = '{request.AppName}' AND EnvironmentName = '{request.EnvironmentName}' AND Override = '{request.Override}')");
            }

            sql = string.Join(";", sqlStatements);

            return sql;
        }
        private ResponseBase validateRequest(SettingsRequest request, SettingsResponse response)
        {
            if (string.IsNullOrWhiteSpace(request.EnvironmentName) &&
                string.IsNullOrWhiteSpace(request.AppName))
            {
                response.ResponseMessage = "At least one of AppName or EnvironmentName is required";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }

            if (request.AppName != null && request.AppName.Length > 50)
            {
                response.ResponseMessage = "AppName must not be > 50 characters";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }

            if (request.EnvironmentName != null && request.EnvironmentName.Length > 50)
            {
                response.ResponseMessage = "EnvironmentName must not be > 50 characters";
                response.ResponseCode = "BAD-PARAM";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }
            if (request.Override != null && request.Override.Length > 50)
            {
                response.ResponseMessage = "Override must not be > 50 characters";
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

           

            return response;
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
        ~SettingsService()
        {
            Dispose(false);
        }
    }
}
